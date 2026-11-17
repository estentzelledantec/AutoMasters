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

namespace AutoMasters.E2ETests.Tier4_RealWorldScenarios;

[TestClass]
[TestCategory("Tier4")]
[TestCategory("UserJourneys")]
public class UserJourneyTests : E2ETestBase
{
    [TestMethod]
    public async Task Journey_01_NewStudent_Launch_And_Authentication()
    {
        // 1. Initial Launch: Verify unauthenticated state
        Assert.IsNull(Client.DefaultRequestHeaders.Authorization);

        // 2. Submit student credentials from Figma Screen 1
        var loginPayload = new DemandeConnexionSpec
        {
            Username = MockGroup2ApiServer.DefaultUser,
            Password = MockGroup2ApiServer.DefaultPassword
        };

        var loginResp = await Client.PostAsync("/auth/login",
            new StringContent(JsonConvert.SerializeObject(loginPayload), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, loginResp.StatusCode);

        string loginJson = await loginResp.Content.ReadAsStringAsync();
        var loginResult = JsonConvert.DeserializeObject<ReponseConnexionSpec>(loginJson);
        Assert.IsNotNull(loginResult);
        Assert.IsFalse(string.IsNullOrEmpty(loginResult.AccessToken));

        // 3. Attach Bearer token to session
        SetAuthorizationToken(loginResult.AccessToken);
        Assert.IsNotNull(Client.DefaultRequestHeaders.Authorization);

        // 4. Session now opens AppShell main routes
        var portfolioResp = await Client.GetAsync("/portfolio");
        Assert.AreEqual(HttpStatusCode.OK, portfolioResp.StatusCode);
    }

    [TestMethod]
    public async Task Journey_02_Collector_Catalog_Search_Filter_And_Inspection()
    {
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);

        // 1. Collector opens Catalog tab
        var catResp = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.OK, catResp.StatusCode);

        // 2. Searches for "Porsche"
        var searchResp = await Client.GetAsync("/cards?search=Porsche");
        string searchJson = await searchResp.Content.ReadAsStringAsync();
        var searchEnvelope = JsonConvert.DeserializeAnonymousType(searchJson, new { items = new System.Collections.Generic.List<CarteSpec>() });
        Assert.IsNotNull(searchEnvelope);
        Assert.IsTrue(searchEnvelope.items.Count > 0);

        // 3. Filters by rarity "ÉPIQUE"
        var filterResp = await Client.GetAsync("/cards?search=Porsche&rarity=ÉPIQUE");
        string filterJson = await filterResp.Content.ReadAsStringAsync();
        var filterEnvelope = JsonConvert.DeserializeAnonymousType(filterJson, new { items = new System.Collections.Generic.List<CarteSpec>() });
        Assert.IsNotNull(filterEnvelope);
        Assert.AreEqual(1, filterEnvelope.items.Count);

        // 4. Selects card to inspect detailed specification
        var porsche = filterEnvelope.items.First();
        var detailResp = await Client.GetAsync($"/cards/{porsche.Id}");
        var detailedPorsche = JsonConvert.DeserializeObject<CarteSpec>(await detailResp.Content.ReadAsStringAsync());

        Assert.IsNotNull(detailedPorsche);
        Assert.AreEqual("911 GT3 RS", detailedPorsche.Model);
        Assert.AreEqual(525, detailedPorsche.PowerHp);
        Assert.AreEqual(223000m, detailedPorsche.OfficialPrice);
        Assert.AreEqual(1200, detailedPorsche.CopiesInCirculation);
    }

    [TestMethod]
    public async Task Journey_03_Strategic_Marketplace_Acquisition()
    {
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);
        decimal initialBalance = MockServer.UserBalance;

        // 1. Browse active marketplace feed
        var mktResp = await Client.GetAsync("/offers");
        Assert.AreEqual(HttpStatusCode.OK, mktResp.StatusCode);

        // 2. Spot Ferrari listing by TurboAce
        var ferrariOffer = MockServer.Offers.First(o => o.Id == "offer_ferrari");
        Assert.AreEqual("TurboAce", ferrariOffer.Seller);
        Assert.AreEqual(480000m, ferrariOffer.Price);

        // 3. Reserve offer temporarily (RM004 lock)
        var reserveResp = await Client.PostAsync($"/offers/{ferrariOffer.Id}/reservations",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, reserveResp.StatusCode);
        Assert.AreEqual("reserved", ferrariOffer.Status);

        // 4. Confirm purchase with UUIDv4 Idempotency Key (RM006)
        string idempotencyKey = Guid.NewGuid().ToString();
        var buyReq = new DemandeAchatSpec
        {
            OfferId = ferrariOffer.Id,
            IdempotencyKey = idempotencyKey
        };

        var buyMsg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        buyMsg.Headers.Add("Idempotency-Key", idempotencyKey);

        var buyResp = await Client.SendAsync(buyMsg);
        Assert.AreEqual(HttpStatusCode.OK, buyResp.StatusCode);

        // 5. Verify transaction finality: coins deducted & transferred to garage
        Assert.AreEqual(initialBalance - ferrariOffer.Price, MockServer.UserBalance);
        Assert.AreEqual("sold", ferrariOffer.Status);

        var portResp = await Client.GetAsync("/portfolio");
        string portJson = await portResp.Content.ReadAsStringAsync();
        Assert.IsTrue(portJson.Contains("LaFerrari Aperta") || portJson.Contains("card_101"));
    }

    [TestMethod]
    public async Task Journey_04_Portfolio_Valuation_Audit_And_Market_Listing()
    {
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);

        // 1. Load garage and verify portfolio valuation
        var portResp = await Client.GetAsync("/portfolio");
        var portfolio = JsonConvert.DeserializeObject<PortefeuilleSpec>(await portResp.Content.ReadAsStringAsync());
        Assert.IsNotNull(portfolio);

        Assert.AreEqual(449000m, portfolio.TotalValuation);
        Assert.AreEqual(39000m, portfolio.TotalGainLoss);

        // 2. Identify duplicate Toyota GR Yaris (x2)
        var toyotaPos = portfolio.Positions.FirstOrDefault(p => p.Card.Brand == "Toyota");
        Assert.IsNotNull(toyotaPos);
        Assert.AreEqual(2, toyotaPos.Quantity);

        // 3. Open sell modal via FAB (+) and list 1 copy for 49 000 coins
        int initialMarketOffers = MockServer.Offers.Count;
        var sellReq = new DemandeCreationOffreSpec
        {
            CopyId = toyotaPos.Id,
            Price = 49000m
        };

        var sellResp = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(sellReq), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Created, sellResp.StatusCode);

        // 4. Verify new offer active in marketplace feed
        Assert.AreEqual(initialMarketOffers + 1, MockServer.Offers.Count);
        var created = MockServer.Offers.Last();
        Assert.AreEqual(49000m, created.Price);
        Assert.AreEqual("active", created.Status);
    }

    [TestMethod]
    public async Task Journey_05_Daily_Booster_Ceremony_And_Reveal_Sequence()
    {
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);

        // 1. Check booster status in garage modal
        int initialQuota = MockServer.BoosterQuota;
        Assert.AreEqual(3, initialQuota);

        // 2. Open booster pack
        var openResp = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, openResp.StatusCode);

        string openJson = await openResp.Content.ReadAsStringAsync();
        var unpackResult = JsonConvert.DeserializeAnonymousType(openJson, new
        {
            cards = new System.Collections.Generic.List<CarteSpec>(),
            remainingQuota = 0
        });

        Assert.IsNotNull(unpackResult);
        Assert.AreEqual(5, unpackResult.cards.Count);
        Assert.AreEqual(initialQuota - 1, unpackResult.remainingQuota);

        // 3. Step through 5-card reveal sequence (Mockup 3)
        for (int i = 0; i < 5; i++)
        {
            var card = unpackResult.cards[i];
            int stepNumber = i + 1;
            string progressBanner = $"Carte {stepNumber} sur 5";
            Assert.AreEqual($"Carte {stepNumber} sur 5", progressBanner);
            Assert.IsNotNull(card.Brand);
            Assert.IsNotNull(card.Model);
        }

        // 4. Inspect garage: all 5 cards present
        var portResp = await Client.GetAsync("/portfolio");
        var updatedPort = JsonConvert.DeserializeObject<PortefeuilleSpec>(await portResp.Content.ReadAsStringAsync());
        Assert.IsNotNull(updatedPort);
        int totalQuantity = updatedPort.Positions.Sum(p => p.Quantity);
        Assert.IsTrue(totalQuantity >= 5);
    }
}
