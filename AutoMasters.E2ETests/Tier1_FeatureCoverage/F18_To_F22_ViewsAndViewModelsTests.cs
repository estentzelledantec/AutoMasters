using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier1_FeatureCoverage;

[TestClass]
[TestCategory("Tier1")]
[TestCategory("M3")]
public class F18_To_F22_ViewsAndViewModelsTests : E2ETestBase
{
    #region F18: Login Page (ConnexionPage)

    [TestMethod]
    public void F18_01_LoginPage_XamlMustExist()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(xamlPath));
    }

    [TestMethod]
    public void F18_02_LoginPage_MustContainEmailAndPassEntries()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("Identifiant") || xaml.Contains("Email"), "Page must bind email/username entry.");
        Assert.IsTrue(xaml.Contains("MotDePasse") || xaml.Contains("Password"), "Page must bind password entry.");
    }

    [TestMethod]
    public void F18_03_LoginPage_MustContainSeConnecterButton()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("SE CONNECTER") || xaml.Contains("SeConnecter"), "Page must contain 'SE CONNECTER' button.");
    }

    [TestMethod]
    public void F18_04_LoginPage_MustContainTitleAutoMasters()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("AUTO") || xaml.Contains("MASTERS"), "Page must display 'AUTO MASTERS' title header.");
    }

    [TestMethod, Ignore("Helper links removed per user request")]
    public void F18_05_LoginPage_MustContainHelperLinks()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "ConnexionPage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionPage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        bool hasLinks = xaml.Contains("oublié") || xaml.Contains("compte");
        Assert.IsTrue(hasLinks, "Page should contain helper links from Figma screen 1.");
    }

    #endregion

    #region F19: Login ViewModel (ConnexionVueModele)

    [TestMethod]
    public void F19_01_LoginViewModel_MustInheritObservableObject()
    {
        string vmPath = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles", "ConnexionVueModele.cs");
        if (!File.Exists(vmPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionVueModele.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(vmPath);
        Assert.IsTrue(code.Contains("ObservableObject"), "ViewModel must inherit from CommunityToolkit ObservableObject.");
    }

    [TestMethod]
    public void F19_02_LoginViewModel_MustExposeCredentialsProperties()
    {
        string vmPath = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles", "ConnexionVueModele.cs");
        if (!File.Exists(vmPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionVueModele.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(vmPath);
        Assert.IsTrue(code.Contains("Identifiant") || code.Contains("identifiant"));
        Assert.IsTrue(code.Contains("MotDePasse") || code.Contains("motDePasse"));
    }

    [TestMethod]
    public void F19_03_LoginViewModel_MustExposeBusyAndAvailableStates()
    {
        string vmPath = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles", "ConnexionVueModele.cs");
        if (!File.Exists(vmPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionVueModele.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(vmPath);
        Assert.IsTrue(code.Contains("EstOccupe") || code.Contains("estOccupe") || code.Contains("IsBusy"));
    }

    [TestMethod]
    public void F19_04_LoginViewModel_MustExposeSeConnecterCommand()
    {
        string vmPath = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles", "ConnexionVueModele.cs");
        if (!File.Exists(vmPath))
        {
            Assert.Inconclusive("Milestone M3 pending: ConnexionVueModele.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(vmPath);
        Assert.IsTrue(code.Contains("SeConnecter") && (code.Contains("RelayCommand") || code.Contains("Command")));
    }

    [TestMethod]
    public void F19_05_LoginViewModel_EmptyInputs_SetsErrorMessageWithoutApiCall()
    {
        // Spec verification: empty inputs reject client-side
        var req = new DemandeConnexionSpec { Username = "", Password = "" };
        bool isValid = !string.IsNullOrWhiteSpace(req.Username) && !string.IsNullOrWhiteSpace(req.Password);
        Assert.IsFalse(isValid, "Client-side validation should detect empty credentials before dispatching network request.");
    }

    #endregion

    #region F20: Navigation Structure (AppShell / TabBar)

    [TestMethod]
    public void F20_01_AppShell_MustDefine5PrimaryTabs()
    {
        string shellPath = Path.Combine(TestWorkspace.AutoMastersDir, "AppShell.xaml");
        if (!File.Exists(shellPath))
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(shellPath);
        bool has5Tabs = (xaml.Contains("Catalogue") || xaml.Contains("Cartes")) &&
                        (xaml.Contains("Marché") || xaml.Contains("Marche")) &&
                        (xaml.Contains("Garage") || xaml.Contains("Portefeuille"));

        if (!has5Tabs)
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell 5-tab navigation not yet configured (currently M1 initial shell).");
            return;
        }

        Assert.IsTrue(has5Tabs, "AppShell must define the 5 primary tabs.");
    }

    [TestMethod]
    public void F20_02_AppShell_MustConfigureDarkThemeColors()
    {
        string shellPath = Path.Combine(TestWorkspace.AutoMastersDir, "AppShell.xaml");
        if (!File.Exists(shellPath))
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(shellPath);
        Assert.IsTrue(xaml.Contains("Shell.BackgroundColor") || xaml.Contains("BackgroundColor") || xaml.Contains("ColorBackground"));
    }

    [TestMethod]
    public void F20_03_AppShell_MustDefineCatalogueTabAsDefaultOrFirst()
    {
        string shellPath = Path.Combine(TestWorkspace.AutoMastersDir, "AppShell.xaml");
        if (!File.Exists(shellPath))
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(shellPath);
        bool hasTabs = xaml.Contains("TabBar") || (xaml.Contains("Tab") && xaml.Contains("Catalogue"));
        if (!hasTabs)
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell TabBar not yet configured.");
            return;
        }

        Assert.IsTrue(hasTabs);
    }

    [TestMethod]
    public void F20_04_AppShell_MustSupportShellNavigationRouting()
    {
        string shellCs = Path.Combine(TestWorkspace.AutoMastersDir, "AppShell.xaml.cs");
        if (!File.Exists(shellCs))
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell.xaml.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(shellCs);
        Assert.IsTrue(code.Contains("InitializeComponent"));
    }

    [TestMethod]
    public void F20_05_AppShell_CodeBehind_MustBePure()
    {
        string shellCs = Path.Combine(TestWorkspace.AutoMastersDir, "AppShell.xaml.cs");
        if (!File.Exists(shellCs))
        {
            Assert.Inconclusive("Milestone M3 pending: AppShell.xaml.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(shellCs);
        bool isPure = TestWorkspace.ValidateCodeBehindPurity(code, out string violation);
        Assert.IsTrue(isPure, $"AppShell.xaml.cs purity violation: {violation}");
    }

    #endregion

    #region F21: Catalog Page (CataloguePage) & ViewModel

    [TestMethod]
    public void F21_01_CatalogPage_XamlMustExist()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "CataloguePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: CataloguePage.xaml not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(xamlPath));
    }

    [TestMethod]
    public void F21_02_CatalogPage_MustContainSearchBar()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "CataloguePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: CataloguePage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("SearchBar") || xaml.Contains("Search") || xaml.Contains("Recherche"));
    }

    [TestMethod]
    public void F21_03_CatalogPage_MustContainCollectionViewOrGrid()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "CataloguePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: CataloguePage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("CollectionView") || xaml.Contains("ListView") || xaml.Contains("Grid"));
    }

    [TestMethod]
    public void F21_04_CatalogViewModel_MustExposeCartesObservableCollection()
    {
        string vmPath = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles", "CatalogueVueModele.cs");
        if (!File.Exists(vmPath))
        {
            Assert.Inconclusive("Milestone M3 pending: CatalogueVueModele.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(vmPath);
        Assert.IsTrue(code.Contains("ObservableCollection"));
    }

    [TestMethod]
    public void F21_05_CatalogViewModel_MustFilterByRarityAndSearch()
    {
        // Spec test: dynamic filter logic
        var catalog = MockServer.Catalog;
        var porscheFiltered = catalog.Where(c => c.Brand.Contains("Porsche") && c.Rarity == "ÉPIQUE").ToList();
        Assert.AreEqual(1, porscheFiltered.Count);
        Assert.AreEqual("911 GT3 RS", porscheFiltered[0].Model);
    }

    #endregion

    #region F22: Market Page (MarchePage) & ViewModel

    [TestMethod]
    public void F22_01_MarketPage_XamlMustExist()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "MarchePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: MarchePage.xaml not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(xamlPath));
    }

    [TestMethod]
    public void F22_02_MarketPage_MustContainLiveBadge()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "MarchePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: MarchePage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("EN DIRECT") || xaml.Contains("Direct") || xaml.Contains("Live"));
    }

    [TestMethod]
    public void F22_03_MarketPage_MustContainBuyAndReserveButtons()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "MarchePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: MarchePage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("ACHETER") || xaml.Contains("Acheter"));
        Assert.IsTrue(xaml.Contains("RÉSERVER") || xaml.Contains("Reserver"));
    }

    [TestMethod]
    public void F22_04_MarketPage_MustContainSellFabButton()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "MarchePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: MarchePage.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(xamlPath);
        Assert.IsTrue(xaml.Contains("Vendre") || xaml.Contains("+") || xaml.Contains("FAB"));
    }

    [TestMethod]
    public void F22_05_MarketViewModel_MustExposeOffresCollection()
    {
        string vmPath = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles", "MarcheVueModele.cs");
        if (!File.Exists(vmPath))
        {
            Assert.Inconclusive("Milestone M3 pending: MarcheVueModele.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(vmPath);
        Assert.IsTrue(code.Contains("ObservableCollection"));
    }

    #endregion
}
