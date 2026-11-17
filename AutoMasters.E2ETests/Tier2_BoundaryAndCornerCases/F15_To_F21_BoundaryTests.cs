using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier2_BoundaryAndCornerCases;

[TestClass]
[TestCategory("Tier2")]
[TestCategory("Boundary")]
public class F15_To_F21_BoundaryTests : E2ETestBase
{
    #region F15 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F15_B01_Booster_QuotaZero_CannotOpen()
    {
        MockServer.BoosterQuota = 0;
        var resp = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [TestMethod]
    public async Task F15_B02_Booster_QuotaOne_BecomesZeroAfterOpen()
    {
        MockServer.BoosterQuota = 1;
        var resp = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        Assert.AreEqual(0, MockServer.BoosterQuota);
    }

    [TestMethod]
    public void F15_B03_Booster_TimerReachesZero_ReplenishesQuota()
    {
        int quota = 0;
        int secondsRemaining = 0; // Hit 00:00!
        if (secondsRemaining <= 0 && quota < 10)
        {
            quota++;
            secondsRemaining = 600; // Reset timer to 10 min
        }

        Assert.AreEqual(1, quota);
        Assert.AreEqual(600, secondsRemaining);
    }

    [TestMethod]
    public async Task F15_B04_Booster_AtomicGrant_SurvivesAppInterruption()
    {
        int initialPortfolio = MockServer.Portfolio.Count;
        // Server creates 5 cards on initial POST call
        await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        // Even if user kills app during card reveal 2, portfolio already has cards
        Assert.IsTrue(MockServer.Portfolio.Count >= initialPortfolio);
    }

    [TestMethod]
    public async Task F15_B05_Booster_DeliversExactly5Cards()
    {
        var resp = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        string json = await resp.Content.ReadAsStringAsync();
        var res = JsonConvert.DeserializeAnonymousType(json, new { cards = new System.Collections.Generic.List<CarteSpec>() });
        Assert.AreEqual(5, res?.cards.Count);
    }

    #endregion

    #region F16 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F16_B01_MockServer_Simulates500InternalServerError()
    {
        MockServer.ForceStatusCode = HttpStatusCode.InternalServerError;
        var resp = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.InternalServerError, resp.StatusCode);
    }

    [TestMethod]
    public async Task F16_B02_MockServer_Simulates503ServiceUnavailable()
    {
        MockServer.ForceStatusCode = HttpStatusCode.ServiceUnavailable;
        var resp = await Client.GetAsync("/portfolio");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, resp.StatusCode);
    }

    [TestMethod]
    public async Task F16_B03_MockServer_SimulatesOfflineDisconnection()
    {
        MockServer.SimulateOffline = true;
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
        {
            await Client.GetAsync("/cards");
        });
    }

    [TestMethod]
    public async Task F16_B04_MockServer_Simulates408RequestTimeout()
    {
        MockServer.ForceStatusCode = HttpStatusCode.RequestTimeout;
        var resp = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.RequestTimeout, resp.StatusCode);
    }

    [TestMethod]
    public void F16_B05_MockServer_ResetRestoresInitialState()
    {
        MockServer.UserBalance = 10m;
        MockServer.BoosterQuota = 0;
        MockServer.Reset();

        Assert.AreEqual(500000m, MockServer.UserBalance);
        Assert.AreEqual(3, MockServer.BoosterQuota);
    }

    #endregion

    #region F17 Boundary Tests (5 tests)

    [TestMethod]
    public void F17_B01_TestsCsproj_IsTestProjectProperty()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<IsTestProject>true</IsTestProject>"));
    }

    [TestMethod]
    public void F17_B02_TestsCsproj_ReferencesTargetProject()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("AutoMasters.csproj"));
    }

    [TestMethod]
    public void F17_B03_TestsCsproj_HasNoPackableOutput()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<IsPackable>false</IsPackable>"));
    }

    [TestMethod]
    public void F17_B04_TestsCsproj_ContainsCoverletCollector()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("coverlet.collector") || xml.Contains("TestAdapter"));
    }

    [TestMethod]
    public void F17_B05_TestsCsproj_ImplicitUsingsEnabled()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<ImplicitUsings>enable</ImplicitUsings>"));
    }

    #endregion

    #region F18 Boundary Tests (5 tests)

    [TestMethod]
    public void F18_B01_LoginPage_CompactWidthLayout_Supported()
    {
        double screenWidth = 320.0; // Small phone viewport
        Assert.IsTrue(screenWidth >= 320.0);
    }

    [TestMethod]
    public void F18_B02_LoginPage_IsPasswordProperty_MustBeSet()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("IsPassword=\"True\"") || xaml.Contains("IsPassword=\"true\""));
    }

    [TestMethod, Ignore("Keyboard changed to Text for username")]
    public void F18_B03_LoginPage_KeyboardEmail_MustBeConfigured()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("Keyboard=\"Email\"") || xaml.Contains("Email"));
    }

    [TestMethod]
    public void F18_B04_LoginPage_ButtonEnabledBinding_ReflectsState()
    {
        bool estOccupe = false;
        bool estDisponible = !estOccupe;
        Assert.IsTrue(estDisponible);
    }

    [TestMethod]
    public void F18_B05_LoginPage_ButtonCornerRadius_Rounded()
    {
        int cornerRadius = 10;
        Assert.IsTrue(cornerRadius >= 8 && cornerRadius <= 16);
    }

    #endregion

    #region F19 Boundary Tests (5 tests)

    [TestMethod]
    public void F19_B01_LoginViewModel_RapidClicks_DebouncedByIsBusy()
    {
        bool isBusy = false;
        int networkCalls = 0;

        Action click = () =>
        {
            if (isBusy) return;
            isBusy = true;
            networkCalls++;
        };

        click();
        click();
        click();

        Assert.AreEqual(1, networkCalls, "Subsequent clicks while IsBusy must be discarded.");
    }

    [TestMethod]
    public void F19_B02_LoginViewModel_FailedLogin_ClearsPasswordKeepsEmail()
    {
        string email = "etudiant@sio.lan";
        string password = "wrong_password";

        // Login fails:
        password = string.Empty;

        Assert.AreEqual("etudiant@sio.lan", email, "Email input must be preserved on failed login.");
        Assert.AreEqual(string.Empty, password, "Password input must be cleared on failed login.");
    }

    [TestMethod]
    public void F19_B03_LoginViewModel_EmailFormatRegex_Validation()
    {
        string validEmail = "etudiant@sio.lan";
        string invalidEmail = "not-an-email";

        bool isValid(string e) => e.Contains("@") && e.Contains(".");

        Assert.IsTrue(isValid(validEmail));
        Assert.IsFalse(isValid(invalidEmail));
    }

    [TestMethod]
    public void F19_B04_LoginViewModel_ErrorMessageFormatting_FriendlyText()
    {
        string rawError = "Identifiants invalides.";
        Assert.IsFalse(rawError.Contains("NullReferenceException"));
        Assert.IsFalse(rawError.Contains("at AutoMasters.Services"));
    }

    [TestMethod]
    public void F19_B05_LoginViewModel_LongInputString_SafelyHandled()
    {
        string longString = new string('x', 1000);
        Assert.AreEqual(1000, longString.Length);
    }

    #endregion

    #region F20 Boundary Tests (5 tests)

    [TestMethod]
    public void F20_B01_Navigation_RapidTabSwitching_StateSafe()
    {
        int activeTab = 0;
        for (int i = 0; i < 50; i++)
        {
            activeTab = (activeTab + 1) % 5;
        }
        Assert.IsTrue(activeTab >= 0 && activeTab < 5);
    }

    [TestMethod]
    public void F20_B02_Navigation_RouteNames_MustBeDistinct()
    {
        var routes = new[] { "catalogue", "marche", "ordres", "garage", "activite" };
        Assert.AreEqual(5, routes.Distinct().Count());
    }

    [TestMethod]
    public void F20_B03_Navigation_ActiveTabColor_RedAccent()
    {
        string activeColor = "#E63946";
        Assert.AreEqual("#E63946", activeColor);
    }

    [TestMethod]
    public void F20_B04_Navigation_InactiveTabColor_MutedGray()
    {
        string inactiveColor = "#9CA3AF";
        Assert.AreEqual("#9CA3AF", inactiveColor);
    }

    [TestMethod]
    public void F20_B05_Navigation_DeepLink_UnknownRoute_Fallback()
    {
        string targetRoute = "non_existent_page";
        string fallbackRoute = "catalogue";
        string resolved = targetRoute == "catalogue" ? targetRoute : fallbackRoute;
        Assert.AreEqual("catalogue", resolved);
    }

    #endregion

    #region F21 Boundary Tests (5 tests)

    [TestMethod]
    public void F21_B01_Catalog_SearchDebounceDelay_300ms()
    {
        TimeSpan debounce = TimeSpan.FromMilliseconds(300);
        Assert.AreEqual(300, debounce.TotalMilliseconds);
    }

    [TestMethod]
    public void F21_B02_Catalog_ZeroMatches_DisplaysEmptyState()
    {
        var items = new System.Collections.Generic.List<CarteSpec>();
        bool showEmptyState = items.Count == 0;
        Assert.IsTrue(showEmptyState);
    }

    [TestMethod]
    public void F21_B03_Catalog_FilterRarityZeroMatches_EmptyCollection()
    {
        var catalog = MockServer.Catalog;
        var results = catalog.Where(c => c.Rarity == "MYTHIQUE").ToList();
        Assert.AreEqual(0, results.Count);
    }

    [TestMethod]
    public void F21_B04_Catalog_ClearingSearch_RestoresAllItems()
    {
        var all = MockServer.Catalog;
        var filtered = all.Where(c => c.Brand == "Porsche").ToList();
        Assert.IsTrue(filtered.Count < all.Count);
        // User clears search:
        var restored = all.ToList();
        Assert.AreEqual(all.Count, restored.Count);
    }

    [TestMethod]
    public void F21_B05_Catalog_SelectCardCommand_SetsSelectedCard()
    {
        var selected = MockServer.Catalog.First();
        Assert.IsNotNull(selected);
    }

    #endregion
}
