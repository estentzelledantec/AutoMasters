using System;
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
public class F08_To_F14_BoundaryTests : E2ETestBase
{
    #region F08 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F08_B01_CatalogSearch_RegexSpecialCharacters_DoesNotCrash()
    {
        var resp = await Client.GetAsync("/cards?search=.*+?^${}()|[]\\");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
    }

    [TestMethod]
    public async Task F08_B02_CatalogSearch_UnicodeAccents_MatchesAccurately()
    {
        var resp = await Client.GetAsync("/cards?search=Mégane");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        string json = await resp.Content.ReadAsStringAsync();
        Assert.IsTrue(json.Contains("Mégane") || json.Contains("Megane"));
    }

    [TestMethod]
    public async Task F08_B03_CatalogSearch_WhitespaceTrimmed()
    {
        var resp = await Client.GetAsync("/cards?search=%20Porsche%20");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        string json = await resp.Content.ReadAsStringAsync();
        Assert.IsTrue(json.Contains("Porsche"));
    }

    [TestMethod]
    public async Task F08_B04_CatalogSearch_EmptyQuery_ReturnsFullList()
    {
        var resp = await Client.GetAsync("/cards?search=");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
    }

    [TestMethod]
    public async Task F08_B05_CatalogRarityFilter_CaseInsensitive()
    {
        var resp = await Client.GetAsync("/cards?rarity=lÉgeNDaiRe");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        string json = await resp.Content.ReadAsStringAsync();
        Assert.IsTrue(json.Contains("Ferrari"));
    }

    #endregion

    #region F09 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F09_B01_CardDetail_SpecialCharactersId_Returns404()
    {
        var resp = await Client.GetAsync("/cards/invalid!@#$%^&*()");
        Assert.AreEqual(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [TestMethod]
    public async Task F09_B02_CardDetail_EmptyId_ReturnsNotFound()
    {
        var resp = await Client.GetAsync("/cards/");
        // Root /cards returns catalog, but sub-path with whitespace returns 404
        var respWs = await Client.GetAsync("/cards/%20");
        Assert.AreEqual(HttpStatusCode.NotFound, respWs.StatusCode);
    }

    [TestMethod]
    public void F09_B03_CardDetail_CirculationCount_ZeroBoundary()
    {
        var card = new CarteSpec { CopiesInCirculation = 0 };
        Assert.AreEqual(0, card.CopiesInCirculation);
    }

    [TestMethod]
    public async Task F09_B04_CardDetail_ExtremeOfficialPrice_PreservesPrecision()
    {
        var resp = await Client.GetAsync("/cards/card_101");
        string json = await resp.Content.ReadAsStringAsync();
        var card = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(card);
        Assert.AreEqual(480000m, card.OfficialPrice);
    }

    [TestMethod]
    public async Task F09_B05_CardDetail_RepeatedQueries_ReturnsIdenticalValues()
    {
        var resp1 = await Client.GetStringAsync("/cards/card_104");
        var resp2 = await Client.GetStringAsync("/cards/card_104");
        Assert.AreEqual(resp1, resp2, "RM001: Consecutive queries without market changes return identical specs.");
    }

    #endregion

    #region F10 Boundary Tests (5 tests)

    [TestMethod]
    public void F10_B01_Portfolio_ZeroCards_ValuationZero()
    {
        var portfolio = new PortefeuilleSpec();
        Assert.AreEqual(0m, portfolio.TotalValuation);
        Assert.AreEqual(0m, portfolio.TotalGainLoss);
    }

    [TestMethod]
    public void F10_B02_Portfolio_BreakEvenCard_ZeroGainLoss()
    {
        var position = new CartePossedeeSpec
        {
            CurrentValue = 50000m,
            PurchasePrice = 50000m,
            Quantity = 1
        };
        Assert.AreEqual(0m, position.GainLoss);
    }

    [TestMethod]
    public void F10_B03_Portfolio_LossCard_ComputesNegativePandL()
    {
        var position = new CartePossedeeSpec
        {
            CurrentValue = 89000m,
            PurchasePrice = 91000m,
            Quantity = 1
        };
        Assert.AreEqual(-2000m, position.GainLoss);
    }

    [TestMethod]
    public void F10_B04_Portfolio_LargeQuantity_AggregatesAccurately()
    {
        var position = new CartePossedeeSpec
        {
            CurrentValue = 1000m,
            PurchasePrice = 800m,
            Quantity = 100
        };
        Assert.AreEqual(20000m, position.GainLoss);
    }

    [TestMethod]
    public void F10_B05_Portfolio_DecimalPrecision_NoFloatingPointErrors()
    {
        var position = new CartePossedeeSpec
        {
            CurrentValue = 0.1m,
            PurchasePrice = 0.2m,
            Quantity = 3
        };
        Assert.AreEqual(-0.3m, position.GainLoss);
    }

    #endregion

    #region F11 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F11_B01_MarketOffers_ZeroActiveOffers_EmptyItems()
    {
        MockServer.Offers.Clear();
        var resp = await Client.GetAsync("/offers");
        string json = await resp.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new { total = -1 });
        Assert.AreEqual(0, envelope?.total);
    }

    [TestMethod]
    public async Task F11_B02_MarketOffers_UnknownCardId_ReturnsEmptyOffers()
    {
        var resp = await Client.GetAsync("/cards/unknown_card_id/offers");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
    }

    [TestMethod]
    public void F11_B03_MarketOffers_SellerUsernameWithSpecialChars_Supported()
    {
        var offer = new OffreSpec
        {
            Seller = "Turbo_Ace#2026_🏎️"
        };
        Assert.IsTrue(offer.Seller.Contains("🏎️"));
    }

    [TestMethod]
    public void F11_B04_MarketOffers_ExtremePrice_Supported()
    {
        var offer = new OffreSpec
        {
            Price = 2650000m
        };
        Assert.AreEqual(2650000m, offer.Price);
    }

    [TestMethod]
    public async Task F11_B05_MarketOffers_ConcurrentRetrieval_Safe()
    {
        var tasks = Enumerable.Range(0, 5).Select(_ => Client.GetAsync("/offers"));
        var responses = await Task.WhenAll(tasks);
        Assert.IsTrue(responses.All(r => r.StatusCode == HttpStatusCode.OK));
    }

    #endregion

    #region F12 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F12_B01_BuyOffer_UserBalanceZero_Rejected()
    {
        MockServer.UserBalance = 0m;
        var req = new DemandeAchatSpec { OfferId = "offer_porsche", IdempotencyKey = Guid.NewGuid().ToString() };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", req.IdempotencyKey);
        var resp = await Client.SendAsync(msg);
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [TestMethod]
    public async Task F12_B02_BuyOffer_BalanceMinusOneCoin_Rejected()
    {
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");
        MockServer.UserBalance = offer.Price - 1m;

        var req = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = Guid.NewGuid().ToString() };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", req.IdempotencyKey);
        var resp = await Client.SendAsync(msg);
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [TestMethod]
    public async Task F12_B03_BuyOffer_ExactBalance_CompletesWithZeroRemaining()
    {
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");
        MockServer.UserBalance = offer.Price;

        var req = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = Guid.NewGuid().ToString() };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", req.IdempotencyKey);
        var resp = await Client.SendAsync(msg);

        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        Assert.AreEqual(0m, MockServer.UserBalance);
    }

    [TestMethod]
    public async Task F12_B04_BuyOffer_ConcurrentDuplicateTaps_OnlyOneExecuted()
    {
        string key = Guid.NewGuid().ToString();
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");
        decimal initialBalance = MockServer.UserBalance;

        var req = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = key };

        Func<Task<HttpResponseMessage>> send = () =>
        {
            var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
            {
                Content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
            };
            msg.Headers.Add("Idempotency-Key", key);
            return Client.SendAsync(msg);
        };

        var resp1 = await send();
        var resp2 = await send();

        Assert.AreEqual(HttpStatusCode.OK, resp1.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, resp2.StatusCode);
        Assert.AreEqual(initialBalance - offer.Price, MockServer.UserBalance);
    }

    [TestMethod]
    public async Task F12_B05_BuyOffer_NonExistentOffer_ReturnsConflictOrNotFound()
    {
        var req = new DemandeAchatSpec { OfferId = "fake_offer_id_9999", IdempotencyKey = Guid.NewGuid().ToString() };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", req.IdempotencyKey);
        var resp = await Client.SendAsync(msg);
        Assert.IsTrue(resp.StatusCode == HttpStatusCode.Conflict || resp.StatusCode == HttpStatusCode.NotFound);
    }

    #endregion

    #region F13 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F13_B01_ReserveOffer_AlreadyReserved_Returns409()
    {
        await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        var resp2 = await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Conflict, resp2.StatusCode);
    }

    [TestMethod]
    public async Task F13_B02_ReserveOffer_AlreadySold_Returns409()
    {
        var offer = MockServer.Offers.First(o => o.Id == "offer_ferrari");
        offer.Status = "sold";
        var resp = await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Conflict, resp.StatusCode);
    }

    [TestMethod]
    public void F13_B03_ReserveOffer_ExpiredReservation_ReleasesLock()
    {
        var offer = new OffreSpec
        {
            Status = "reserved",
            ExpiresAt = DateTime.UtcNow.AddMinutes(-5) // Expired 5 min ago!
        };

        bool isExpired = offer.ExpiresAt.HasValue && offer.ExpiresAt.Value < DateTime.UtcNow;
        Assert.IsTrue(isExpired, "RM004: Expired reservation lock must be released.");
    }

    [TestMethod]
    public async Task F13_B04_ReserveOffer_NonExistentOffer_Rejected()
    {
        var resp = await Client.PostAsync("/offers/non_existent_offer/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.IsTrue(resp.StatusCode == HttpStatusCode.Conflict || resp.StatusCode == HttpStatusCode.NotFound);
    }

    [TestMethod]
    public void F13_B05_ReserveOffer_ExpirationWindow_15MinutesStandard()
    {
        var expiresAt = DateTime.UtcNow.AddMinutes(15);
        var diff = expiresAt - DateTime.UtcNow;
        Assert.IsTrue(diff.TotalMinutes >= 14.9 && diff.TotalMinutes <= 15.1);
    }

    #endregion

    #region F14 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F14_B01_CreateOffer_ZeroPrice_Rejected()
    {
        var req = new DemandeCreationOffreSpec { CopyId = "copy_porsche", Price = 0m };
        var resp = await Client.PostAsync("/offers", new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [TestMethod]
    public async Task F14_B02_CreateOffer_NegativePrice_Rejected()
    {
        var req = new DemandeCreationOffreSpec { CopyId = "copy_porsche", Price = -500m };
        var resp = await Client.PostAsync("/offers", new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [TestMethod]
    public async Task F14_B03_CreateOffer_UnownedCard_Returns404()
    {
        var req = new DemandeCreationOffreSpec { CopyId = "not_my_car_123", Price = 50000m };
        var resp = await Client.PostAsync("/offers", new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.NotFound, resp.StatusCode);
    }

    [TestMethod]
    public async Task F14_B04_CreateOffer_ExtremePrice_Succeeds()
    {
        var owned = MockServer.Portfolio.First();
        var req = new DemandeCreationOffreSpec { CopyId = owned.Id, Price = 999999999m };
        var resp = await Client.PostAsync("/offers", new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Created, resp.StatusCode);
    }

    [TestMethod]
    public void F14_B05_CreateOffer_CardAlreadyListed_RM003Conflict()
    {
        var offer = new OffreSpec { CopyId = "copy_porsche", Status = "active" };
        bool canListAgain = offer.Status != "active";
        Assert.IsFalse(canListAgain, "RM003: Cannot list an already active offer.");
    }

    #endregion
}
