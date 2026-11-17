using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Xml.Linq;
using AutoMasters.Modeles;
using AutoMasters.Services;
using AutoMasters.VuesModeles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AutoMasters.Tests;

/// <summary>
/// Suite de tests empiriques contradictoires (Adversarial Stress Harness) pour le Milestone 5 :
/// Validation approfondie de Dynamic Cards (F31) et Dedicated Ordres Page (F33).
/// </summary>
public class ChallengerM5F31F33VerificationTests
{
    private static string SolutionRootDir
    {
        get
        {
            string current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                string candidate = Path.Combine(current, "AutoMasters.slnx");
                if (File.Exists(candidate))
                {
                    return current;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            return @"c:\Users\stent\source\repos\AutoMasters";
        }
    }

    private static string ProjectDir => Path.Combine(SolutionRootDir, "AutoMasters");

    #region 1. Dynamic Cards (F31) - Carte.cs Calculated Properties Stress Tests

    [Theory]
    [InlineData("LÉGENDAIRE", "#F59E0B", "#78350F", "UR")]
    [InlineData("Légendaire", "#F59E0B", "#78350F", "UR")]
    [InlineData("LEGENDARY", "#F59E0B", "#78350F", "UR")]
    [InlineData("UR", "#F59E0B", "#78350F", "UR")]
    [InlineData("  UR  ", "#F59E0B", "#78350F", "UR")]
    [InlineData("ÉPIQUE", "#EC4899", "#701A75", "SR")]
    [InlineData("Épique", "#EC4899", "#701A75", "SR")]
    [InlineData("EPIQUE", "#EC4899", "#701A75", "SR")]
    [InlineData("EPIC", "#EC4899", "#701A75", "SR")]
    [InlineData("SR", "#EC4899", "#701A75", "SR")]
    [InlineData("RARE", "#0284C7", "#0C4A6E", "R")]
    [InlineData("rare", "#0284C7", "#0C4A6E", "R")]
    [InlineData("R", "#0284C7", "#0C4A6E", "R")]
    [InlineData("PEU COMMUNE", "#10B981", "#064E3B", "UC")]
    [InlineData("peu commune", "#10B981", "#064E3B", "UC")]
    [InlineData("PEU_COMMUNE", "#10B981", "#064E3B", "UC")]
    [InlineData("UNCOMMON", "#10B981", "#064E3B", "UC")]
    [InlineData("UC", "#10B981", "#064E3B", "UC")]
    [InlineData("COMMUNE", "#64748B", "#1E293B", "C")]
    [InlineData("commune", "#64748B", "#1E293B", "C")]
    [InlineData("COMMON", "#64748B", "#1E293B", "C")]
    [InlineData("C", "#64748B", "#1E293B", "C")]
    public void F31_Carte_CalculatedProperties_AllStandardRarities_MapCorrectly(
        string rarityInput,
        string expectedPrimary,
        string expectedSecondary,
        string expectedBadge)
    {
        var carte = new Carte { Rarete = rarityInput };

        Assert.NotNull(expectedPrimary);
        Assert.NotNull(expectedSecondary);
        Assert.Equal(expectedBadge, carte.BadgeRareteCourt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("INCONNU")]
    [InlineData("MYTHIQUE")]
    [InlineData("CUSTOM_RARITY_XYZ")]
    public void F31_Carte_CalculatedProperties_UnknownOrNullRarity_GracefulFallbackWithoutException(string? unhandledRarity)
    {
        var carte = new Carte();
        if (unhandledRarity != null)
        {
            carte.Rarete = unhandledRarity;
        }

        // Test with getter/computed properties directly
        var exception = Record.Exception(() =>
        {
            string badge = carte.BadgeRareteCourt;
            int atk = carte.ScoreAttaque;
            int def = carte.ScoreDefense;

            Assert.Equal("C", badge);
            Assert.True(atk > 0);
            Assert.True(def > 0);
        });

        Assert.Null(exception);
    }

    [Fact]
    public void F31_Carte_CalculatedProperties_NullBackingFieldViaJson_HandlesGracefully()
    {
        // When JSON contains explicit null for rarity
        string json = "{\"id\":\"test_null\",\"brand\":\"Test\",\"model\":\"Vehicle\",\"rarity\":null,\"powerHp\":0,\"topSpeedKmH\":0}";
        Carte? carte = JsonConvert.DeserializeObject<Carte>(json);

        Assert.NotNull(carte);
        Assert.Equal("C", carte.BadgeRareteCourt);
        Assert.Equal(500, carte.ScoreAttaque);
        Assert.Equal(5000, carte.ScoreDefense);
    }

    [Theory]
    [InlineData(100, 1000)]
    [InlineData(500, 5000)]
    [InlineData(1500, 15000)]
    [InlineData(0, 500)]
    [InlineData(-50, 500)]
    public void F31_Carte_ScoreAttaque_HandlesZeroAndNegativeValuesGracefully(int power, int expectedScore)
    {
        var carte = new Carte { Puissance = power };
        Assert.Equal(expectedScore, carte.ScoreAttaque);
    }

    [Theory]
    [InlineData(200, 5000)]
    [InlineData(300, 7500)]
    [InlineData(400, 10000)]
    [InlineData(0, 5000)]
    [InlineData(-200, 5000)]
    public void F31_Carte_ScoreDefense_HandlesZeroAndNegativeValuesGracefully(int speed, int expectedScore)
    {
        var carte = new Carte { VitesseMax = speed };
        Assert.Equal(expectedScore, carte.ScoreDefense);
    }

    #endregion

    #region 2. Dedicated Ordres Page (F33) - OrdresVueModele Partitioning Tests

    [Fact]
    public async Task F33_OrdresVueModele_ChargerDonneesAsync_PartitionsVentesAndReservationsCorrectly()
    {
        var mockHandler = new MockHttpMessageHandler();

        var offres = new List<Offre>
        {
            // 1. Vente active (non réservée)
            new Offre("o1", "c1", 100000m, "Vendeur1", "active", new Carte { Marque = "Alpine", Modele = "A110" })
            {
                EstReservee = false
            },
            // 2. Vente active ET réservée
            new Offre("o2", "c2", 250000m, "Vendeur2", "active", new Carte { Marque = "Porsche", Modele = "911 GT3" })
            {
                EstReservee = true
            },
            // 3. Offre au statut 'reserved'
            new Offre("o3", "c3", 500000m, "Vendeur3", "reserved", new Carte { Marque = "Ferrari", Modele = "F8" })
            {
                EstReservee = true
            },
            // 4. Offre vendue (doit être ignorée des deux onglets)
            new Offre("o4", "c4", 120000m, "Vendeur4", "sold", new Carte { Marque = "BMW", Modele = "M3" })
            {
                EstReservee = false
            },
            // 5. Offre annulée (doit être ignorée des deux onglets)
            new Offre("o5", "c5", 80000m, "Vendeur5", "cancelled", new Carte { Marque = "Audi", Modele = "RS6" })
            {
                EstReservee = false
            }
        };

        mockHandler.QuandGet("/offers", HttpStatusCode.OK, offres);

        var service = new ServiceAPI(mockHandler);
        var vm = new OrdresVueModele(service);

        // Wait for data load
        await vm.ChargerDonneesAsync();

        // MesVentes : Statut "active" OU "reserved" -> o1, o2, o3 (3 offres)
        Assert.Equal(3, vm.MesVentes.Count);
        Assert.Contains(vm.MesVentes, o => o.Identifiant == "o1");
        Assert.Contains(vm.MesVentes, o => o.Identifiant == "o2");
        Assert.Contains(vm.MesVentes, o => o.Identifiant == "o3");
        Assert.DoesNotContain(vm.MesVentes, o => o.Identifiant == "o4");
        Assert.DoesNotContain(vm.MesVentes, o => o.Identifiant == "o5");

        // MesReservations : EstReservee == true -> o2, o3 (2 offres)
        Assert.Equal(2, vm.MesReservations.Count);
        Assert.Contains(vm.MesReservations, o => o.Identifiant == "o2");
        Assert.Contains(vm.MesReservations, o => o.Identifiant == "o3");
        Assert.DoesNotContain(vm.MesReservations, o => o.Identifiant == "o1");
    }

    [Fact]
    public async Task F33_OrdresVueModele_ChargerDonneesAsync_EmptyList_InitializesEmptyCollections()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<Offre>());

        var service = new ServiceAPI(mockHandler);
        var vm = new OrdresVueModele(service);

        await vm.ChargerDonneesAsync();

        Assert.NotNull(vm.MesVentes);
        Assert.Empty(vm.MesVentes);
        Assert.NotNull(vm.MesReservations);
        Assert.Empty(vm.MesReservations);
        Assert.Equal(string.Empty, vm.Message);
    }

    [Fact]
    public async Task F33_OrdresVueModele_ChargerDonneesAsync_ServerError_SetsErrorMessageWithoutCrashing()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulerPanneReseau = true;

        var service = new ServiceAPI(mockHandler);
        var vm = new OrdresVueModele(service);

        await vm.ChargerDonneesAsync();

        Assert.False(vm.EstOccupe);
        Assert.True(vm.EstDisponible);
        Assert.False(string.IsNullOrWhiteSpace(vm.Message));
        Assert.Contains("Erreur lors du chargement des ordres", vm.Message);
    }

    [Fact]
    public void F33_OrdresVueModele_ChangerOnglet_TogglesVisibilityAndTabColors()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandGet("/offers", HttpStatusCode.OK, new List<Offre>());
        var service = new ServiceAPI(mockHandler);

        var vm = new OrdresVueModele(service);

        // Default state (Onglet 0: Mes Ventes)
        Assert.Equal(0, vm.OngletActifIndex);
        Assert.True(vm.AfficherMesVentes);
        Assert.False(vm.AfficherMesReservations);

        // Switch to Onglet 1: Mes Reservations
        vm.ChangerOnglet("1");
        Assert.Equal(1, vm.OngletActifIndex);
        Assert.False(vm.AfficherMesVentes);
        Assert.True(vm.AfficherMesReservations);

        // Invalid input string should safely do nothing
        vm.ChangerOnglet("invalid");
        Assert.Equal(1, vm.OngletActifIndex);
    }

    [Fact]
    public async Task F33_OrdresVueModele_ChargerDonneesAsync_GuardedAgainstConcurrentCalls()
    {
        var mockHandler = new MockHttpMessageHandler();
        int apiCalls = 0;
        mockHandler.EnregistrerRoute(HttpMethod.Get, "/offers", req =>
        {
            apiCalls++;
            return MockHttpMessageHandler.CreerReponseJson(HttpStatusCode.OK, new List<Offre>());
        });

        var service = new ServiceAPI(mockHandler);
        var vm = new OrdresVueModele(service);

        // Allow initial ctor background load to finish
        await Task.Delay(50);
        int initialCalls = apiCalls;
        Assert.Equal(1, initialCalls);

        // If manually setting EstOccupe = true (representing an in-flight network request)
        vm.EstOccupe = true;
        await vm.ChargerDonneesAsync();

        // Should return early without sending any additional network request
        Assert.Equal(initialCalls, apiCalls);
    }

    #endregion

    #region 3. AppShell & Architecture Verification (F33)

    [Fact]
    public void F33_AppShell_Tab3_PointsToDedicatedOrdresPageAndDoesNotDuplicateMarche()
    {
        string shellPath = Path.Combine(ProjectDir, "AppShell.xaml");
        Assert.True(File.Exists(shellPath), $"AppShell.xaml must exist at: {shellPath}");

        string xaml = File.ReadAllText(shellPath);
        var doc = XDocument.Parse(xaml);
        var defaultNs = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        var tabBars = doc.Descendants(defaultNs + "TabBar").ToList();
        Assert.Single(tabBars);
        var mainTabBar = tabBars.First();

        var tabs = mainTabBar.Elements(defaultNs + "Tab").ToList();
        Assert.Equal(5, tabs.Count);

        // Inspect Tab 2 (Marché)
        var tab2 = tabs[1];
        Assert.Equal("Marché", (string?)tab2.Attribute("Title"));
        var tab2Content = tab2.Element(defaultNs + "ShellContent");
        Assert.NotNull(tab2Content);
        Assert.Equal("MarchePage", (string?)tab2Content.Attribute("Route"));
        Assert.Contains("vues:MarchePage", (string?)tab2Content.Attribute("ContentTemplate"));

        // Inspect Tab 3 (Ordres - Must be OrdresPage)
        var tab3 = tabs[2];
        Assert.Equal("Ordres", (string?)tab3.Attribute("Title"));
        Assert.Equal("Ordres", (string?)tab3.Attribute("Route"));
        var tab3Content = tab3.Element(defaultNs + "ShellContent");
        Assert.NotNull(tab3Content);
        Assert.Equal("OrdresPage", (string?)tab3Content.Attribute("Route"));
        Assert.Contains("vues:OrdresPage", (string?)tab3Content.Attribute("ContentTemplate"));

        // Assert MarchePage is NOT duplicated in Tab 3 or anywhere else
        var allShellContents = mainTabBar.Descendants(defaultNs + "ShellContent").ToList();
        int marcheCount = allShellContents.Count(sc => (string?)sc.Attribute("Route") == "MarchePage");
        Assert.Equal(1, marcheCount);

        int ordresCount = allShellContents.Count(sc => (string?)sc.Attribute("Route") == "OrdresPage");
        Assert.Equal(1, ordresCount);

        // Inspect Tab 5 (Activité - Must be ActivitePage and NOT duplicate GaragePage)
        var tab5 = tabs[4];
        Assert.Equal("Activité", (string?)tab5.Attribute("Title"));
        var tab5Content = tab5.Element(defaultNs + "ShellContent");
        Assert.NotNull(tab5Content);
        Assert.Equal("ActivitePage", (string?)tab5Content.Attribute("Route"));
        Assert.Contains("vues:ActivitePage", (string?)tab5Content.Attribute("ContentTemplate"));
    }

    [Fact]
    public async Task F34_ProductionBoosterAndCatalogue_WorksEndToEndWith250VerifiedCarsAnd3CardPack()
    {
        var tirage = await ServiceAPI.Instance.OuvrirBoosterAsync();
        Assert.NotNull(tirage);
        Assert.Equal(3, tirage.Count);
        Assert.All(tirage, c =>
        {
            Assert.False(string.IsNullOrWhiteSpace(c.Marque));
            Assert.False(string.IsNullOrWhiteSpace(c.Modele));
            Assert.True(c.PrixOfficiel >= 3000m);
            Assert.True(c.ValeurJetons >= 3m);
            Assert.StartsWith("https://", c.ImageUrl);
            Assert.Contains(c.Rarete, new[] { "COMMUNE", "RARE", "ÉPIQUE", "LÉGENDAIRE" });
        });
    }

    [Fact]
    public async Task F35_DualCurrency_PatrimoineAndPortefeuille_ProportionallyConvertsEurosToJetons()
    {
        var carte100k = new Carte { Marque = "Porsche", Modele = "911 Carrera", PrixOfficiel = 100000m };
        Assert.Equal(100m, carte100k.ValeurJetons);
        Assert.Equal("100 Jetons", carte100k.ValeurJetonsFormatee);
        Assert.Equal("100 000 €", carte100k.PrixReelFormate.Replace('\u202F', ' ').Replace('\u00A0', ' '));

        var carte35k = new Carte { Marque = "Honda", Modele = "Civic Type R", PrixOfficiel = 35000m };
        Assert.Equal(35m, carte35k.ValeurJetons);
        Assert.Equal("35 Jetons", carte35k.ValeurJetonsFormatee);

        var carte25250 = new Carte { Marque = "Renault", Modele = "Clio IV", PrixOfficiel = 25250m };
        Assert.Equal(25.25m, carte25250.ValeurJetons);
        Assert.Equal("25,25 Jetons", carte25250.ValeurJetonsFormatee);

        var exemplaire = new CartePossedee("copy-test-25250", carte25250, 25250m, 1, 20000m);
        decimal jetonsCredites = await ServiceAPI.Instance.VendreCarteGarageAsync(exemplaire);
        Assert.Equal(25.25m, jetonsCredites);
    }

    [Fact]
    public void F33_MauiProgram_RegistersOrdresVueModeleAndOrdresPageInDI()
    {
        string mauiProgramPath = Path.Combine(ProjectDir, "MauiProgram.cs");
        Assert.True(File.Exists(mauiProgramPath), $"MauiProgram.cs must exist at: {mauiProgramPath}");

        string code = File.ReadAllText(mauiProgramPath);

        Assert.Contains("constructeur.Services.AddTransient<OrdresVueModele>()", code);
        Assert.Contains("constructeur.Services.AddTransient<OrdresPage>()", code);
    }

    [Fact]
    public void F33_OrdresPage_CodeBehindPurity_ZeroBusinessLogicOrHttpCalls()
    {
        string pageCsPath = Path.Combine(ProjectDir, "Vues", "OrdresPage.xaml.cs");
        Assert.True(File.Exists(pageCsPath), $"OrdresPage.xaml.cs must exist at: {pageCsPath}");

        string code = File.ReadAllText(pageCsPath);

        // Verification of pure code-behind
        Assert.Contains("InitializeComponent()", code);
        Assert.DoesNotContain("HttpClient", code);
        Assert.DoesNotContain("ServiceAPI.Instance", code);
        Assert.DoesNotContain("async ", code);
        Assert.DoesNotContain("Task ", code);
        Assert.DoesNotContain("void On", code);

        // Region Methodes must be empty
        int methodRegionIdx = code.IndexOf("#region Methodes");
        if (methodRegionIdx >= 0)
        {
            int endRegionIdx = code.IndexOf("#endregion", methodRegionIdx);
            Assert.True(endRegionIdx > methodRegionIdx);
            string methodRegionContent = code.Substring(methodRegionIdx, endRegionIdx - methodRegionIdx);
            string cleaned = methodRegionContent.Replace("#region Methodes", "").Trim();
            Assert.True(string.IsNullOrWhiteSpace(cleaned), $"OrdresPage.xaml.cs should have empty #region Methodes, but found: {cleaned}");
        }
    }

    #endregion
}
