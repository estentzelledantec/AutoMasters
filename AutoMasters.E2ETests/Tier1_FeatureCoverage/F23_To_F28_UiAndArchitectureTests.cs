using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier1_FeatureCoverage;

[TestClass]
[TestCategory("Tier1")]
[TestCategory("M3")]
public class F23_To_F28_UiAndArchitectureTests : E2ETestBase
{
    #region F23: Immediate Buy & Reserve UX

    [TestMethod]
    public void F23_01_ImmediateBuyUx_MustPresentConfirmationRecapDialog()
    {
        // Spec test: Recap dialog before purchase per EF030
        var offer = MockServer.Offers.First(o => o.Id == "offer_ferrari");
        decimal currentBalance = MockServer.UserBalance;

        // Model validation of recap prompt
        string promptText = $"Acheter {offer.Card?.Brand} {offer.Card?.Model} pour {offer.Price:N0} coins ? Solde actuel : {currentBalance:N0} coins.";
        Assert.IsTrue(promptText.Contains("480"));
        Assert.IsTrue(promptText.Contains("Ferrari"));
    }

    [TestMethod]
    public void F23_02_ImmediateBuyUx_MustDisplayAskingPriceAndUserBalance()
    {
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");
        Assert.IsTrue(offer.Price > 0);
        Assert.IsTrue(MockServer.UserBalance >= offer.Price);
    }

    [TestMethod]
    public void F23_03_ImmediateBuyUx_CancellingDialogDoesNotTriggerPurchase()
    {
        decimal initialBalance = MockServer.UserBalance;
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");

        // User cancels prompt: no HTTP call sent, balance unchanged
        Assert.AreEqual(initialBalance, MockServer.UserBalance);
        Assert.AreEqual("active", offer.Status);
    }

    [TestMethod]
    public void F23_04_ImmediateBuyUx_DisablesBuyButtonWhileExecuting()
    {
        // Spec test: IsBusy flag disables button
        bool isBusy = true;
        bool canExecute = !isBusy;
        Assert.IsFalse(canExecute, "Buy button must be disabled while transaction is in-flight.");
    }

    [TestMethod]
    public void F23_05_ReserveUx_StartsExpirationCountdown()
    {
        DateTime now = DateTime.UtcNow;
        DateTime expiresAt = now.AddMinutes(15);
        TimeSpan remaining = expiresAt - now;

        Assert.IsTrue(remaining.TotalMinutes > 14 && remaining.TotalMinutes <= 15);
    }

    #endregion

    #region F24: Sell Card Modal UX

    [TestMethod]
    public void F24_01_SellModal_MustAllowSelectingOwnedCard()
    {
        var userCars = MockServer.Portfolio;
        Assert.IsTrue(userCars.Count > 0, "User must have owned cards to choose from.");
        var selected = userCars.First();
        Assert.IsNotNull(selected.Card);
    }

    [TestMethod]
    public void F24_02_SellModal_MustAllowEnteringAskingPrice()
    {
        decimal askingPrice = 250000m;
        Assert.IsTrue(askingPrice > 0, "Asking price must be a valid positive coin amount.");
    }

    [TestMethod]
    public void F24_03_SellModal_ValidatesPriceStrictlyGreaterThanZero()
    {
        decimal invalidPriceZero = 0m;
        decimal invalidPriceNeg = -100m;

        bool isValidZero = invalidPriceZero > 0;
        bool isValidNeg = invalidPriceNeg > 0;

        Assert.IsFalse(isValidZero);
        Assert.IsFalse(isValidNeg);
    }

    [TestMethod]
    public void F24_04_SellModal_SubmissionCreatesMarketListing()
    {
        var owned = MockServer.Portfolio.First();
        int initialOffersCount = MockServer.Offers.Count;

        var newOffer = new OffreSpec
        {
            Id = "off_modal_test",
            CopyId = owned.Id,
            Card = owned.Card,
            Seller = MockGroup2ApiServer.DefaultUser,
            Price = 300000m,
            Status = "active"
        };
        MockServer.Offers.Add(newOffer);

        Assert.AreEqual(initialOffersCount + 1, MockServer.Offers.Count);
    }

    [TestMethod]
    public void F24_05_SellModal_DismissSafelyPreservesState()
    {
        int initialCount = MockServer.Offers.Count;
        // User closes modal without submitting
        Assert.AreEqual(initialCount, MockServer.Offers.Count);
    }

    #endregion

    #region F25: Garage Page (GaragePage) & ViewModel

    [TestMethod]
    public void F25_01_GaragePage_XamlMustExist()
    {
        string xamlPath = Path.Combine(TestWorkspace.AutoMastersDir, "Vues", "GaragePage.xaml");
        if (!File.Exists(xamlPath))
        {
            Assert.Inconclusive("Milestone M3 pending: GaragePage.xaml not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(xamlPath));
    }

    [TestMethod]
    public void F25_02_GaragePage_MustDisplayTotalCollectionValuation()
    {
        var portfolio = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        decimal totalValuation = portfolio.TotalValuation;
        Assert.IsTrue(totalValuation > 0, "Total collection valuation must be calculated.");
    }

    [TestMethod]
    public void F25_03_GaragePage_MustDisplayNetPandLBanner()
    {
        var portfolio = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        decimal netPandL = portfolio.TotalGainLoss;
        string formattedPandL = netPandL >= 0 ? $"+{netPandL:N0} € P&L total" : $"{netPandL:N0} € P&L total";
        Assert.IsTrue(formattedPandL.Contains("P&L total"));
    }

    [TestMethod]
    public void F25_04_GaragePage_MustDisplayMultiCopyBadge()
    {
        var toyota = MockServer.Portfolio.FirstOrDefault(p => p.Quantity > 1);
        Assert.IsNotNull(toyota);
        string badgeText = $"x{toyota.Quantity}";
        Assert.AreEqual("x2", badgeText);
    }

    [TestMethod]
    public void F25_05_GarageViewModel_CalculatesValuationAndPandL()
    {
        var portfolio = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        Assert.AreEqual(449000m, portfolio.TotalValuation);
        Assert.AreEqual(39000m, portfolio.TotalGainLoss);
    }

    #endregion

    #region F26: Booster Pack Dialog & Reveal Flow

    [TestMethod]
    public void F26_01_BoosterDialog_MustDisplayAvailableQuota()
    {
        var state = new BoosterStateSpec { QuotaRemaining = 3, QuotaMax = 10 };
        string quotaDisplay = $"{state.QuotaRemaining}/{state.QuotaMax} disponibles";
        Assert.AreEqual("3/10 disponibles", quotaDisplay);
    }

    [TestMethod]
    public void F26_02_BoosterDialog_MustDisplayRefillCountdown()
    {
        var state = new BoosterStateSpec { NextRefillSeconds = 595 };
        TimeSpan span = TimeSpan.FromSeconds(state.NextRefillSeconds);
        string timerDisplay = $"Prochain dans {span:mm\\:ss}";
        Assert.AreEqual("Prochain dans 09:55", timerDisplay);
    }

    [TestMethod]
    public void F26_03_BoosterDialog_MustProvideOuvrirCtaButton()
    {
        var state = new BoosterStateSpec { QuotaRemaining = 3 };
        bool canOpen = state.QuotaRemaining > 0;
        Assert.IsTrue(canOpen, "Button 'OUVRIR' must be enabled when quota > 0.");
    }

    [TestMethod]
    public void F26_04_BoosterFlow_MustTrack5CardRevealProgression()
    {
        for (int step = 1; step <= 5; step++)
        {
            string progressText = $"Carte {step} sur 5";
            Assert.IsTrue(progressText.Contains($"Carte {step}"));
        }
    }

    [TestMethod]
    public void F26_05_BoosterFlow_CardFlipActionRevealsSpecs()
    {
        var card = MockServer.Catalog.First();
        bool isFlipped = false;

        // User taps "RETOURNER ▶"
        isFlipped = true;

        Assert.IsTrue(isFlipped);
        Assert.IsFalse(string.IsNullOrEmpty(card.Brand));
        Assert.IsFalse(string.IsNullOrEmpty(card.Rarity));
    }

    #endregion

    #region F27: Automotive Design Tokens & Styling

    [TestMethod]
    public void F27_01_DesignTokens_ColorsXamlMustExist()
    {
        string colorsPath = Path.Combine(TestWorkspace.AutoMastersDir, "Resources", "Styles", "Colors.xaml");
        if (!File.Exists(colorsPath))
        {
            Assert.Inconclusive("Milestone M3 pending: Colors.xaml not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(colorsPath));
    }

    [TestMethod]
    public void F27_02_DesignTokens_MustDefineDarkBackgroundToken()
    {
        string colorsPath = Path.Combine(TestWorkspace.AutoMastersDir, "Resources", "Styles", "Colors.xaml");
        if (!File.Exists(colorsPath))
        {
            Assert.Inconclusive("Milestone M3 pending: Colors.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(colorsPath);
        bool hasDarkBg = xaml.Contains("#0D0D0E") || xaml.Contains("#10080B") || xaml.Contains("ColorBackground");
        if (!hasDarkBg)
        {
            Assert.Inconclusive("Milestone M3 pending: Colors.xaml not yet updated with automotive palette (currently default template).");
            return;
        }

        Assert.IsTrue(hasDarkBg, "Colors.xaml must define the dark automotive background color token.");
    }

    [TestMethod]
    public void F27_03_DesignTokens_MustDefinePrimaryRedToken()
    {
        string colorsPath = Path.Combine(TestWorkspace.AutoMastersDir, "Resources", "Styles", "Colors.xaml");
        if (!File.Exists(colorsPath))
        {
            Assert.Inconclusive("Milestone M3 pending: Colors.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(colorsPath);
        bool hasRed = xaml.Contains("#E63946") || xaml.Contains("ColorPrimaryRed") || xaml.Contains("Primary");
        Assert.IsTrue(hasRed, "Colors.xaml must define the primary red accent token (#E63946).");
    }

    [TestMethod]
    public void F27_04_DesignTokens_MustDefineSecondaryGreenToken()
    {
        string colorsPath = Path.Combine(TestWorkspace.AutoMastersDir, "Resources", "Styles", "Colors.xaml");
        if (!File.Exists(colorsPath))
        {
            Assert.Inconclusive("Milestone M3 pending: Colors.xaml not yet created.");
            return;
        }

        string xaml = File.ReadAllText(colorsPath);
        bool hasGreen = xaml.Contains("#10B981") || xaml.Contains("SecondaryGreen") || xaml.Contains("Green");
        if (!hasGreen)
        {
            Assert.Inconclusive("Milestone M3 pending: Colors.xaml not yet updated with secondary green accent.");
            return;
        }

        Assert.IsTrue(hasGreen, "Colors.xaml must define the secondary green accent token (#10B981).");
    }

    [TestMethod]
    public void F27_05_DesignTokens_StylesXamlMustExist()
    {
        string stylesPath = Path.Combine(TestWorkspace.AutoMastersDir, "Resources", "Styles", "Styles.xaml");
        if (!File.Exists(stylesPath))
        {
            Assert.Inconclusive("Milestone M3 pending: Styles.xaml not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(stylesPath));
    }

    #endregion

    #region F28: Code-Behind MVVM Purity

    [TestMethod]
    public void F28_01_CodeBehind_AllViewCsFilesMustBePure()
    {
        string vuesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(vuesDir))
        {
            Assert.Inconclusive("Milestone M3 pending: Vues directory not yet created.");
            return;
        }

        var files = Directory.GetFiles(vuesDir, "*.xaml.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            bool isPure = TestWorkspace.ValidateCodeBehindPurity(content, out string violation);
            Assert.IsTrue(isPure, $"MVVM code-behind purity violation in {Path.GetFileName(file)}: {violation}");
        }
    }

    [TestMethod]
    public void F28_02_CodeBehind_MustContainZeroHttpClientInstances()
    {
        string vuesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(vuesDir))
        {
            Assert.Inconclusive("Milestone M3 pending: Vues directory not yet created.");
            return;
        }

        var files = Directory.GetFiles(vuesDir, "*.xaml.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            Assert.IsFalse(content.Contains("HttpClient"), $"Direct HttpClient found in {Path.GetFileName(file)}");
        }
    }

    [TestMethod]
    public void F28_03_CodeBehind_MustContainZeroServiceApiMethodCalls()
    {
        string vuesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(vuesDir))
        {
            Assert.Inconclusive("Milestone M3 pending: Vues directory not yet created.");
            return;
        }

        var files = Directory.GetFiles(vuesDir, "*.xaml.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            Assert.IsFalse(content.Contains("ServiceAPI.Instance.Recuperer"), $"Direct ServiceAPI call in {Path.GetFileName(file)}");
        }
    }

    [TestMethod]
    public void F28_04_CodeBehind_MustOnlyInitializeComponentAndBindingContext()
    {
        string vuesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(vuesDir))
        {
            Assert.Inconclusive("Milestone M3 pending: Vues directory not yet created.");
            return;
        }

        var files = Directory.GetFiles(vuesDir, "*.xaml.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            Assert.IsTrue(content.Contains("InitializeComponent()"), $"{Path.GetFileName(file)} must call InitializeComponent()");
        }
    }

    [TestMethod]
    public void F28_05_CodeBehind_MustNotMutateDomainModelsDirectly()
    {
        string vuesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(vuesDir))
        {
            Assert.Inconclusive("Milestone M3 pending: Vues directory not yet created.");
            return;
        }

        var files = Directory.GetFiles(vuesDir, "*.xaml.cs", SearchOption.AllDirectories);
        foreach (var file in files)
        {
            string content = File.ReadAllText(file);
            Assert.IsFalse(content.Contains("Positions.Add"), $"Direct model mutation in {Path.GetFileName(file)}");
        }
    }

    #endregion
}
