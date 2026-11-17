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
using Xunit;

namespace AutoMasters.Tests;

/// <summary>
/// Empirical Challenger verification tests for Milestone 3 (F18-F28).
/// Stress-tests code-behind purity, authentication states, error banner, credential preservation,
/// and Shell routing invariants.
/// </summary>
public class ChallengerM3VerificationTests
{
    private static string AutoMastersProjectDir
    {
        get
        {
            string current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                string candidate = Path.Combine(current, "AutoMasters");
                if (Directory.Exists(candidate) && File.Exists(Path.Combine(candidate, "AutoMasters.csproj")))
                {
                    return candidate;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            return @"c:\Users\stent\source\repos\AutoMasters\AutoMasters";
        }
    }

    #region 1. Empirical Code-Behind Purity Tests

    [Fact]
    public void Challenger_CodeBehind_AllXamlCs_ZeroHttpClientOrServiceApiInstance()
    {
        string projectDir = AutoMastersProjectDir;
        var xamlCsFiles = Directory.GetFiles(projectDir, "*.xaml.cs", SearchOption.AllDirectories)
            .Where(f => !f.Contains("obj") && !f.Contains("bin"))
            .ToList();

        Assert.NotEmpty(xamlCsFiles);

        foreach (var file in xamlCsFiles)
        {
            string content = File.ReadAllText(file);
            string fileName = Path.GetFileName(file);

            Assert.DoesNotContain("HttpClient", content);
            Assert.DoesNotContain("ServiceAPI.Instance", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("ServiceApi.Instance", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("new ServiceAPI", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("new ServiceApi", content, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("Task.Run", content);
            Assert.DoesNotContain("JsonConvert", content);
        }
    }

    [Fact]
    public void Challenger_CodeBehind_Vues_HaveZeroBusinessMethodsAndOnlyConstructors()
    {
        string vuesDir = Path.Combine(AutoMastersProjectDir, "Vues");
        var viewCsFiles = Directory.GetFiles(vuesDir, "*.xaml.cs", SearchOption.TopDirectoryOnly);

        Assert.True(viewCsFiles.Length >= 6, $"Expected at least 6 view code-behind files, found {viewCsFiles.Length}");

        foreach (var file in viewCsFiles)
        {
            string content = File.ReadAllText(file);
            string fileName = Path.GetFileName(file);

            // Must contain InitializeComponent
            Assert.Contains("InitializeComponent()", content);

            // Must NOT contain async methods or task returns
            Assert.DoesNotContain("async ", content);
            Assert.DoesNotContain("Task ", content);
            Assert.DoesNotContain("void On", content); // no UI event handlers in code-behind

            // In BTS SIO convention, region Methodes should be empty in pure code-behind
            int methodRegionIdx = content.IndexOf("#region Methodes");
            if (methodRegionIdx >= 0)
            {
                int endRegionIdx = content.IndexOf("#endregion", methodRegionIdx);
                Assert.True(endRegionIdx > methodRegionIdx);
                string methodRegionContent = content.Substring(methodRegionIdx, endRegionIdx - methodRegionIdx);
                string cleaned = methodRegionContent.Replace("#region Methodes", "").Trim();
                Assert.True(string.IsNullOrWhiteSpace(cleaned), $"View {fileName} should have empty #region Methodes, but found: {cleaned}");
            }
        }
    }

    [Fact]
    public void Challenger_CodeBehind_AppShell_OnlyContainsRouteRegistration()
    {
        string appShellCs = Path.Combine(AutoMastersProjectDir, "AppShell.xaml.cs");
        Assert.True(File.Exists(appShellCs));

        string content = File.ReadAllText(appShellCs);
        Assert.Contains("InitializeComponent()", content);
        Assert.Contains("Routing.RegisterRoute", content);
        Assert.DoesNotContain("HttpClient", content);
        Assert.DoesNotContain("ServiceAPI.Instance", content);
        Assert.DoesNotContain("async ", content);
    }

    #endregion

    #region 2. Empirical Navigation Flow & Authentication Tests

    [Fact]
    public async Task Challenger_Login_ValidCredentials_ClearsPassword_PreservesEmail_ClearsError()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
        {
            accessToken = "jwt.valid.token",
            refreshToken = "refresh.token",
            expiresAt = DateTime.UtcNow.AddHours(2)
        });

        var serviceApi = new ServiceAPI(mockHandler);
        var vm = new ConnexionVueModele(serviceApi)
        {
            Email = "etudiant@sio.lan",
            MotDePasse = "SuperSecret123!"
        };

        await vm.SeConnecterCommand.ExecuteAsync(null);

        // Mot de passe cleared for security
        Assert.Equal(string.Empty, vm.MotDePasse);
        // Email retained
        Assert.Equal("etudiant@sio.lan", vm.Email);
        // No error banner
        Assert.Equal(string.Empty, vm.Message);
        Assert.False(vm.AUnMessage);
        // Not busy
        Assert.False(vm.EstOccupe);
        Assert.True(vm.EstDisponible);
    }

    [Fact]
    public async Task Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandPost("/auth/login", HttpStatusCode.Unauthorized, new
        {
            code = "INVALID_CREDENTIALS",
            message = "Identifiants invalides."
        });

        var serviceApi = new ServiceAPI(mockHandler);
        var vm = new ConnexionVueModele(serviceApi)
        {
            Email = "etudiant@sio.lan",
            MotDePasse = "WrongPassword999!"
        };

        await vm.SeConnecterCommand.ExecuteAsync(null);

        // Password must be cleared
        Assert.Equal(string.Empty, vm.MotDePasse);
        // Email must be strictly preserved
        Assert.Equal("etudiant@sio.lan", vm.Email);
        // Error banner must be visible and inform about invalid credentials
        Assert.True(vm.AUnMessage);
        Assert.Contains("Identifiants invalides", vm.Message);
        // Not busy
        Assert.False(vm.EstOccupe);
        Assert.True(vm.EstDisponible);
    }

    [Fact]
    public async Task Challenger_Login_EmptyEmailOrPassword_BlocksLocallyWithoutNetwork()
    {
        var mockHandler = new MockHttpMessageHandler();
        var serviceApi = new ServiceAPI(mockHandler);

        // Case A: Empty email
        var vm1 = new ConnexionVueModele(serviceApi)
        {
            Email = "",
            MotDePasse = "Pass123"
        };
        await vm1.SeConnecterCommand.ExecuteAsync(null);
        Assert.Null(mockHandler.DerniereRequete);
        Assert.True(vm1.AUnMessage);
        Assert.Contains("Veuillez saisir votre identifiant et votre mot de passe", vm1.Message);

        // Case B: Empty password
        var vm2 = new ConnexionVueModele(serviceApi)
        {
            Email = "test@sio.lan",
            MotDePasse = "   "
        };
        await vm2.SeConnecterCommand.ExecuteAsync(null);
        Assert.Null(mockHandler.DerniereRequete);
        Assert.True(vm2.AUnMessage);
    }

    [Theory(Skip="Validation email retirée au profit de l'identifiant")]
    [InlineData("not-an-email")]
    [InlineData("etudiant@")]
    [InlineData("etudiant.lan")]
    [InlineData("plainaddress")]
    public async Task Challenger_Login_InvalidEmailFormat_BlocksLocallyAndPreservesInput(string badEmail)
    {
        var mockHandler = new MockHttpMessageHandler();
        var serviceApi = new ServiceAPI(mockHandler);

        var vm = new ConnexionVueModele(serviceApi)
        {
            Email = badEmail,
            MotDePasse = "Pass123"
        };

        await vm.SeConnecterCommand.ExecuteAsync(null);

        // No network request
        Assert.Null(mockHandler.DerniereRequete);
        // Error banner set
        Assert.True(vm.AUnMessage);
        Assert.Contains("adresse email valide", vm.Message);
        // Email preserved
        Assert.Equal(badEmail, vm.Email);
    }

    [Fact]
    public async Task Challenger_Login_NetworkUnavailable_HandlesGracefullyWithBanner()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.SimulerPanneReseau = true;

        var serviceApi = new ServiceAPI(mockHandler);
        var vm = new ConnexionVueModele(serviceApi)
        {
            Email = "etudiant@sio.lan",
            MotDePasse = "Pass123"
        };

        await vm.SeConnecterCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, vm.MotDePasse);
        Assert.Equal("etudiant@sio.lan", vm.Email);
        Assert.True(vm.AUnMessage);
        Assert.Contains("serveur", vm.Message, StringComparison.OrdinalIgnoreCase);
        Assert.False(vm.EstOccupe);
    }

    [Fact]
    public async Task Challenger_Login_RapidSubsequentExecution_BlockedWhenBusy()
    {
        var mockHandler = new MockHttpMessageHandler();
        mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
        {
            accessToken = "token123",
            expiresAt = DateTime.UtcNow.AddHours(2)
        });

        var serviceApi = new ServiceAPI(mockHandler);
        var vm = new ConnexionVueModele(serviceApi)
        {
            Email = "etudiant@sio.lan",
            MotDePasse = "Pass123"
        };

        // If manually setting EstOccupe = true (as during in-flight network call)
        vm.EstOccupe = true;
        await vm.SeConnecterCommand.ExecuteAsync(null);

        // Should return immediately without sending any request
        Assert.Null(mockHandler.DerniereRequete);
    }

    #endregion

    #region 3. Empirical AppShell & Routing Invariants Tests

    [Fact]
    public void Challenger_AppShell_XmlHierarchy_DefinesUnauthenticatedRootAndMainTabs()
    {
        string shellPath = Path.Combine(AutoMastersProjectDir, "AppShell.xaml");
        string xaml = File.ReadAllText(shellPath);

        XDocument doc = XDocument.Parse(xaml);
        XNamespace defaultNs = doc.Root?.GetDefaultNamespace() ?? XNamespace.None;

        // Root content must be ConnexionPage
        var shellContents = doc.Descendants(defaultNs + "ShellContent").ToList();
        Assert.NotEmpty(shellContents);
        var rootContent = shellContents.First();
        Assert.Equal("ConnexionPage", (string?)rootContent.Attribute("Route"));

        // TabBar must be MainTabs
        var tabBars = doc.Descendants(defaultNs + "TabBar").ToList();
        Assert.Single(tabBars);
        var mainTabBar = tabBars.First();
        Assert.Equal("MainTabs", (string?)mainTabBar.Attribute("Route"));

        // Tabs inside TabBar
        var tabs = mainTabBar.Elements(defaultNs + "Tab").ToList();
        Assert.Equal(5, tabs.Count);

        var tabRoutes = tabs.Select(t => (string?)t.Attribute("Route")).ToList();
        Assert.Equal("Catalogue", tabRoutes[0]);
        Assert.Equal("Marche", tabRoutes[1]);
        Assert.Equal("Ordres", tabRoutes[2]);
        Assert.Equal("Garage", tabRoutes[3]);
        Assert.Equal("Activite", tabRoutes[4]);
    }

    [Fact]
    public void Challenger_LoginViewModel_NavigatesToMainTabsCatalogue()
    {
        string vmPath = Path.Combine(AutoMastersProjectDir, "VuesModeles", "ConnexionVueModele.cs");
        string code = File.ReadAllText(vmPath);

        // Must explicitly route to //MainTabs/Catalogue on successful login
        Assert.Contains("\"//MainTabs/Catalogue\"", code);
    }

    [Fact]
    public void Challenger_SecondaryRoutes_RegisteredInAppShellCodeBehind()
    {
        string appShellCs = Path.Combine(AutoMastersProjectDir, "AppShell.xaml.cs");
        string code = File.ReadAllText(appShellCs);

        // FicheCartePage and CreationOffrePage must be registered
        Assert.Contains("Routing.RegisterRoute(nameof(FicheCartePage), typeof(FicheCartePage))", code);
        Assert.Contains("Routing.RegisterRoute(nameof(CreationOffrePage), typeof(CreationOffrePage))", code);
    }

    #endregion

    #region 4. Empirical Booster Idempotency Multi-Click Stress Tests

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(50)]
    public async Task Challenger_Booster_Adversarial_RapidMultiClickOnFinish_EventFiredCountStrictlyOne(int extraClicks)
    {
        var mockHandler = new MockHttpMessageHandler();
        var cartesServeur = Enumerable.Range(1, 5).Select(i => new Carte
        {
            Identifiant = $"c{i}",
            Marque = $"Brand_{i}",
            Modele = $"Model_{i}",
            Rarete = "COMMUNE"
        }).ToList();

        mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = cartesServeur });

        var service = new ServiceAPI(mockHandler);
        var boosterVm = new BoosterVueModele(service);
        await boosterVm.DemarrerOuvertureAsync();

        int eventFiredCount = 0;
        boosterVm.SurBoosterTermine += () => eventFiredCount++;

        // 9 clics pour reveler les 4 premieres cartes et la face recto de la 5eme
        for (int i = 0; i < 9; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        Assert.True(boosterVm.EstDerniereCarte);
        Assert.Equal("TERMINER & AJOUTER AU GARAGE", boosterVm.LibelleBoutonAction);

        // 10eme clic : finalisation legitime
        boosterVm.ActionCarteSuivante();
        Assert.True(boosterVm.EstTermine);
        Assert.Equal(1, eventFiredCount);

        // Clics rapides intempestifs consecutifs (1, 3, 5, 10, 50 taps)
        for (int i = 0; i < extraClicks; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        // Invariant contradictoire : l'evenement ne doit JAMAIS etre declenche plus d'une fois
        Assert.Equal(1, eventFiredCount);
        Assert.True(boosterVm.EstTermine);
    }

    [Theory]
    [InlineData(3)]
    [InlineData(5)]
    [InlineData(10)]
    public async Task Challenger_Garage_Booster_MultiClickOnFinish_DecrementsQuotaOnlyOnce(int extraClicks)
    {
        var mockHandler = new MockHttpMessageHandler();
        var cartesServeur = Enumerable.Range(1, 5).Select(i => new Carte
        {
            Identifiant = $"c{i}",
            Marque = $"Brand_{i}",
            Modele = $"Model_{i}",
            Rarete = "COMMUNE"
        }).ToList();

        mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, new Portefeuille());
        mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = cartesServeur });

        var service = new ServiceAPI(mockHandler);
        var garageVm = new GarageVueModele(service);
        garageVm.PaquetsDisponibles = 3;

        await garageVm.OuvrirBoosterCommand.ExecuteAsync(null);

        Assert.True(garageVm.EstBoosterModalOuvert);
        Assert.NotNull(garageVm.BoosterVueModele);

        var boosterVm = garageVm.BoosterVueModele;

        // Revelation des 5 cartes (9 clics)
        for (int i = 0; i < 9; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        // Clic de terminaison (10eme)
        boosterVm.ActionCarteSuivante();

        // Clics repetes apres terminaison (3, 5, 10 taps)
        for (int i = 0; i < extraClicks; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        // Le quota doit avoir ete decremente d'exactement 1 (3 -> 2)
        Assert.Equal(2, garageVm.PaquetsDisponibles);
        Assert.False(garageVm.EstBoosterModalOuvert);
    }

    [Fact]
    public async Task Challenger_Booster_Adversarial_ConcurrentClicksOnFinish_ThreadSafeIdempotency()
    {
        var mockHandler = new MockHttpMessageHandler();
        var cartesServeur = Enumerable.Range(1, 5).Select(i => new Carte
        {
            Identifiant = $"c{i}",
            Marque = $"Brand_{i}",
            Modele = $"Model_{i}",
            Rarete = "COMMUNE"
        }).ToList();

        mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, new { cards = cartesServeur });

        var service = new ServiceAPI(mockHandler);
        var boosterVm = new BoosterVueModele(service);
        await boosterVm.DemarrerOuvertureAsync();

        int eventFiredCount = 0;
        boosterVm.SurBoosterTermine += () => System.Threading.Interlocked.Increment(ref eventFiredCount);

        for (int i = 0; i < 9; i++)
        {
            boosterVm.ActionCarteSuivante();
        }

        // Lancement concurrent de 20 appels simultanes sur le bouton final
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() => boosterVm.ActionCarteSuivante())).ToArray();
        await Task.WhenAll(tasks);

        Assert.True(boosterVm.EstTermine);
        Assert.Equal(1, eventFiredCount);
    }

    #endregion
}
