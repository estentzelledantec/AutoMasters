using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using AutoMasters.Modeles;
using AutoMasters.Services;
using Xunit;

namespace AutoMasters.Tests;

/// <summary>
/// Suite de 33 tests unitaires exhaustive pour ServiceAPI (Fonctionnalité F17).
/// Utilise MockHttpMessageHandler pour garantir une exécution 100% déterministe et hors-ligne.
/// </summary>
public class ServiceApiTests
{
    #region Attributs

    private readonly MockHttpMessageHandler _mockHandler;
    private readonly ServiceAPI _service;

    #endregion

    #region Constructeurs

    public ServiceApiTests()
    {
        _mockHandler = new MockHttpMessageHandler();
        _service = new ServiceAPI(_mockHandler);
    }

    #endregion

    #region Tests Authentification (F07)

    [Fact]
    public async Task SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer()
    {
        // Disposition
        string jetonTest = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.token_unitaire_test_123";
        _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
        {
            accessToken = jetonTest,
            refreshToken = "refresh_unitaire_456",
            expiresAt = DateTime.UtcNow.AddHours(2)
        });

        // Action
        await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");

        // Assertions
        Assert.NotNull(_service.ClientHttp.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", _service.ClientHttp.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal(jetonTest, _service.ClientHttp.DefaultRequestHeaders.Authorization.Parameter);

        // Vérification de la requête HTTP émise
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        Assert.Equal(HttpMethod.Post, requete.Method);
        string corpsEnvoye = await requete.Content!.ReadAsStringAsync();
        Assert.Contains("etudiant@bts.sio", corpsEnvoye);
        Assert.Contains("MotDePasse123!", corpsEnvoye);
    }

    [Fact]
    public async Task SeConnecterAsync_IdentifiantsInvalides_401_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/auth/login", HttpStatusCode.Unauthorized, new
        {
            code = "UNAUTHORIZED",
            message = "Identifiants invalides."
        });

        // Action & Assertion
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.SeConnecterAsync("mauvais_utilisateur", "mauvais_mdp"));

        Assert.Contains("Connexion refusée", exception.Message);
    }

    [Fact]
    public async Task SeConnecterAsync_ErreurServeur_500_ExtraitMessageErreurApi()
    {
        // Disposition
        _mockHandler.QuandPost("/auth/login", HttpStatusCode.InternalServerError, new
        {
            code = "SERVER_ERROR",
            message = "Base de données temporairement inaccessible."
        });

        // Action & Assertion
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.SeConnecterAsync("etudiant@bts.sio", "mdp"));

        Assert.Contains("Base de données temporairement inaccessible.", exception.Message);
    }

    [Fact]
    public async Task SeConnecterAsync_JetonManquantDansReponse_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
        {
            accessToken = "",
            expiresAt = DateTime.UtcNow
        });

        // Action & Assertion
        var exception = await Assert.ThrowsAsync<Exception>(() =>
            _service.SeConnecterAsync("etudiant@bts.sio", "mdp"));

        Assert.Contains("jeton", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SeConnecterAsync_PanneReseau_LeveHttpRequestException()
    {
        // Disposition
        _mockHandler.SimulerPanneReseau = true;

        // Action & Assertion
        await Assert.ThrowsAsync<HttpRequestException>(() =>
            _service.SeConnecterAsync("etudiant@bts.sio", "mdp"));
    }

    [Fact]
    public async Task Test_401_RevoqueSessionEtAuthentification()
    {
        // Disposition : établissement préalable d'une session valide
        string jetonTest = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.token_actif_avant_401";
        _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
        {
            accessToken = jetonTest,
            refreshToken = "refresh_token_test",
            expiresAt = DateTime.UtcNow.AddHours(2)
        });

        await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");

        // Vérification de l'état initial : utilisateur authentifié avec Bearer token
        Assert.True(_service.EstAuthentifie, "L'utilisateur doit être authentifié initialement.");
        Assert.NotNull(_service.ClientHttp.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", _service.ClientHttp.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal(jetonTest, _service.ClientHttp.DefaultRequestHeaders.Authorization.Parameter);
        Assert.Equal(jetonTest, _service.Jeton);

        // Simulation de l'expiration/révocation : l'API renvoie 401 sur un endpoint protégé (GET /cards)
        _mockHandler.QuandGet("/cards", HttpStatusCode.Unauthorized, new
        {
            code = "UNAUTHORIZED",
            message = "Jeton de session expiré ou révoqué."
        });

        // Action : appel protégé levant l'exception
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RecupererCatalogueAsync());
        Assert.Contains("Connexion refusée ou expirée", ex.Message);

        // Assertions critiques : révocation immédiate de la session en mémoire et dans les en-têtes
        Assert.False(_service.EstAuthentifie, "EstAuthentifie doit impérativement basculer à false après réception d'un code 401.");
        Assert.Null(_service.ClientHttp.DefaultRequestHeaders.Authorization);
        Assert.Null(_service.Jeton);
    }

    #endregion

    #region Tests Catalogue (F08, F09)

    [Fact]
    public async Task RecupererCatalogueAsync_Succes_DeserialiseListeCartesNewtonsoft()
    {
        // Disposition
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, new
        {
            items = new[]
            {
                new
                {
                    id = "card_ferrari_101",
                    brand = "Ferrari",
                    model = "LaFerrari Aperta",
                    variant = "Aperta",
                    year = 2017,
                    powerHp = 963,
                    rarity = "LÉGENDAIRE",
                    officialPrice = 480000.0,
                    copiesInCirculation = 210,
                    imageUrl = "https://images.unsplash.com/photo-1583121274602",
                    description = "Hypercar hybride V12"
                }
            },
            page = 1,
            pageSize = 20,
            total = 1,
            totalPages = 1
        });

        // Action
        List<Carte> cartes = await _service.RecupererCatalogueAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Single(cartes);

        Carte ferrari = cartes[0];
        Assert.Equal("card_ferrari_101", ferrari.Identifiant);
        Assert.Equal("Ferrari", ferrari.Marque);
        Assert.Equal("LaFerrari Aperta", ferrari.Modele);
        Assert.Equal("Aperta", ferrari.Variante);
        Assert.Equal(2017, ferrari.Annee);
        Assert.Equal(963, ferrari.Puissance);
        Assert.Equal("LÉGENDAIRE", ferrari.Rarete);
        Assert.Equal(480000m, ferrari.PrixOfficiel);
        Assert.Equal(210, ferrari.ExemplairesEnCirculation);
    }

    [Fact]
    public async Task RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl()
    {
        // Disposition
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, new
        {
            items = Array.Empty<object>(),
            page = 1,
            totalPages = 0
        });

        // Action
        await _service.RecupererCatalogueAsync(search: "Porsche", rarete: "ÉPIQUE");

        // Assertions
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        string queryString = requete.RequestUri?.Query ?? string.Empty;
        Assert.Contains("Porsche", queryString);
        Assert.Contains(Uri.EscapeDataString("ÉPIQUE"), queryString);
    }

    [Fact]
    public async Task RecupererCatalogueAsync_CatalogueVide_RetourneListeNonNullEtVide()
    {
        // Disposition
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, new
        {
            items = Array.Empty<object>(),
            page = 1,
            totalPages = 0
        });

        // Action
        List<Carte> cartes = await _service.RecupererCatalogueAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Empty(cartes);
    }

    [Fact]
    public async Task RecupererCatalogueAsync_TableauBrutDirect_DeserialiseListeCartes()
    {
        // Disposition : le serveur retourne un tableau brut JSON direct sans enveloppe { items: [...] }
        string jsonTableauBrut = @"[
            {
                ""id"": ""card_alpine_01"",
                ""brand"": ""Alpine"",
                ""model"": ""A110 R"",
                ""variant"": ""Turini"",
                ""year"": 2023,
                ""powerHp"": 300,
                ""rarity"": ""ÉPIQUE"",
                ""officialPrice"": 105000.0,
                ""circulationCount"": 500,
                ""imageUrl"": ""https://images.unsplash.com/alpine.jpg"",
                ""description"": ""Version sportive allégée.""
            },
            {
                ""id"": ""card_bugatti_01"",
                ""brand"": ""Bugatti"",
                ""model"": ""Chiron Super Sport"",
                ""variant"": ""300+"",
                ""year"": 2021,
                ""powerHp"": 1600,
                ""rarity"": ""LÉGENDAIRE"",
                ""officialPrice"": 3500000.0,
                ""circulationCount"": 30,
                ""imageUrl"": ""https://images.unsplash.com/bugatti.jpg"",
                ""description"": ""Hypercar franchissant les 490 km/h.""
            }
        ]";

        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        List<Carte> cartes = await _service.RecupererCatalogueAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Equal(2, cartes.Count);

        var c1 = cartes[0];
        Assert.Equal("card_alpine_01", c1.Identifiant);
        Assert.Equal("Alpine", c1.Marque);
        Assert.Equal("A110 R", c1.Modele);
        Assert.Equal("Turini", c1.Variante);
        Assert.Equal(2023, c1.Annee);
        Assert.Equal(300, c1.Puissance);
        Assert.Equal("ÉPIQUE", c1.Rarete);
        Assert.Equal(105000m, c1.PrixOfficiel);

        var c2 = cartes[1];
        Assert.Equal("card_bugatti_01", c2.Identifiant);
        Assert.Equal("Bugatti", c2.Marque);
        Assert.Equal("Chiron Super Sport", c2.Modele);
        Assert.Equal(1600, c2.Puissance);
        Assert.Equal("LÉGENDAIRE", c2.Rarete);
        Assert.Equal(3500000m, c2.PrixOfficiel);
    }

    [Fact]
    public async Task RecupererFicheCarteAsync_CarteExistante_RetourneCaracteristiquesTechniques()
    {
        // Disposition
        _mockHandler.QuandGet("/cards/card_porsche_104", HttpStatusCode.OK, new
        {
            id = "card_porsche_104",
            brand = "Porsche",
            model = "911 GT3 RS",
            variant = "992 GT3 RS",
            year = 2023,
            powerHp = 525,
            rarity = "ÉPIQUE",
            officialPrice = 223000.0,
            copiesInCirculation = 1200,
            imageUrl = "https://images.unsplash.com/photo-1614162692292",
            description = "Athlète haute performance conçue pour les circuits",
            topSpeedKmH = 296,
            zeroToHundred = 3.2
        });

        // Action
        Carte porsche = await _service.RecupererFicheCarteAsync("card_porsche_104");

        // Assertions
        Assert.NotNull(porsche);
        Assert.Equal("card_porsche_104", porsche.Identifiant);
        Assert.Equal(296, porsche.VitesseMax);
        Assert.Equal(3.2, porsche.ZeroACent);
        Assert.Equal(1200, porsche.ExemplairesEnCirculation);
        Assert.Equal("ÉPIQUE", porsche.Rarete);
    }

    [Fact]
    public async Task RecupererFicheCarteAsync_CarteInexistante_404_LeveException()
    {
        // Disposition
        _mockHandler.QuandGet("/cards/inconnu", HttpStatusCode.NotFound, new
        {
            code = "NOT_FOUND",
            message = "Carte inexistante."
        });

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererFicheCarteAsync("inconnu"));

        Assert.Contains("Carte inexistante.", ex.Message);
    }

    #endregion

    #region Tests Portefeuille & Garage (F10)

    [Fact]
    public async Task RecupererPortefeuilleAsync_Succes_DeserialisePositionsEtCalculeValorisation()
    {
        // Disposition
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new
        {
            positions = new[]
            {
                new
                {
                    id = "copy_porsche",
                    card = new
                    {
                        id = "card_104",
                        brand = "Porsche",
                        model = "911 GT3 RS",
                        officialPrice = 223000.0
                    },
                    currentValue = 222000.0,
                    quantity = 1,
                    purchasePrice = 198000.0
                },
                new
                {
                    id = "copy_toyota",
                    card = new
                    {
                        id = "card_107",
                        brand = "Toyota",
                        model = "GR Yaris",
                        officialPrice = 47000.0
                    },
                    currentValue = 47000.0,
                    quantity = 2,
                    purchasePrice = 39500.0
                }
            }
        });

        // Action
        Portefeuille portefeuille = await _service.RecupererPortefeuilleAsync();

        // Assertions
        Assert.NotNull(portefeuille);
        Assert.Equal(2, portefeuille.Positions.Count);

        // Position 1 : Porsche (Quantité 1)
        var posPorsche = portefeuille.Positions[0];
        Assert.Equal("copy_porsche", posPorsche.Identifiant);
        Assert.Equal(222000m, posPorsche.ValeurActuelle);
        Assert.Equal(1, posPorsche.Quantite);
        Assert.Equal(198000m, posPorsche.PrixAchat);
        Assert.Equal(24000m, posPorsche.GainPerte); // 222 000 - 198 000 = +24 000

        // Position 2 : Toyota (Quantité 2)
        var posToyota = portefeuille.Positions[1];
        Assert.Equal(2, posToyota.Quantite);
        Assert.Equal(47000m, posToyota.ValeurActuelle);
        Assert.Equal(39500m, posToyota.PrixAchat);
        Assert.Equal(15000m, posToyota.GainPerte); // 2 * (47 000 - 39 500) = +15 000

        // Calculs d'agrégation du Portefeuille
        decimal valeurAttendue = (222000m * 1) + (47000m * 2); // 222 000 + 94 000 = 316 000
        decimal gainPerteAttendu = 24000m + 15000m; // +39 000
        Assert.Equal(valeurAttendue, portefeuille.ValeurTotale);
        Assert.Equal(gainPerteAttendu, portefeuille.TotalGainPerte);
    }

    [Fact]
    public async Task RecupererPortefeuilleAsync_CalculPlusMoinsValue_CalculExactParQuantite()
    {
        // Disposition
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new
        {
            positions = new[]
            {
                new
                {
                    id = "copy_toyota_multi",
                    card = new
                    {
                        id = "card_107",
                        brand = "Toyota",
                        model = "GR Yaris",
                        officialPrice = 47000.0
                    },
                    currentValue = 47000.0,
                    quantity = 3,
                    purchasePrice = 39000.0
                }
            }
        });

        // Action
        Portefeuille portefeuille = await _service.RecupererPortefeuilleAsync();

        // Assertions
        Assert.NotNull(portefeuille);
        Assert.Single(portefeuille.Positions);
        var pos = portefeuille.Positions[0];
        Assert.Equal(3, pos.Quantite);
        // (47 000 - 39 000) * 3 = 8 000 * 3 = 24 000
        Assert.Equal(24000m, pos.GainPerte);
        Assert.Equal(24000m, portefeuille.TotalGainPerte);
    }

    [Fact]
    public async Task RecupererPortefeuilleAsync_GarageVide_RetourneZeroCoins()
    {
        // Disposition
        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new
        {
            positions = Array.Empty<object>()
        });

        // Action
        Portefeuille portefeuille = await _service.RecupererPortefeuilleAsync();

        // Assertions
        Assert.NotNull(portefeuille);
        Assert.Empty(portefeuille.Positions);
        Assert.Equal(0m, portefeuille.ValeurTotale);
        Assert.Equal(0m, portefeuille.TotalGainPerte);
    }

    [Fact]
    public async Task RecupererPortefeuilleAsync_TableauBrutDirect_CreePortefeuilleEtCalculeValorisation()
    {
        // Disposition : le serveur retourne un tableau brut JSON de positions sans enveloppe { positions: [...] }
        string jsonTableauBrut = @"[
            {
                ""id"": ""copy_alpine_garage"",
                ""card"": {
                    ""id"": ""card_alpine_01"",
                    ""brand"": ""Alpine"",
                    ""model"": ""A110 R"",
                    ""officialPrice"": 105000.0
                },
                ""currentValue"": 115000.0,
                ""quantity"": 2,
                ""purchasePrice"": 100000.0
            },
            {
                ""id"": ""copy_porsche_garage"",
                ""card"": {
                    ""id"": ""card_porsche_01"",
                    ""brand"": ""Porsche"",
                    ""model"": ""911 GT3 RS"",
                    ""officialPrice"": 223000.0
                },
                ""currentValue"": 240000.0,
                ""quantity"": 1,
                ""purchasePrice"": 210000.0
            }
        ]";

        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        Portefeuille portefeuille = await _service.RecupererPortefeuilleAsync();

        // Assertions
        Assert.NotNull(portefeuille);
        Assert.Equal(2, portefeuille.Positions.Count);

        // Position 1 (Alpine - Quantité 2)
        var pos1 = portefeuille.Positions[0];
        Assert.Equal("copy_alpine_garage", pos1.Identifiant);
        Assert.Equal("Alpine", pos1.Carte.Marque);
        Assert.Equal(2, pos1.Quantite);
        Assert.Equal(115000m, pos1.ValeurActuelle);
        Assert.Equal(100000m, pos1.PrixAchat);
        // Gain attendu : 2 * (115 000 - 100 000) = +30 000
        Assert.Equal(30000m, pos1.GainPerte);

        // Position 2 (Porsche - Quantité 1)
        var pos2 = portefeuille.Positions[1];
        Assert.Equal("copy_porsche_garage", pos2.Identifiant);
        Assert.Equal("Porsche", pos2.Carte.Marque);
        Assert.Equal(1, pos2.Quantite);
        Assert.Equal(240000m, pos2.ValeurActuelle);
        Assert.Equal(210000m, pos2.PrixAchat);
        // Gain attendu : 1 * (240 000 - 210 000) = +30 000
        Assert.Equal(30000m, pos2.GainPerte);

        // Calculs agrégés calculés dynamiquement par le constructeur Portefeuille(positions)
        // ValeurTotale : (115 000 * 2) + (240 000 * 1) = 230 000 + 240 000 = 470 000
        Assert.Equal(470000m, portefeuille.ValeurTotale);
        // TotalGainPerte : 30 000 + 30 000 = +60 000
        Assert.Equal(60000m, portefeuille.TotalGainPerte);
        Assert.Equal(3, portefeuille.NombreTotalCartes);
        Assert.Equal(2, portefeuille.NombreModelesDistincts);
        Assert.True(portefeuille.EstEnGain);
    }

    #endregion

    #region Tests Marché & Transactions (F11, F12, F13, F14)

    [Fact]
    public async Task RecupererOffresAsync_Succes_DeserialiseOffresActives()
    {
        // Disposition
        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new
        {
            items = new[]
            {
                new
                {
                    id = "offer_ferrari_01",
                    copyId = "copy_ferrari_mkt",
                    card = new
                    {
                        id = "card_101",
                        brand = "Ferrari",
                        model = "LaFerrari Aperta"
                    },
                    seller = "TurboAce",
                    price = 480000.0,
                    status = "active"
                }
            },
            total = 1
        });

        // Action
        List<Offre> offres = await _service.RecupererOffresAsync();

        // Assertions
        Assert.NotNull(offres);
        Assert.Single(offres);
        Offre offre = offres[0];
        Assert.Equal("offer_ferrari_01", offre.Identifiant);
        Assert.Equal("TurboAce", offre.Vendeur);
        Assert.Equal(480000m, offre.Prix);
        Assert.Equal("active", offre.Statut);
    }

    [Fact]
    public async Task RecupererOffresAsync_TableauBrutDirect_DeserialiseOffres()
    {
        // Disposition : le serveur retourne un tableau brut JSON direct d'offres sans enveloppe { items: [...] }
        string jsonTableauBrut = @"[
            {
                ""id"": ""offer_direct_01"",
                ""copyId"": ""copy_ferrari_direct"",
                ""card"": {
                    ""id"": ""card_ferrari_101"",
                    ""brand"": ""Ferrari"",
                    ""model"": ""LaFerrari Aperta""
                },
                ""seller"": ""ScuderiaBTS"",
                ""price"": 450000.0,
                ""status"": ""active""
            },
            {
                ""id"": ""offer_direct_02"",
                ""copyId"": ""copy_toyota_direct"",
                ""card"": {
                    ""id"": ""card_toyota_107"",
                    ""brand"": ""Toyota"",
                    ""model"": ""GR Yaris""
                },
                ""seller"": ""RallyFan"",
                ""price"": 42000.0,
                ""status"": ""active""
            }
        ]";

        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        List<Offre> offres = await _service.RecupererOffresAsync();

        // Assertions
        Assert.NotNull(offres);
        Assert.Equal(2, offres.Count);

        var off1 = offres[0];
        Assert.Equal("offer_direct_01", off1.Identifiant);
        Assert.Equal("copy_ferrari_direct", off1.IdentifiantExemplaire);
        Assert.Equal("ScuderiaBTS", off1.Vendeur);
        Assert.Equal(450000m, off1.Prix);
        Assert.Equal("active", off1.Statut);
        Assert.False(off1.EstReservee);
        Assert.NotNull(off1.Carte);
        Assert.Equal("Ferrari", off1.Carte.Marque);

        var off2 = offres[1];
        Assert.Equal("offer_direct_02", off2.Identifiant);
        Assert.Equal("RallyFan", off2.Vendeur);
        Assert.Equal(42000m, off2.Prix);
        Assert.NotNull(off2.Carte);
        Assert.Equal("Toyota", off2.Carte.Marque);
    }

    [Fact]
    public async Task AcheterOffreAsync_Succes_TransmetEnTeteIdempotencyKey()
    {
        // Disposition
        _mockHandler.QuandPost("/orders/buy", HttpStatusCode.OK, new
        {
            status = "completed",
            transactionId = "tx_123456789",
            remainingBalance = 20000.0
        });

        // Action
        bool succes = await _service.AcheterOffreAsync("offer_ferrari_01");

        // Assertions
        Assert.True(succes);

        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        Assert.True(requete.Headers.Contains("Idempotency-Key"), "L'en-tête Idempotency-Key (RM006) doit être présent.");
        string cleIdempotence = requete.Headers.GetValues("Idempotency-Key").First();
        Assert.True(Guid.TryParse(cleIdempotence, out _), "La clé d'idempotence doit être un UUID valide.");
    }

    [Fact]
    public async Task AcheterOffreAsync_SoldeInsuffisant_400_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/orders/buy", HttpStatusCode.BadRequest, new
        {
            code = "INSUFFICIENT_FUNDS",
            message = "Solde insuffisant pour cet achat."
        });

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.AcheterOffreAsync("offer_trop_chere"));

        Assert.Contains("Solde insuffisant", ex.Message);
    }

    [Fact]
    public async Task AcheterOffreAsync_OffreDejaVendue_409_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/orders/buy", HttpStatusCode.Conflict, new
        {
            code = "OFFER_NOT_AVAILABLE",
            message = "L'offre sélectionnée n'est plus disponible."
        });

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.AcheterOffreAsync("offer_deja_vendue"));

        Assert.Contains("n'est plus disponible", ex.Message);
    }

    [Fact]
    public async Task ReserverOffreAsync_Succes_VerrouilleOffreEtConfirme()
    {
        // Disposition
        _mockHandler.QuandPost("/offers/offer_101/reservations", HttpStatusCode.OK, new
        {
            offerId = "offer_101",
            status = "reserved",
            expiresAt = DateTime.UtcNow.AddMinutes(15)
        });

        // Action
        bool succes = await _service.ReserverOffreAsync("offer_101");

        // Assertions
        Assert.True(succes);
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        Assert.Contains("reservations", requete.RequestUri?.AbsolutePath ?? string.Empty);
    }

    [Fact]
    public async Task ReserverOffreAsync_DejaReservee_409_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/offers/offer_101/reservations", HttpStatusCode.Conflict, new
        {
            code = "CANNOT_RESERVE",
            message = "Cette offre ne peut pas être réservée."
        });

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.ReserverOffreAsync("offer_101"));

        Assert.Contains("ne peut pas être réservée", ex.Message);
    }

    [Fact]
    public async Task CreerOffreAsync_DonneesValides_RetourneOffreCreee()
    {
        // Disposition
        _mockHandler.QuandPost("/offers", HttpStatusCode.Created, new
        {
            id = "offer_creee_99",
            copyId = "copy_toyota_123",
            price = 49000.0,
            status = "active"
        });

        // Action
        Offre offre = await _service.CreerOffreAsync("copy_toyota_123", 49000m);

        // Assertions
        Assert.NotNull(offre);
        Assert.Equal("offer_creee_99", offre.Identifiant);
        Assert.Equal(49000m, offre.Prix);
        Assert.Equal("active", offre.Statut);
    }

    [Fact]
    public async Task CreerOffreAsync_PrixNegatif_400_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/offers", HttpStatusCode.BadRequest, new
        {
            code = "INVALID_PRICE",
            message = "Le prix de vente doit être strictement supérieur à zéro."
        });

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreerOffreAsync("copy_toyota_123", -100m));

        Assert.Contains("strictement supérieur à zéro", ex.Message);
    }

    #endregion

    #region Tests Boosters (F15)

    [Fact]
    public async Task OuvrirBoosterAsync_QuotaDisponible_Retourne5Cartes()
    {
        // Disposition
        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new
        {
            cards = new[]
            {
                new { id = "c1", brand = "Ferrari", model = "LaFerrari" },
                new { id = "c2", brand = "Porsche", model = "911 GT3 RS" },
                new { id = "c3", brand = "Renault", model = "Mégane R.S." },
                new { id = "c4", brand = "Toyota", model = "GR Yaris" },
                new { id = "c5", brand = "Lotus", model = "Emira" }
            },
            remainingQuota = 2
        });

        // Action
        List<Carte> cartes = await _service.OuvrirBoosterAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Equal(5, cartes.Count);
    }

    [Fact]
    public async Task OuvrirBoosterAsync_TableauBrutDirect_Retourne5Cartes()
    {
        // Disposition : le serveur retourne un tableau brut JSON direct de 5 cartes sans enveloppe { cards: [...] }
        string jsonTableauBrut = @"[
            { ""id"": ""card_b1"", ""brand"": ""Ferrari"", ""model"": ""F40"", ""rarity"": ""LÉGENDAIRE"" },
            { ""id"": ""card_b2"", ""brand"": ""Porsche"", ""model"": ""911 GT3 RS"", ""rarity"": ""ÉPIQUE"" },
            { ""id"": ""card_b3"", ""brand"": ""Renault"", ""model"": ""Clio V6"", ""rarity"": ""RARE"" },
            { ""id"": ""card_b4"", ""brand"": ""Peugeot"", ""model"": ""205 Turbo 16"", ""rarity"": ""RARE"" },
            { ""id"": ""card_b5"", ""brand"": ""Alpine"", ""model"": ""A110 R"", ""rarity"": ""ÉPIQUE"" }
        ]";

        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        List<Carte> cartes = await _service.OuvrirBoosterAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Equal(5, cartes.Count);

        Assert.Equal("card_b1", cartes[0].Identifiant);
        Assert.Equal("Ferrari", cartes[0].Marque);
        Assert.Equal("F40", cartes[0].Modele);
        Assert.Equal("LÉGENDAIRE", cartes[0].Rarete);

        Assert.Equal("card_b2", cartes[1].Identifiant);
        Assert.Equal("Porsche", cartes[1].Marque);

        Assert.Equal("card_b3", cartes[2].Identifiant);
        Assert.Equal("Renault", cartes[2].Marque);

        Assert.Equal("card_b4", cartes[3].Identifiant);
        Assert.Equal("Peugeot", cartes[3].Marque);

        Assert.Equal("card_b5", cartes[4].Identifiant);
        Assert.Equal("Alpine", cartes[4].Marque);
    }

    [Fact]
    public async Task OuvrirBoosterAsync_QuotaEpuise_400_LeveException()
    {
        // Disposition
        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.BadRequest, new
        {
            code = "QUOTA_EXHAUSTED",
            message = "Aucun booster disponible aujourd'hui."
        });

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.OuvrirBoosterAsync());

        Assert.Contains("Aucun booster disponible", ex.Message);
    }

    #endregion

    #region Tests Robustesse, DI & Cas Limites

    [Fact]
    public async Task ServiceAPI_ReponseHtml502_GereErreurServeurSansCrashJson()
    {
        // Disposition : réponse HTML brute d'un proxy Nginx ou passerelle 502/503
        _mockHandler.QuandGet("/cards", HttpStatusCode.BadGateway,
            "<html><head><title>502 Bad Gateway</title></head><body><center><h1>502 Bad Gateway</h1></center></body></html>");

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererCatalogueAsync());

        Assert.NotNull(ex.Message);
        Assert.DoesNotContain("JsonReaderException", ex.GetType().Name);
    }

    [Fact]
    public void ServiceAPI_InjectableHandler_PermetTestsCompletsHorsLigne()
    {
        var mock = new MockHttpMessageHandler();
        var service = new ServiceAPI(mock);
        Assert.NotNull(service.ClientHttp);
        Assert.Equal("https://api-cartes-g2.fldsio.com/", service.ClientHttp.BaseAddress?.ToString());
        Assert.Equal(TimeSpan.FromSeconds(15), service.ClientHttp.Timeout);
    }

    [Fact]
    public async Task RecupererCatalogueAsync_CaracteresSpeciauxRecherche_EchappeUrl()
    {
        // Disposition
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, new
        {
            items = Array.Empty<object>(),
            total = 0
        });

        // Action
        await _service.RecupererCatalogueAsync(search: "Porsche & Ferrari", rarete: "LÉGENDAIRE");

        // Assertions
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        string queryString = requete.RequestUri?.Query ?? string.Empty;
        Assert.Contains(Uri.EscapeDataString("Porsche & Ferrari"), queryString);
        Assert.Contains(Uri.EscapeDataString("LÉGENDAIRE"), queryString);
    }

    [Fact]
    public async Task AcheterOffreAsync_FormatCleIdempotence_EstUuidV4Valide()
    {
        // Disposition
        _mockHandler.QuandPost("/orders/buy", HttpStatusCode.OK, new
        {
            status = "completed",
            transactionId = "tx_987654321"
        });

        // Action
        await _service.AcheterOffreAsync("offer_test_uuid");

        // Assertions
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        Assert.True(requete.Headers.Contains("Idempotency-Key"));
        string key = requete.Headers.GetValues("Idempotency-Key").First();
        Assert.True(Guid.TryParse(key, out Guid parsedGuid));
        Assert.NotEqual(Guid.Empty, parsedGuid);
    }

    [Fact]
    public async Task RecupererCatalogueAsync_Reponse200JsonCorrompu_LeveExceptionClaireSansCrash()
    {
        // Disposition : le serveur retourne HTTP 200 OK avec un corps textuel JSON malformé / corrompu
        string jsonInvalide = "{ invalid json }";
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, jsonInvalide);

        // Action & Assertion : l'appel doit lever une Exception gérée proprement
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererCatalogueAsync());

        // Assertions : le message doit être informatif et non vide (pas de chaîne vide silencieuse)
        Assert.NotNull(ex);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message), "Le message d'erreur ne doit jamais être vide lors d'une désérialisation échouée.");
        Assert.True(
            ex.Message.Contains("format", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("invalide", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("JSON", StringComparison.OrdinalIgnoreCase),
            $"Le message d'erreur doit expliciter une anomalie de format ou de données invalides. Message obtenu: '{ex.Message}'");
    }

    #endregion
}
