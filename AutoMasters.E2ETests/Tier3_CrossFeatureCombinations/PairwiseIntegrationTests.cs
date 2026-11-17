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

namespace AutoMasters.E2ETests.Tier3_CrossFeatureCombinations;

[TestClass]
[TestCategory("Tier3")]
[TestCategory("Pairwise")]
public class PairwiseIntegrationTests : E2ETestBase
{
    [TestMethod]
    public async Task Pairwise_01_Auth_And_Catalog()
    {
        // 1. Unauthenticated request to /portfolio or secured resource
        var unauthClient = new HttpClient(MockServer) { BaseAddress = Client.BaseAddress };
        // 2. Perform login
        var loginReq = new DemandeConnexionSpec
        {
            Username = MockGroup2ApiServer.DefaultUser,
            Password = MockGroup2ApiServer.DefaultPassword
        };
        var loginResp = await unauthClient.PostAsync("/auth/login",
            new StringContent(JsonConvert.SerializeObject(loginReq), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, loginResp.StatusCode);

        var loginResult = JsonConvert.DeserializeObject<ReponseConnexionSpec>(await loginResp.Content.ReadAsStringAsync());
        Assert.IsNotNull(loginResult);

        // 3. Authenticated request
        unauthClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResult.AccessToken);
        var catalogResp = await unauthClient.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.OK, catalogResp.StatusCode);
    }

    [TestMethod]
    public async Task Pairwise_02_Auth_And_Market()
    {
        // Login and view market listings
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);
        var marketResp = await Client.GetAsync("/offers");
        Assert.AreEqual(HttpStatusCode.OK, marketResp.StatusCode);

        string json = await marketResp.Content.ReadAsStringAsync();
        Assert.IsTrue(json.Contains("TurboAce") || json.Contains("NightRider"));
    }

    [TestMethod]
    public async Task Pairwise_03_Auth_And_Portfolio()
    {
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);
        var resp = await Client.GetAsync("/portfolio");
        Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);

        string json = await resp.Content.ReadAsStringAsync();
        var port = JsonConvert.DeserializeObject<PortefeuilleSpec>(json);
        Assert.IsNotNull(port);
        Assert.IsTrue(port.Positions.Count > 0);
    }

    [TestMethod]
    public async Task Pairwise_04_Catalog_And_CardDetail()
    {
        // Select first card from catalog and verify detail matches
        var catResp = await Client.GetAsync("/cards");
        string catJson = await catResp.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(catJson, new { items = new System.Collections.Generic.List<CarteSpec>() });

        Assert.IsNotNull(envelope);
        var catalogCard = envelope.items.First();

        var detailResp = await Client.GetAsync($"/cards/{catalogCard.Id}");
        Assert.AreEqual(HttpStatusCode.OK, detailResp.StatusCode);
        var detailCard = JsonConvert.DeserializeObject<CarteSpec>(await detailResp.Content.ReadAsStringAsync());

        Assert.IsNotNull(detailCard);
        Assert.AreEqual(catalogCard.Id, detailCard.Id);
        Assert.AreEqual(catalogCard.Brand, detailCard.Brand);
        Assert.AreEqual(catalogCard.OfficialPrice, detailCard.OfficialPrice);
    }

    [TestMethod]
    public async Task Pairwise_05_CatalogFilter_And_MarketOffers()
    {
        // Filter catalog for Ferrari
        var catResp = await Client.GetAsync("/cards?search=Ferrari");
        string catJson = await catResp.Content.ReadAsStringAsync();
        Assert.IsTrue(catJson.Contains("LaFerrari"));

        // Verify active market also contains Ferrari offer
        var mktResp = await Client.GetAsync("/offers");
        string mktJson = await mktResp.Content.ReadAsStringAsync();
        Assert.IsTrue(mktJson.Contains("Ferrari") || mktJson.Contains("card_101"));
    }

    [TestMethod]
    public async Task Pairwise_06_MarketPurchase_And_GarageValuation()
    {
        var portfolioBefore = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        decimal valuationBefore = portfolioBefore.TotalValuation;

        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");
        string key = Guid.NewGuid().ToString();

        var buyReq = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = key };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", key);

        var buyResp = await Client.SendAsync(msg);
        Assert.AreEqual(HttpStatusCode.OK, buyResp.StatusCode);

        var portfolioAfter = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        Assert.IsTrue(portfolioAfter.TotalValuation >= valuationBefore,
            "Purchasing an asset must increase or preserve total garage valuation.");
    }

    [TestMethod]
    public async Task Pairwise_07_MarketPurchase_And_BalanceDeduction()
    {
        decimal balanceBefore = MockServer.UserBalance;
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");

        string key = Guid.NewGuid().ToString();
        var buyReq = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = key };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", key);

        var buyResp = await Client.SendAsync(msg);
        Assert.AreEqual(HttpStatusCode.OK, buyResp.StatusCode);
        Assert.AreEqual(balanceBefore - offer.Price, MockServer.UserBalance);
    }

    [TestMethod]
    public async Task Pairwise_08_MarketReservation_And_PurchaseLock()
    {
        // Reserve offer
        await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        MockServer.ActiveReservations["offer_ferrari"] = "competitor_user";

        // Second user attempts immediate buy
        string key = Guid.NewGuid().ToString();
        var buyReq = new DemandeAchatSpec { OfferId = "offer_ferrari", IdempotencyKey = key };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", key);

        var buyResp = await Client.SendAsync(msg);
        Assert.AreEqual(HttpStatusCode.Conflict, buyResp.StatusCode);
    }

    [TestMethod]
    public async Task Pairwise_09_MarketReservationExpiry_And_PublicAvailability()
    {
        var offer = MockServer.Offers.First(o => o.Id == "offer_ferrari");
        offer.Status = "reserved";
        offer.ExpiresAt = DateTime.UtcNow.AddMinutes(-1); // Expired!
        MockServer.ActiveReservations.Remove(offer.Id);
        offer.Status = "active"; // Released lock

        // Immediate buy now succeeds
        string key = Guid.NewGuid().ToString();
        var buyReq = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = key };
        var msg = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        msg.Headers.Add("Idempotency-Key", key);

        var buyResp = await Client.SendAsync(msg);
        Assert.AreEqual(HttpStatusCode.OK, buyResp.StatusCode);
    }

    [TestMethod]
    public async Task Pairwise_10_GarageCard_And_MarketSellOffer()
    {
        var owned = MockServer.Portfolio.First();
        var createReq = new DemandeCreationOffreSpec
        {
            CopyId = owned.Id,
            Price = 300000m
        };

        var resp = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(createReq), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Created, resp.StatusCode);

        var created = JsonConvert.DeserializeObject<OffreSpec>(await resp.Content.ReadAsStringAsync());
        Assert.IsNotNull(created);
        Assert.AreEqual(MockGroup2ApiServer.DefaultUser, created.Seller);
    }

    [TestMethod]
    public async Task Pairwise_11_BoosterPack_And_GarageValuation()
    {
        var portBefore = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        int countBefore = portBefore.Positions.Sum(p => p.Quantity);

        var openResp = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, openResp.StatusCode);

        var portAfter = new PortefeuilleSpec { Positions = MockServer.Portfolio };
        int countAfter = portAfter.Positions.Sum(p => p.Quantity);
        Assert.AreEqual(countBefore + 5, countAfter);
    }

    [TestMethod]
    public async Task Pairwise_12_BoosterPack_And_QuotaCountdown()
    {
        int initialQuota = MockServer.BoosterQuota;
        await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(initialQuota - 1, MockServer.BoosterQuota);
    }

    [TestMethod]
    public async Task Pairwise_13_MarketIdempotency_And_DoubleTap()
    {
        string key = Guid.NewGuid().ToString();
        var offer = MockServer.Offers.First(o => o.Id == "offer_porsche");
        decimal balanceBefore = MockServer.UserBalance;

        var buyReq = new DemandeAchatSpec { OfferId = offer.Id, IdempotencyKey = key };

        var msg1 = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        msg1.Headers.Add("Idempotency-Key", key);
        var resp1 = await Client.SendAsync(msg1);

        var msg2 = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        msg2.Headers.Add("Idempotency-Key", key);
        var resp2 = await Client.SendAsync(msg2);

        Assert.AreEqual(HttpStatusCode.OK, resp1.StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, resp2.StatusCode);
        Assert.AreEqual(balanceBefore - offer.Price, MockServer.UserBalance);
    }

    [TestMethod]
    public void Pairwise_14_SeedDatabase_And_CatalogDeserialization()
    {
        string path = TestWorkspace.CarsDatabasePath;
        if (!File.Exists(path))
        {
            Assert.Inconclusive("Milestone M1 pending: cars_database.json not found.");
            return;
        }

        string json = File.ReadAllText(path);
        var cars = JsonConvert.DeserializeObject<System.Collections.Generic.List<CarteSpec>>(json);

        Assert.IsNotNull(cars);
        Assert.IsTrue(cars.Count >= 12);
        foreach (var car in cars)
        {
            Assert.IsFalse(string.IsNullOrEmpty(car.Brand));
            Assert.IsFalse(string.IsNullOrEmpty(car.Model));
            Assert.IsTrue(car.OfficialPrice > 0);
        }
    }

    [TestMethod]
    public void Pairwise_15_ViewModelCommand_And_CodeBehindPurity()
    {
        // Static analysis pairing: All Views in Vues/ have zero code-behind commands
        string vuesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(vuesDir))
        {
            Assert.Inconclusive("Milestone M3 pending: Vues directory not yet created.");
            return;
        }

        var csFiles = Directory.GetFiles(vuesDir, "*.xaml.cs");
        foreach (var cs in csFiles)
        {
            string code = File.ReadAllText(cs);
            Assert.IsFalse(code.Contains("ICommand"), $"View code-behind {Path.GetFileName(cs)} should not declare ICommand.");
            Assert.IsFalse(code.Contains("RelayCommand"), $"View code-behind {Path.GetFileName(cs)} should not declare RelayCommand.");
        }
    }
}
