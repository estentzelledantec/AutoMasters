using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMasters.Modeles;
using AutoMasters.Services;
using AutoMasters.VuesModeles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AutoMasters.Tests;

/// <summary>
/// Suite de tests empiriques contradictoires (Adversarial Stress Harness)
/// pour la validation stricte de la logique de présentation du Milestone 3 (F18-F26).
/// Valide le comportement réel des ViewModels, l'idempotence, le compte à rebours,
/// la validation du prix, le calcul de valorisation/P&L, et la cérémonie booster.
/// </summary>
public class ViewModelEmpiricalChallengeTests
{
    #region Attributs

    private readonly MockHttpMessageHandler _mockHandler;
    private readonly ServiceAPI _service;

    #endregion

    #region Constructeurs

    public ViewModelEmpiricalChallengeTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _service = new ServiceAPI(_mockHandler);
    }

    #endregion

    #region 1. MarcheVueModele - Achat immédiat & Idempotence (F22, F23)

    [Fact]
    public async Task Marche_AcheterOffreAsync_EmitsIdempotencyKeyInHeaderAndBody()
    {
        // Disposition
        string capturedIdempotencyHeader = "";
        string capturedOfferIdInBody = "";
        string capturedIdempotencyInBody = "";

        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<object>());
        _mockHandler.EnregistrerRoute(HttpMethod.Post, "/orders/buy", req =>
        {
            if (req.Headers.TryGetValues("Idempotency-Key", out var vals))
            {
                capturedIdempotencyHeader = vals.FirstOrDefault() ?? "";
            }

            string body = req.Content?.ReadAsStringAsync().Result ?? "";
            var json = JObject.Parse(body);
            capturedOfferIdInBody = json["offerId"]?.ToString() ?? "";
            capturedIdempotencyInBody = json["idempotencyKey"]?.ToString() ?? "";

            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new { success = true });
        });

        var vm = new MarcheVueModele(_service);
        vm.SoldeUtilisateur = 500000m;

        var offre = new Offre
        {
            Identifiant = "offre_ferrari_812",
            IdentifiantExemplaire = "copy_11",
            Prix = 250000m,
            Statut = "active",
            Carte = new Carte { Marque = "Ferrari", Modele = "812 Superfast" }
        };

        // Action
        await vm.AcheterOffreAsync(offre);

        // Assertions
        Assert.False(string.IsNullOrWhiteSpace(capturedIdempotencyHeader), "L'en-tête Idempotency-Key doit être présent.");
        Assert.True(Guid.TryParse(capturedIdempotencyHeader, out _), "La clé d'idempotence doit être un GUID valide.");
        Assert.Equal("offre_ferrari_812", capturedOfferIdInBody);
        Assert.Equal(capturedIdempotencyHeader, capturedIdempotencyInBody);

        // Vérification de la mise à jour du solde
        Assert.Equal(250000m, vm.SoldeUtilisateur);
        Assert.Equal("250 000 coins", vm.SoldeFormate.Replace("\u00A0", " ").Replace("\u202F", " "));
    }

    [Fact]
    public async Task Marche_AcheterOffreAsync_InsufficientBalance_AbortsWithoutApiCall()
    {
        // Disposition
        int buyCallCount = 0;
        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<object>());
        _mockHandler.EnregistrerRoute(HttpMethod.Post, "/orders/buy", req =>
        {
            buyCallCount++;
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new { success = true });
        });

        var vm = new MarcheVueModele(_service);
        vm.SoldeUtilisateur = 50000m; // Moins que le prix demandé

        var offre = new Offre
        {
            Identifiant = "offre_bugatti",
            Prix = 300000m,
            Statut = "active",
            Carte = new Carte { Marque = "Bugatti", Modele = "Chiron" }
        };

        // Action
        await vm.AcheterOffreAsync(offre);

        // Assertions
        Assert.Equal(0, buyCallCount);
        Assert.Equal(50000m, vm.SoldeUtilisateur);
        Assert.Contains("Fonds insuffisants", vm.Message);
    }

    [Fact]
    public async Task Marche_AcheterOffreAsync_NullOffer_IsSafeNoOp()
    {
        var vm = new MarcheVueModele(_service);
        decimal initialBalance = vm.SoldeUtilisateur;

        await vm.AcheterOffreAsync(null);

        Assert.Equal(initialBalance, vm.SoldeUtilisateur);
    }

    [Fact]
    public async Task Marche_AcheterOffreAsync_ApiError_DoesNotDeductBalance()
    {
        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<object>());
        _mockHandler.QuandPost("/orders/buy", HttpStatusCode.BadRequest, new
        {
            code = "OFFER_EXPIRED",
            message = "Cette offre n'est plus disponible."
        });

        var vm = new MarcheVueModele(_service);
        decimal initialBalance = vm.SoldeUtilisateur;

        var offre = new Offre
        {
            Identifiant = "off_fail",
            Prix = 100000m,
            Carte = new Carte { Marque = "Porsche", Modele = "911" }
        };

        await vm.AcheterOffreAsync(offre);

        Assert.Equal(initialBalance, vm.SoldeUtilisateur);
        Assert.Contains("n'est plus disponible", vm.Message);
    }

    #endregion

    #region 2. MarcheVueModele - Réservation 15 minutes (F23)

    [Fact]
    public async Task Marche_ReserverOffreAsync_SetsReservedStatusAnd15MinCountdown()
    {
        // Disposition
        string requestedUrl = "";
        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<object>());
        _mockHandler.EnregistrerRoute(HttpMethod.Post, "/offers/off_reserve_1/reservations", req =>
        {
            requestedUrl = req.RequestUri?.AbsolutePath ?? "";
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new { success = true });
        });

        var vm = new MarcheVueModele(_service);
        var offre = new Offre
        {
            Identifiant = "off_reserve_1",
            Prix = 150000m,
            Statut = "active",
            EstReservee = false,
            Carte = new Carte { Marque = "McLaren", Modele = "720S" }
        };

        DateTime beforeUtc = DateTime.UtcNow;

        // Action
        await vm.ReserverOffreAsync(offre);

        DateTime afterUtc = DateTime.UtcNow;

        // Assertions
        Assert.True(offre.EstReservee);
        Assert.Equal("reserved", offre.Statut);
        Assert.NotNull(offre.DateExpiration);
        Assert.True(offre.DateExpiration.Value >= beforeUtc.AddMinutes(14.8));
        Assert.True(offre.DateExpiration.Value <= afterUtc.AddMinutes(15.2));
        Assert.Contains("15 minutes", vm.Message);
        Assert.Contains("reservations", requestedUrl);
    }

    [Fact]
    public async Task Marche_ReserverOffreAsync_AlreadyReserved_RejectsWithoutNetworkCall()
    {
        int reservationCalls = 0;
        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<object>());
        _mockHandler.EnregistrerRoute(HttpMethod.Post, "/offers/off_already_res/reservations", req =>
        {
            reservationCalls++;
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new { success = true });
        });

        var vm = new MarcheVueModele(_service);
        var offre = new Offre
        {
            Identifiant = "off_already_res",
            Prix = 100000m,
            Statut = "reserved",
            EstReservee = true
        };

        await vm.ReserverOffreAsync(offre);

        Assert.Equal(0, reservationCalls);
        Assert.Contains("déjà réservée", vm.Message);
    }

    #endregion

    #region 3. CreationOffreVueModele - Validation stricte du prix (F24)

    [Theory]
    [InlineData("0")]
    [InlineData("0,00")]
    [InlineData("0.0")]
    [InlineData("-10")]
    [InlineData("-250000")]
    [InlineData("-0.01")]
    public async Task CreationOffre_PrixInferieurOuEgalAZero_EstRejete(string prixInvalide)
    {
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());

        var vm = new CreationOffreVueModele(_service);
        vm.CarteSelectionnee = new CartePossedee("copy_1", new Carte { Marque = "Alpine", Modele = "A110" }, 55000m);
        vm.PrixSaisi = prixInvalide;

        await vm.CreerOffreAsync();

        Assert.Equal("Le prix de vente doit être strictement supérieur à zéro.", vm.MessageErreur);
        Assert.DoesNotContain(_mockHandler.RequetesEnregistrees, r => r.Method == HttpMethod.Post && (r.RequestUri?.AbsolutePath.Contains("offers") ?? false));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("100coins")]
    [InlineData("!@#$")]
    public async Task CreationOffre_FormatPrixInvalide_EstRejete(string formatInvalide)
    {
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());

        var vm = new CreationOffreVueModele(_service);
        vm.CarteSelectionnee = new CartePossedee("copy_2", new Carte { Marque = "Peugeot", Modele = "205 T16" }, 45000m);
        vm.PrixSaisi = formatInvalide;

        await vm.CreerOffreAsync();

        Assert.Equal("Veuillez saisir un montant numérique valide.", vm.MessageErreur);
    }

    [Fact]
    public async Task CreationOffre_AucunVehiculeSelectionne_EstRejete()
    {
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());

        var vm = new CreationOffreVueModele(_service);
        vm.CarteSelectionnee = null;
        vm.PrixSaisi = "150000";

        await vm.CreerOffreAsync();

        Assert.Equal("Veuillez sélectionner un véhicule de votre garage.", vm.MessageErreur);
    }

    [Theory]
    [InlineData("250 000", 250000)]
    [InlineData("250000,50", 250000.50)]
    [InlineData("1", 1)]
    [InlineData("999999.99", 999999.99)]
    public async Task CreationOffre_PrixValide_EnvoieRequetePostOffers(string saisie, double attenduDouble)
    {
        decimal prixAttendu = (decimal)attenduDouble;
        decimal prixRecuDansCorps = 0m;
        string copyIdRecuDansCorps = "";

        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        _mockHandler.EnregistrerRoute(HttpMethod.Post, "/offers", req =>
        {
            string body = req.Content?.ReadAsStringAsync().Result ?? "";
            var json = JObject.Parse(body);
            prixRecuDansCorps = json["price"]?.Value<decimal>() ?? 0m;
            copyIdRecuDansCorps = json["copyId"]?.ToString() ?? "";

            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new
            {
                id = "off_created_success",
                status = "active",
                price = prixRecuDansCorps
            });
        });

        var vm = new CreationOffreVueModele(_service);
        vm.CarteSelectionnee = new CartePossedee("copy_alpine_42", new Carte { Marque = "Alpine", Modele = "A110 R" }, 110000m);
        vm.PrixSaisi = saisie;

        await vm.CreerOffreAsync();

        Assert.Equal("", vm.MessageErreur);
        Assert.Equal(prixAttendu, prixRecuDansCorps);
        Assert.Equal("copy_alpine_42", copyIdRecuDansCorps);
    }

    #endregion

    #region 4. GarageVueModele - Valorisation, P&L et Badge Multiplicateur (F25)

    [Fact]
    public async Task Garage_GainPositif_AfficheVertEtSignePlus()
    {
        // Données conformes à la maquette Figma 2 : +32 500 € P&L total en vert
        var portfolioData = new
        {
            totalValuation = 482500m,
            totalProfitLoss = 32500m,
            positions = new List<object>
            {
                new { id = "c1", card = new { id = "p1", brand = "Porsche", model = "911 GT3", rarity = "ÉPIQUE" }, quantity = 2, currentValue = 200000m, purchasePrice = 175000m }
            }
        };

        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, portfolioData);

        var vm = new GarageVueModele(_service);
        await vm.ChargerGarageAsync();

        Assert.Equal(482500m, vm.ValeurTotale);
        string valeurNorm = vm.ValeurTotaleFormatee.Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.Contains("482 500", valeurNorm);
        Assert.Equal(32500m, vm.TotalGainPerte);
        Assert.True(vm.EstEnGain);
        Assert.StartsWith("+", vm.TotalGainPerteFormate);
        Assert.EndsWith("€ P&L total", vm.TotalGainPerteFormate);
        string pnlNorm = vm.TotalGainPerteFormate.Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.Contains("32 500", pnlNorm);
    }

    [Fact]
    public async Task Garage_PerteNegative_AfficheRougeEtSigneMoins()
    {
        var portfolioData = new
        {
            totalValuation = 150000m,
            totalProfitLoss = -2000m,
            positions = new List<object>()
        };

        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, portfolioData);

        var vm = new GarageVueModele(_service);
        await vm.ChargerGarageAsync();

        Assert.Equal(-2000m, vm.TotalGainPerte);
        Assert.False(vm.EstEnGain);
        Assert.StartsWith("-", vm.TotalGainPerteFormate);
        Assert.DoesNotContain("--", vm.TotalGainPerteFormate);
        string pnlNorm = vm.TotalGainPerteFormate.Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.Contains("2 000", pnlNorm);
        Assert.EndsWith("€ P&L total", vm.TotalGainPerteFormate);
    }

    [Fact]
    public void CartePossedee_QuantiteBadge_AfficheMultiplicateurUniquementSiSuperieurAUn()
    {
        var pos1 = new CartePossedee("1", new Carte(), 10000m, quantite: 1);
        var pos2 = new CartePossedee("2", new Carte(), 10000m, quantite: 2);
        var pos3 = new CartePossedee("3", new Carte(), 10000m, quantite: 3);
        var pos0 = new CartePossedee("0", new Carte(), 10000m, quantite: 0);

        Assert.Equal(string.Empty, pos1.QuantiteBadge);
        Assert.Equal("x2", pos2.QuantiteBadge);
        Assert.Equal("x3", pos3.QuantiteBadge);
        Assert.Equal(string.Empty, pos0.QuantiteBadge);
    }

    #endregion

    #region 5. BoosterVueModele - Séquence 5 cartes et flux Garage (F26)

    [Fact]
    public async Task Booster_SequenceComplete5Cartes_ProgressionEtRetournementInteractif()
    {
        var cartesServeur = new List<Carte>
        {
            new Carte { Identifiant = "c1", Marque = "Renault", Modele = "Clio V6", Rarete = "COMMUNE", PrixOfficiel = 25000m },
            new Carte { Identifiant = "c2", Marque = "Subaru", Modele = "Impreza WRX", Rarete = "RARE", PrixOfficiel = 45000m },
            new Carte { Identifiant = "c3", Marque = "Nissan", Modele = "GT-R Nismo", Rarete = "ÉPIQUE", PrixOfficiel = 120000m },
            new Carte { Identifiant = "c4", Marque = "Peugeot", Modele = "208 GTi", Rarete = "COMMUNE", PrixOfficiel = 18000m },
            new Carte { Identifiant = "c5", Marque = "Bugatti", Modele = "Chiron Pur Sport", Rarete = "LÉGENDAIRE", PrixOfficiel = 850000m }
        };

        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = cartesServeur });

        var boosterVm = new BoosterVueModele(_service);

        // 1. Démarrage de l'ouverture
        await boosterVm.DemarrerOuvertureAsync();

        Assert.Equal("Carte 1 sur 5", boosterVm.TexteProgression);
        Assert.Equal("RETOURNER ▶", boosterVm.LibelleBoutonAction);
        Assert.False(boosterVm.EstRetournee);
        Assert.True(boosterVm.Segment1Actif);
        Assert.False(boosterVm.Segment2Actif);
        Assert.Equal("Renault", boosterVm.CarteActuelle?.Marque);

        // 2. Retourner Carte 1
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstRetournee);
        Assert.Equal("CARTE SUIVANTE ▶", boosterVm.LibelleBoutonAction);

        // 3. Passer à Carte 2
        boosterVm.ActionCarteSuivante();
        Assert.Equal("Carte 2 sur 5", boosterVm.TexteProgression);
        Assert.Equal("RETOURNER ▶", boosterVm.LibelleBoutonAction);
        Assert.False(boosterVm.EstRetournee);
        Assert.True(boosterVm.Segment1Actif);
        Assert.True(boosterVm.Segment2Actif);
        Assert.False(boosterVm.Segment3Actif);
        Assert.Equal("Subaru", boosterVm.CarteActuelle?.Marque);
        Assert.Equal("RARE", boosterVm.LibelleRareteActuelle);

        // 4. Retourner Carte 2
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstRetournee);

        // 5. Passer à Carte 3
        boosterVm.ActionCarteSuivante();
        Assert.Equal("Carte 3 sur 5", boosterVm.TexteProgression);
        Assert.Equal("ÉPIQUE", boosterVm.LibelleRareteActuelle);

        // 6. Retourner Carte 3
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstRetournee);

        // 7. Passer à Carte 4
        boosterVm.ActionCarteSuivante();
        Assert.Equal("Carte 4 sur 5", boosterVm.TexteProgression);

        // 8. Retourner Carte 4
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstRetournee);

        // 9. Passer à Carte 5 (dernière carte)
        boosterVm.ActionCarteSuivante();
        Assert.Equal("Carte 5 sur 5", boosterVm.TexteProgression);
        Assert.Equal("Bugatti", boosterVm.CarteActuelle?.Marque);
        Assert.Equal("★ LÉGENDAIRE", boosterVm.LibelleRareteActuelle);
        Assert.True(boosterVm.Segment5Actif);

        // 10. Retourner Carte 5
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstRetournee);
        Assert.True(boosterVm.EstDerniereCarte);
        Assert.Equal("TERMINER & AJOUTER AU GARAGE", boosterVm.LibelleBoutonAction);

        // 11. Finaliser la cérémonie
        bool evenementDeclenche = false;
        boosterVm.SurBoosterTermine += () => evenementDeclenche = true;

        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstTermine);
        Assert.True(evenementDeclenche);
    }

    [Fact]
    public async Task Garage_CoordinationBooster_DecrementeQuotaEtRechargeCollection()
    {
        bool boosterOuvert = false;

        _mockHandler.EnregistrerRoute(HttpMethod.Get, "/portfolio", req =>
        {
            int count = boosterOuvert ? 7 : 2; // 2 au départ, 7 après le booster de 5
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new
            {
                totalValuation = 250000m,
                totalProfitLoss = 15000m,
                positions = Enumerable.Range(1, count).Select(i => new
                {
                    id = $"card_pos_{i}",
                    card = new { id = $"c_{i}", brand = $"Brand_{i}", model = $"Model_{i}", rarity = "COMMUNE" },
                    quantity = 1,
                    currentValue = 25000m,
                    purchasePrice = 20000m
                }).ToList()
            });
        });

        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new
        {
            cards = Enumerable.Range(1, 5).Select(i => new
            {
                id = $"new_{i}",
                brand = $"Brand_New_{i}",
                model = $"Model_New_{i}",
                rarity = "RARE",
                officialPrice = 30000m
            }).ToList()
        });

        var garageVm = new GarageVueModele(_service);
        await garageVm.ChargerGarageAsync();

        Assert.Equal(3, garageVm.PaquetsDisponibles);
        Assert.Equal(2, garageVm.CartesPossedees.Count);

        // Déclencher l'ouverture de booster
        await garageVm.OuvrirBoosterAsync();
        boosterOuvert = true;

        Assert.True(garageVm.EstBoosterModalOuvert);
        Assert.NotNull(garageVm.BoosterVueModele);

        // Avancer la séquence jusqu'à la fin (10 clics exactement pour 5 cartes : 5 flips + 4 advances + 1 finish)
        for (int step = 0; step < 10; step++)
        {
            garageVm.BoosterVueModele.ActionCarteSuivante();
        }

        // Le booster doit être fermé, le quota décrémenté (2/3), et le garage synchronisé
        Assert.False(garageVm.EstBoosterModalOuvert);
        Assert.Equal(2, garageVm.PaquetsDisponibles);
        Assert.Equal("2/3 disponibles", garageVm.QuotaBoosterFormate);
        Assert.Equal(7, garageVm.CartesPossedees.Count);
    }

    [Fact]
    public async Task Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine()
    {
        var cartesServeur = Enumerable.Range(1, 5).Select(i => new Carte
        {
            Identifiant = $"c{i}",
            Marque = $"Brand_{i}",
            Modele = $"Model_{i}",
            Rarete = "COMMUNE"
        }).ToList();

        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = cartesServeur });

        var boosterVm = new BoosterVueModele(_service);
        await boosterVm.DemarrerOuvertureAsync();

        int eventFiredCount = 0;
        boosterVm.SurBoosterTermine += () => eventFiredCount++;

        // Étapes 0 à 8 (9 clics pour les 4 premières cartes et la face recto de la 5ème)
        for (int i = 0; i < 9; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        Assert.True(boosterVm.EstDerniereCarte);
        Assert.Equal("TERMINER & AJOUTER AU GARAGE", boosterVm.LibelleBoutonAction);

        // 10ème clic : terminaison normale
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstTermine);
        Assert.Equal(1, eventFiredCount);

        // 11ème clic : double-clic intempestif ou tap répété sur l'écran tactile
        boosterVm.ActionCarteSuivante();

        // Invariant contradictoire : la finalisation d'un booster DOIT être idempotente
        Assert.Equal(1, eventFiredCount);
    }

    [Fact]
    public async Task Garage_QuotaEpuise_NePeutPasOuvrirBooster()
    {
        int boosterCallCount = 0;
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        _mockHandler.EnregistrerRoute(HttpMethod.Post, "/boosters/open", req =>
        {
            boosterCallCount++;
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new { cards = new List<object>() });
        });

        var garageVm = new GarageVueModele(_service);
        garageVm.PaquetsDisponibles = 0;

        Assert.False(garageVm.PeutOuvrirBooster);

        await garageVm.OuvrirBoosterAsync();

        Assert.Equal(0, boosterCallCount);
        Assert.False(garageVm.EstBoosterModalOuvert);
    }

    #endregion
}
