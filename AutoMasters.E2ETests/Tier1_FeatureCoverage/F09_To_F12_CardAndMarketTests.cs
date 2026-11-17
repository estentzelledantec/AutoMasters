using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier1_FeatureCoverage;

[TestClass]
[TestCategory("Tier1")]
[TestCategory("M2")]
public class F09_To_F12_CardAndMarketTests : E2ETestBase
{
    #region F09: Card Detail Service (GET /cards/{id})

    [TestMethod]
    public async Task F09_01_GetCardDetail_ExistingId_ReturnsFullTechnicalSpecifications()
    {
        var response = await Client.GetAsync("/cards/card_101");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var card = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(card);
        Assert.AreEqual("card_101", card.Id);
        Assert.AreEqual("Ferrari", card.Brand);
        Assert.AreEqual(963, card.PowerHp);
        Assert.AreEqual(2017, card.Year);
        Assert.AreEqual(480000m, card.OfficialPrice);
    }

    [TestMethod]
    public async Task F09_02_GetCardDetail_NonExistingId_Returns404NotFound()
    {
        var response = await Client.GetAsync("/cards/non_existent_card_9999");
        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);

        Assert.IsNotNull(err);
        Assert.AreEqual("NOT_FOUND", err.Code);
    }

    [TestMethod]
    public async Task F09_03_GetCardDetail_RM001_ServerAuthoritativePrice()
    {
        var response = await Client.GetAsync("/cards/card_104");
        string json = await response.Content.ReadAsStringAsync();
        var card = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(card);
        Assert.AreEqual(223000m, card.OfficialPrice, "RM001: Server price is authoritative.");
    }

    [TestMethod]
    public async Task F09_04_GetCardDetail_RM002_ServerCalculatedRarity()
    {
        var response = await Client.GetAsync("/cards/card_101");
        string json = await response.Content.ReadAsStringAsync();
        var card = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(card);
        Assert.AreEqual("LÉGENDAIRE", card.Rarity, "RM002: Rarity is determined by server circulation count.");
    }

    [TestMethod]
    public async Task F09_05_GetCardDetail_CirculationCount_MustBePositive()
    {
        var response = await Client.GetAsync("/cards/card_104");
        string json = await response.Content.ReadAsStringAsync();
        var card = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(card);
        Assert.IsTrue(card.CopiesInCirculation > 0, "Circulation count must be strictly positive.");
    }

    #endregion

    #region F10: Portfolio Service (GET /portfolio)

    [TestMethod]
    public async Task F10_01_GetPortfolio_ReturnsOwnedCardsCollection()
    {
        var response = await Client.GetAsync("/portfolio");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var portfolio = JsonConvert.DeserializeObject<PortefeuilleSpec>(json);

        Assert.IsNotNull(portfolio);
        Assert.IsTrue(portfolio.Positions.Count > 0, "Portfolio must contain user positions.");
    }

    [TestMethod]
    public async Task F10_02_GetPortfolio_CalculatesTotalValuationAccurately()
    {
        var response = await Client.GetAsync("/portfolio");
        string json = await response.Content.ReadAsStringAsync();
        var portfolio = JsonConvert.DeserializeObject<PortefeuilleSpec>(json);

        Assert.IsNotNull(portfolio);
        decimal manualSum = portfolio.Positions.Sum(p => p.CurrentValue * p.Quantity);
        Assert.AreEqual(manualSum, portfolio.TotalValuation, "Total valuation must equal sum of current values * quantities.");
    }

    [TestMethod]
    public async Task F10_03_GetPortfolio_CalculatesNetProfitLoss()
    {
        var response = await Client.GetAsync("/portfolio");
        string json = await response.Content.ReadAsStringAsync();
        var portfolio = JsonConvert.DeserializeObject<PortefeuilleSpec>(json);

        Assert.IsNotNull(portfolio);
        decimal manualGainLoss = portfolio.Positions.Sum(p => (p.CurrentValue - p.PurchasePrice) * p.Quantity);
        Assert.AreEqual(manualGainLoss, portfolio.TotalGainLoss, "Total net P&L must equal sum of individual position P&Ls.");
    }

    [TestMethod]
    public async Task F10_04_GetPortfolio_MultiCopyQuantityHandling()
    {
        var response = await Client.GetAsync("/portfolio");
        string json = await response.Content.ReadAsStringAsync();
        var portfolio = JsonConvert.DeserializeObject<PortefeuilleSpec>(json);

        Assert.IsNotNull(portfolio);
        var toyota = portfolio.Positions.FirstOrDefault(p => p.Card.Brand == "Toyota");
        Assert.IsNotNull(toyota);
        Assert.AreEqual(2, toyota.Quantity, "Multiple copies of same card must be grouped with quantity > 1 (e.g. x2).");
    }

    [TestMethod]
    public async Task F10_05_GetPortfolio_EmptyCollection_ReturnsEmptyPositions()
    {
        MockServer.Portfolio.Clear();
        var response = await Client.GetAsync("/portfolio");
        string json = await response.Content.ReadAsStringAsync();
        var portfolio = JsonConvert.DeserializeObject<PortefeuilleSpec>(json);

        Assert.IsNotNull(portfolio);
        Assert.AreEqual(0, portfolio.Positions.Count);
        Assert.AreEqual(0m, portfolio.TotalValuation);
        Assert.AreEqual(0m, portfolio.TotalGainLoss);
    }

    #endregion

    #region F11: Market Offers Service (GET /cards/{id}/offers)

    [TestMethod]
    public async Task F11_01_GetMarketOffers_ReturnsActiveOffersList()
    {
        var response = await Client.GetAsync("/cards/card_101/offers");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<OffreSpec>(),
            total = 0
        });

        Assert.IsNotNull(envelope);
        Assert.IsTrue(envelope.items.Count > 0);
    }

    [TestMethod]
    public async Task F11_02_GetMarketOffers_IncludesSellerInformation()
    {
        var response = await Client.GetAsync("/cards/card_101/offers");
        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<OffreSpec>()
        });

        Assert.IsNotNull(envelope);
        var first = envelope.items.First();
        Assert.IsFalse(string.IsNullOrWhiteSpace(first.Seller), "Listing must display seller username (e.g. TurboAce).");
    }

    [TestMethod]
    public async Task F11_03_GetMarketOffers_IncludesPriceInCoins()
    {
        var response = await Client.GetAsync("/cards/card_101/offers");
        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<OffreSpec>()
        });

        Assert.IsNotNull(envelope);
        Assert.IsTrue(envelope.items.First().Price > 0, "Price in coins must be positive.");
    }

    [TestMethod]
    public async Task F11_04_GetMarketOffers_FiltersOutSoldOffers()
    {
        MockServer.Offers.Add(new OffreSpec
        {
            Id = "off_sold_999",
            Price = 100000m,
            Status = "sold"
        });

        var response = await Client.GetAsync("/cards/card_101/offers");
        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<OffreSpec>()
        });

        Assert.IsNotNull(envelope);
        Assert.IsTrue(envelope.items.All(o => o.Status != "sold"), "Sold offers must not appear in active marketplace.");
    }

    [TestMethod]
    public async Task F11_05_GetMarketOffers_EmptyOffers_ReturnsEmptyArray()
    {
        MockServer.Offers.Clear();
        var response = await Client.GetAsync("/cards/card_101/offers");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<OffreSpec>(),
            total = 0
        });

        Assert.IsNotNull(envelope);
        Assert.AreEqual(0, envelope.total);
    }

    #endregion

    #region F12: Immediate Purchase Service (POST /orders/buy)

    [TestMethod]
    public async Task F12_01_BuyOffer_WithValidOfferAndBalance_DeductsBalanceAndCompletes()
    {
        decimal initialBalance = MockServer.UserBalance;
        var offerToBuy = MockServer.Offers.First(o => o.Id == "offer_porsche");
        string idempotencyKey = Guid.NewGuid().ToString();

        var buyReq = new DemandeAchatSpec
        {
            OfferId = offerToBuy.Id,
            IdempotencyKey = idempotencyKey
        };

        var requestMsg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        requestMsg.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await Client.SendAsync(requestMsg);
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(initialBalance - offerToBuy.Price, MockServer.UserBalance, "Coins must be deducted.");
        Assert.AreEqual("sold", offerToBuy.Status, "Offer must be marked sold.");
    }

    [TestMethod]
    public async Task F12_02_BuyOffer_RM006_RequiresIdempotencyKeyHeader()
    {
        var buyReq = new DemandeAchatSpec
        {
            OfferId = "offer_ferrari",
            IdempotencyKey = "" // Missing!
        };

        var requestMsg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };

        var response = await Client.SendAsync(requestMsg);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);
        Assert.AreEqual("MISSING_IDEMPOTENCY_KEY", err?.Code);
    }

    [TestMethod]
    public async Task F12_03_BuyOffer_RM006_DuplicateRequestWithSameKey_DoesNotDoubleDebit()
    {
        string idempotencyKey = Guid.NewGuid().ToString();
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");

        var buyReq = new DemandeAchatSpec
        {
            OfferId = offer.Id,
            IdempotencyKey = idempotencyKey
        };

        // First attempt
        var req1 = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        req1.Headers.Add("Idempotency-Key", idempotencyKey);
        var resp1 = await Client.SendAsync(req1);
        Assert.AreEqual(HttpStatusCode.OK, resp1.StatusCode);
        decimal balanceAfterFirst = MockServer.UserBalance;

        // Duplicate attempt (lag / double-click)
        var req2 = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        req2.Headers.Add("Idempotency-Key", idempotencyKey);
        var resp2 = await Client.SendAsync(req2);

        Assert.AreEqual(HttpStatusCode.OK, resp2.StatusCode);
        Assert.AreEqual(balanceAfterFirst, MockServer.UserBalance, "RM006: Duplicate request must not double-debit balance.");
    }

    [TestMethod]
    public async Task F12_04_BuyOffer_WithInsufficientBalance_Returns400BadRequest()
    {
        MockServer.UserBalance = 1000m; // Less than 480 000 coins asking price
        string idempotencyKey = Guid.NewGuid().ToString();

        var buyReq = new DemandeAchatSpec
        {
            OfferId = "offer_ferrari",
            IdempotencyKey = idempotencyKey
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await Client.SendAsync(req);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);
        Assert.AreEqual("INSUFFICIENT_FUNDS", err?.Code);
    }

    [TestMethod]
    public async Task F12_05_BuyOffer_WhenOfferAlreadySold_Returns409Conflict()
    {
        var offer = MockServer.Offers.First(o => o.Id == "offer_ferrari");
        offer.Status = "sold"; // Already sold!

        string idempotencyKey = Guid.NewGuid().ToString();
        var buyReq = new DemandeAchatSpec
        {
            OfferId = offer.Id,
            IdempotencyKey = idempotencyKey
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Idempotency-Key", idempotencyKey);

        var response = await Client.SendAsync(req);
        Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);
        Assert.AreEqual("OFFER_NOT_AVAILABLE", err?.Code);
    }

    #endregion
}
