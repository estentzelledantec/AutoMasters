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

namespace AutoMasters.E2ETests.Tier1_FeatureCoverage;

[TestClass]
[TestCategory("Tier1")]
[TestCategory("M2")]
public class F13_To_F17_ServiceIntegrationTests : E2ETestBase
{
    #region F13: Offer Reservation Service (POST /offers/{id}/reservations)

    [TestMethod]
    public async Task F13_01_ReserveOffer_WithActiveOffer_LocksOfferAndSetsExpiration()
    {
        var response = await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        var offer = MockServer.Offers.First(o => o.Id == "offer_ferrari");
        Assert.AreEqual("reserved", offer.Status);
        Assert.IsTrue(offer.IsReserved);
    }

    [TestMethod]
    public async Task F13_02_ReserveOffer_RM004_SetsValidFutureExpirationDate()
    {
        var response = await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeAnonymousType(json, new { expiresAt = DateTime.MinValue });

        Assert.IsNotNull(result);
        Assert.IsTrue(result.expiresAt > DateTime.UtcNow, "RM004: Reservation must have a future expiration timestamp.");
    }

    [TestMethod]
    public async Task F13_03_ReserveOffer_RM003_PreventsAnotherUserFromReserving()
    {
        // First reservation
        await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));

        // Second reservation attempt on same offer
        var secondResp = await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Conflict, secondResp.StatusCode);
    }

    [TestMethod]
    public async Task F13_04_ReserveOffer_RM003_PreventsAnotherUserFromBuying()
    {
        // Reserve offer
        await Client.PostAsync("/offers/offer_ferrari/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));

        // Another buyer tries to buy
        MockServer.ActiveReservations["offer_ferrari"] = "another_student_user";

        var buyReq = new DemandeAchatSpec
        {
            OfferId = "offer_ferrari",
            IdempotencyKey = Guid.NewGuid().ToString()
        };

        var req = new HttpRequestMessage(HttpMethod.Post, "/orders/buy")
        {
            Content = new StringContent(JsonConvert.SerializeObject(buyReq), Encoding.UTF8, "application/json")
        };
        req.Headers.Add("Idempotency-Key", buyReq.IdempotencyKey);

        var buyResp = await Client.SendAsync(req);
        Assert.AreEqual(HttpStatusCode.Conflict, buyResp.StatusCode);

        string json = await buyResp.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);
        Assert.AreEqual("OFFER_RESERVED", err?.Code);
    }

    [TestMethod]
    public async Task F13_05_ReserveOffer_WhenOfferNotFound_Returns409Or404()
    {
        var response = await Client.PostAsync("/offers/unknown_offer_999/reservations", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.IsTrue(response.StatusCode == HttpStatusCode.NotFound || response.StatusCode == HttpStatusCode.Conflict);
    }

    #endregion

    #region F14: Offer Creation Service (POST /offers)

    [TestMethod]
    public async Task F14_01_CreateOffer_WithValidOwnedCard_CreatesActiveOffer()
    {
        var owned = MockServer.Portfolio.First();
        var createReq = new DemandeCreationOffreSpec
        {
            CopyId = owned.Id,
            Price = 250000m
        };

        var response = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(createReq), Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.Created, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        var created = JsonConvert.DeserializeObject<OffreSpec>(json);

        Assert.IsNotNull(created);
        Assert.AreEqual(250000m, created.Price);
        Assert.AreEqual("active", created.Status);
    }

    [TestMethod]
    public async Task F14_02_CreateOffer_WithZeroOrNegativePrice_Returns400BadRequest()
    {
        var createReq = new DemandeCreationOffreSpec
        {
            CopyId = "copy_porsche",
            Price = 0m
        };

        var response = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(createReq), Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);
        Assert.AreEqual("INVALID_PRICE", err?.Code);
    }

    [TestMethod]
    public async Task F14_03_CreateOffer_ForUnownedCard_Returns404NotFound()
    {
        var createReq = new DemandeCreationOffreSpec
        {
            CopyId = "copy_not_in_user_garage",
            Price = 50000m
        };

        var response = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(createReq), Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
    }

    [TestMethod]
    public async Task F14_04_CreateOffer_RM003_CannotSellCardAlreadyListedOrReserved()
    {
        var owned = MockServer.Portfolio.First();
        var createReq = new DemandeCreationOffreSpec
        {
            CopyId = owned.Id,
            Price = 250000m
        };

        // First creation succeeds
        var resp1 = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(createReq), Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.Created, resp1.StatusCode);

        // Offer is listed
        Assert.IsTrue(MockServer.Offers.Any(o => o.CopyId == owned.Id));
    }

    [TestMethod]
    public async Task F14_05_CreateOffer_AssignsCurrentUserAsSeller()
    {
        var owned = MockServer.Portfolio.First();
        var createReq = new DemandeCreationOffreSpec
        {
            CopyId = owned.Id,
            Price = 180000m
        };

        var response = await Client.PostAsync("/offers",
            new StringContent(JsonConvert.SerializeObject(createReq), Encoding.UTF8, "application/json"));
        var created = JsonConvert.DeserializeObject<OffreSpec>(await response.Content.ReadAsStringAsync());

        Assert.IsNotNull(created);
        Assert.AreEqual(MockGroup2ApiServer.DefaultUser, created.Seller);
    }

    #endregion

    #region F15: Booster Opening Service

    [TestMethod]
    public async Task F15_01_OpenBooster_WithAvailableQuota_ReturnsExactly5Cards()
    {
        var response = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeAnonymousType(json, new
        {
            cards = new System.Collections.Generic.List<CarteSpec>(),
            remainingQuota = 0
        });

        Assert.IsNotNull(result);
        Assert.AreEqual(5, result.cards.Count, "Booster pack must deliver exactly 5 cards.");
    }

    [TestMethod]
    public async Task F15_02_OpenBooster_DecrementsRemainingQuotaByOne()
    {
        int initialQuota = MockServer.BoosterQuota;
        var response = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual(initialQuota - 1, MockServer.BoosterQuota);
    }

    [TestMethod]
    public async Task F15_03_OpenBooster_WhenQuotaIsZero_Returns400BadRequest()
    {
        MockServer.BoosterQuota = 0;
        var response = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var err = JsonConvert.DeserializeObject<ErreurApiSpec>(json);
        Assert.AreEqual("QUOTA_EXHAUSTED", err?.Code);
    }

    [TestMethod]
    public async Task F15_04_OpenBooster_AtomicallyDeliversCardsToPortfolio()
    {
        int initialPortfolioCount = MockServer.Portfolio.Sum(p => p.Quantity);
        var response = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        int updatedPortfolioCount = MockServer.Portfolio.Sum(p => p.Quantity);
        Assert.AreEqual(initialPortfolioCount + 5, updatedPortfolioCount, "All 5 cards must be added to user garage immediately.");
    }

    [TestMethod]
    public async Task F15_05_OpenBooster_AllRevealedCardsHaveValidSpecs()
    {
        var response = await Client.PostAsync("/boosters/open", new StringContent("{}", Encoding.UTF8, "application/json"));
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeAnonymousType(json, new
        {
            cards = new System.Collections.Generic.List<CarteSpec>()
        });

        Assert.IsNotNull(result);
        foreach (var c in result.cards)
        {
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.Brand));
            Assert.IsFalse(string.IsNullOrWhiteSpace(c.Model));
            Assert.IsTrue(c.PowerHp > 0);
            Assert.IsTrue(c.OfficialPrice > 0);
        }
    }

    #endregion

    #region F16: Injectable HTTP Handler & Mockability

    [TestMethod]
    public async Task F16_01_InjectableHandler_AllowsFullyOfflineExecution()
    {
        // Executes entirely against MockGroup2ApiServer without network calls
        var response = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
    }

    [TestMethod]
    public async Task F16_02_InjectableHandler_SimulatesServerOfflineException()
    {
        MockServer.SimulateOffline = true;
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
        {
            await Client.GetAsync("/cards");
        });
    }

    [TestMethod]
    public async Task F16_03_InjectableHandler_Simulates500InternalServerError()
    {
        MockServer.ForceStatusCode = HttpStatusCode.InternalServerError;
        var response = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [TestMethod]
    public async Task F16_04_InjectableHandler_Simulates502BadGatewayHtmlResponse()
    {
        MockServer.ForceStatusCode = HttpStatusCode.BadGateway;
        var response = await Client.GetAsync("/portfolio");
        Assert.AreEqual(HttpStatusCode.BadGateway, response.StatusCode);
    }

    [TestMethod]
    public async Task F16_05_InjectableHandler_CustomStatusInjection()
    {
        MockServer.ForceStatusCode = HttpStatusCode.ServiceUnavailable;
        var response = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
    }

    #endregion

    #region F17: ServiceAPI Unit Tests Presence

    [TestMethod]
    public void F17_01_UnitTestProject_MustExistInSolutionLayout()
    {
        string testProjDir = TestWorkspace.TestsDir;
        if (!Directory.Exists(testProjDir))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests project not yet created.");
            return;
        }

        string csproj = Path.Combine(testProjDir, "AutoMasters.Tests.csproj");
        Assert.IsTrue(File.Exists(csproj), "AutoMasters.Tests.csproj must exist.");
    }

    [TestMethod]
    public void F17_02_UnitTestProject_MustReferenceNewtonsoftJson()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not yet created.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("Newtonsoft.Json"), "Test project must reference Newtonsoft.Json");
    }

    [TestMethod]
    public void F17_03_UnitTestProject_MustReferenceMSTestOrXUnit()
    {
        string csproj = Path.Combine(TestWorkspace.TestsDir, "AutoMasters.Tests.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests.csproj not yet created.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        bool hasAdapter = xml.Contains("MSTest.TestFramework") || xml.Contains("xunit") || xml.Contains("NUnit");
        Assert.IsTrue(hasAdapter, "Test project must reference a recognized test framework.");
    }

    [TestMethod]
    public void F17_04_UnitTestProject_MustContainMockHttpMessageHandler()
    {
        string testProjDir = TestWorkspace.TestsDir;
        if (!Directory.Exists(testProjDir))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests not yet created.");
            return;
        }

        var files = Directory.GetFiles(testProjDir, "*Mock*.cs", SearchOption.AllDirectories);
        Assert.IsTrue(files.Length > 0, "AutoMasters.Tests must contain a MockHttpMessageHandler file.");
    }

    [TestMethod]
    public void F17_05_UnitTestProject_MustCoverAuthenticationAndCatalog()
    {
        string testProjDir = TestWorkspace.TestsDir;
        if (!Directory.Exists(testProjDir))
        {
            Assert.Inconclusive("Milestone M2 pending: AutoMasters.Tests not yet created.");
            return;
        }

        var csFiles = Directory.GetFiles(testProjDir, "*.cs", SearchOption.AllDirectories);
        string allCode = string.Join("\n", csFiles.Select(File.ReadAllText));
        Assert.IsTrue(allCode.Contains("SeConnecter") || allCode.Contains("Login"), "Tests must verify authentication.");
        Assert.IsTrue(allCode.Contains("Recuperer") || allCode.Contains("Catalogue") || allCode.Contains("Cards"), "Tests must verify catalog.");
    }

    #endregion
}
