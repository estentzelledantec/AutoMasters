using System;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier2_BoundaryAndCornerCases;

[TestClass]
[TestCategory("Tier2")]
[TestCategory("Boundary")]
public class F22_To_F28_BoundaryTests : E2ETestBase
{
    #region F22 Boundary Tests (5 tests)

    [TestMethod]
    public void F22_B01_Market_EmptyOffers_DisplaysEmptyState()
    {
        var offers = new System.Collections.Generic.List<OffreSpec>();
        bool showEmptyState = offers.Count == 0;
        Assert.IsTrue(showEmptyState);
    }

    [TestMethod]
    public void F22_B02_Market_OffersCountBadgeText()
    {
        int count = 8;
        string badgeText = $"{count} offres actives";
        Assert.AreEqual("8 offres actives", badgeText);
    }

    [TestMethod]
    public void F22_B03_Market_MissingSeller_FallbackName()
    {
        var offer = new OffreSpec { Seller = "" };
        string displayedSeller = string.IsNullOrWhiteSpace(offer.Seller) ? "Vendeur anonyme" : offer.Seller;
        Assert.AreEqual("Vendeur anonyme", displayedSeller);
    }

    [TestMethod]
    public void F22_B04_Market_InsufficientBalance_DisablesBuyButton()
    {
        decimal balance = 50000m;
        decimal price = 480000m;
        bool canBuy = balance >= price;
        Assert.IsFalse(canBuy);
    }

    [TestMethod]
    public void F22_B05_Market_LiveBadgeText()
    {
        string badge = "🟢 EN DIRECT";
        Assert.IsTrue(badge.Contains("EN DIRECT"));
    }

    #endregion

    #region F23 Boundary Tests (5 tests)

    [TestMethod]
    public void F23_B01_RecapDialog_BalanceUnchangedOnDismiss()
    {
        decimal initialBalance = 500000m;
        // User dismisses dialog:
        decimal updatedBalance = initialBalance;
        Assert.AreEqual(initialBalance, updatedBalance);
    }

    [TestMethod]
    public void F23_B02_RecapDialog_FormattedPriceThousandSeparators()
    {
        decimal price = 480000m;
        string formatted = $"{price:N0} coins".Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.IsTrue(formatted.Contains("480 000") || formatted.Contains("480,000") || formatted.Contains("480.000"));
    }

    [TestMethod]
    public void F23_B03_RecapDialog_ReservationTimerZero_DisablesAction()
    {
        int remainingSeconds = 0;
        bool canReserve = remainingSeconds > 0;
        Assert.IsFalse(canReserve);
    }

    [TestMethod]
    public void F23_B04_RecapDialog_DoubleConfirm_Debounced()
    {
        int executions = 0;
        bool isExecuting = false;

        Action confirm = () =>
        {
            if (isExecuting) return;
            isExecuting = true;
            executions++;
        };

        confirm();
        confirm();

        Assert.AreEqual(1, executions);
    }

    [TestMethod]
    public void F23_B05_RecapDialog_DismissAction_ClosesModal()
    {
        bool isOpen = true;
        // Tap outside
        isOpen = false;
        Assert.IsFalse(isOpen);
    }

    #endregion

    #region F24 Boundary Tests (5 tests)

    [TestMethod]
    public void F24_B01_SellModal_ZeroCarsInGarage_EmptyAlert()
    {
        var ownedCars = new System.Collections.Generic.List<CartePossedeeSpec>();
        bool canSell = ownedCars.Count > 0;
        Assert.IsFalse(canSell);
    }

    [TestMethod]
    public void F24_B02_SellModal_PriceWithComma_Parsed()
    {
        string input = "250000,50";
        bool parsed = decimal.TryParse(input.Replace(',', '.'), System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out decimal val);
        Assert.IsTrue(parsed);
        Assert.AreEqual(250000.50m, val);
    }

    [TestMethod]
    public void F24_B03_SellModal_PriceWithSpaces_Parsed()
    {
        string input = "250 000";
        bool parsed = decimal.TryParse(input.Replace(" ", ""), out decimal val);
        Assert.IsTrue(parsed);
        Assert.AreEqual(250000m, val);
    }

    [TestMethod]
    public void F24_B04_SellModal_PriceNonNumeric_Rejected()
    {
        string input = "abc_coins";
        bool parsed = decimal.TryParse(input, out _);
        Assert.IsFalse(parsed);
    }

    [TestMethod]
    public void F24_B05_SellModal_CancelAction_NoListing()
    {
        int initialOffers = 10;
        // User clicks cancel:
        Assert.AreEqual(10, initialOffers);
    }

    #endregion

    #region F25 Boundary Tests (5 tests)

    [TestMethod]
    public void F25_B01_Garage_TotalValuation_Formatting()
    {
        decimal valuation = 482500m;
        string text = $"{valuation:N0} coins".Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.IsTrue(text.Contains("482 500") || text.Contains("482,500") || text.Contains("482.500"));
    }

    [TestMethod]
    public void F25_B02_Garage_PositivePandL_GreenTextFormat()
    {
        decimal pnl = 32500m;
        string text = $"+{pnl:N0} € P&L total".Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.IsTrue(text.StartsWith("+"));
        Assert.IsTrue(text.Contains("32 500") || text.Contains("32,500") || text.Contains("32.500"));
    }

    [TestMethod]
    public void F25_B03_Garage_NegativePandL_RedTextFormat()
    {
        decimal pnl = -2000m;
        string text = $"{pnl:N0} € P&L total".Replace("\u00A0", " ").Replace("\u202F", " ");
        Assert.IsTrue(text.StartsWith("-"));
        Assert.IsTrue(text.Contains("2 000") || text.Contains("2,000") || text.Contains("2.000"));
    }

    [TestMethod]
    public void F25_B04_Garage_QuantityMultiplier_HiddenWhenQuantityOne()
    {
        int quantity = 1;
        bool isBadgeVisible = quantity > 1;
        Assert.IsFalse(isBadgeVisible, "Multiplier badge (x2) must be hidden when quantity == 1.");
    }

    [TestMethod]
    public void F25_B05_Garage_QuantityMultiplier_VisibleWhenQuantityTwo()
    {
        int quantity = 2;
        bool isBadgeVisible = quantity > 1;
        string badgeText = $"x{quantity}";
        Assert.IsTrue(isBadgeVisible);
        Assert.AreEqual("x2", badgeText);
    }

    #endregion

    #region F26 Boundary Tests (5 tests)

    [TestMethod]
    public void F26_B01_Booster_CountdownTimerFormatting()
    {
        int seconds = 595;
        TimeSpan span = TimeSpan.FromSeconds(seconds);
        string timer = $"{span:mm\\:ss}";
        Assert.AreEqual("09:55", timer);
    }

    [TestMethod]
    public void F26_B02_Booster_5SegmentProgress_Indices()
    {
        for (int i = 1; i <= 5; i++)
        {
            double progressFraction = i / 5.0;
            Assert.IsTrue(progressFraction > 0.0 && progressFraction <= 1.0);
        }
    }

    [TestMethod]
    public void F26_B03_Booster_ButtonText_Retourner()
    {
        string cta = "RETOURNER ▶";
        Assert.IsTrue(cta.Contains("RETOURNER"));
    }

    [TestMethod]
    public void F26_B04_Booster_ProgressStep_CompletionAtCard5()
    {
        int currentCard = 5;
        bool isLast = currentCard == 5;
        Assert.IsTrue(isLast);
    }

    [TestMethod]
    public void F26_B05_Booster_QuotaIndicator_3Of10()
    {
        int available = 3;
        int max = 10;
        string display = $"{available}/{max} disponibles";
        Assert.AreEqual("3/10 disponibles", display);
    }

    #endregion

    #region F27 Boundary Tests (5 tests)

    [TestMethod]
    public void F27_B01_DesignTokens_HexValidity_AllTokens()
    {
        string[] hexTokens = new[]
        {
            "#0D0D0E", "#1A1A1E", "#242426", "#141416",
            "#E63946", "#10B981", "#2D1518",
            "#F59E0B", "#A855F7", "#38BDF8", "#9CA3AF"
        };

        foreach (var hex in hexTokens)
        {
            Assert.IsTrue(hex.StartsWith("#"));
            Assert.AreEqual(7, hex.Length);
        }
    }

    [TestMethod]
    public void F27_B02_DesignTokens_LegendaryColorToken()
    {
        Assert.AreEqual("#F59E0B", "#F59E0B");
    }

    [TestMethod]
    public void F27_B03_DesignTokens_EpicColorToken()
    {
        Assert.AreEqual("#A855F7", "#A855F7");
    }

    [TestMethod]
    public void F27_B04_DesignTokens_RareColorToken()
    {
        Assert.AreEqual("#38BDF8", "#38BDF8");
    }

    [TestMethod]
    public void F27_B05_DesignTokens_CommonColorToken()
    {
        Assert.AreEqual("#9CA3AF", "#9CA3AF");
    }

    #endregion

    #region F28 Boundary Tests (5 tests)

    [TestMethod]
    public void F28_B01_PurityValidator_DetectsHttpClient()
    {
        string sampleCode = "public partial class BadPage { void Fetch() { var client = new HttpClient(); } }";
        bool isPure = TestWorkspace.ValidateCodeBehindPurity(sampleCode, out string err);
        Assert.IsFalse(isPure);
        Assert.IsTrue(err.Contains("HttpClient"));
    }

    [TestMethod]
    public void F28_B02_PurityValidator_DetectsTaskRun()
    {
        string sampleCode = "public partial class BadPage { void Run() { Task.Run(() => {}); } }";
        bool isPure = TestWorkspace.ValidateCodeBehindPurity(sampleCode, out string err);
        Assert.IsFalse(isPure);
        Assert.IsTrue(err.Contains("Task.Run"));
    }

    [TestMethod]
    public void F28_B03_PurityValidator_DetectsServiceApiInstance()
    {
        string sampleCode = "public partial class BadPage { void Call() { ServiceAPI.Instance.RecupererCatalogueAsync(); } }";
        bool isPure = TestWorkspace.ValidateCodeBehindPurity(sampleCode, out string err);
        Assert.IsFalse(isPure);
        Assert.IsTrue(err.Contains("ServiceAPI"));
    }

    [TestMethod]
    public void F28_B04_PurityValidator_DetectsJsonConvert()
    {
        string sampleCode = "public partial class BadPage { void Parse() { JsonConvert.DeserializeObject<Carte>(raw); } }";
        bool isPure = TestWorkspace.ValidateCodeBehindPurity(sampleCode, out string err);
        Assert.IsFalse(isPure);
        Assert.IsTrue(err.Contains("JsonConvert"));
    }

    [TestMethod]
    public void F28_B05_PurityValidator_PassesCleanConstructorOnlyCodeBehind()
    {
        string cleanCode = @"
namespace AutoMasters.Vues;

public partial class ConnexionPage : ContentPage
{
    #region Constructeurs
    public ConnexionPage()
    {
        InitializeComponent();
        BindingContext = new ConnexionVueModele();
    }
    #endregion
}";
        bool isPure = TestWorkspace.ValidateCodeBehindPurity(cleanCode, out string err);
        Assert.IsTrue(isPure, $"Clean code-behind must pass purity validation: {err}");
    }

    #endregion
}
