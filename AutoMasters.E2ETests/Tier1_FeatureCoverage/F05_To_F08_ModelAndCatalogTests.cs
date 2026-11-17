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
public class F05_To_F08_ModelAndCatalogTests : E2ETestBase
{
    #region F05: API Domain Models (DTOs)

    [TestMethod]
    public void F05_01_CarteModel_MustRoundTripSerializeViaNewtonsoft()
    {
        var carte = new CarteSpec
        {
            Id = "card_test_99",
            Brand = "Bugatti",
            Model = "Chiron Super Sport",
            Variant = "300+",
            Year = 2021,
            PowerHp = 1600,
            Rarity = "LÉGENDAIRE",
            OfficialPrice = 3800000m,
            CopiesInCirculation = 30,
            ImageUrl = "https://example.com/chiron.jpg",
            Description = "Hypercar W16"
        };

        string json = JsonConvert.SerializeObject(carte);
        var deserialized = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(carte.Id, deserialized.Id);
        Assert.AreEqual(carte.Brand, deserialized.Brand);
        Assert.AreEqual(carte.Model, deserialized.Model);
        Assert.AreEqual(carte.OfficialPrice, deserialized.OfficialPrice);
        Assert.AreEqual(carte.Rarity, deserialized.Rarity);
    }

    [TestMethod]
    public void F05_02_CartePossedeeModel_MustRoundTripSerializeViaNewtonsoft()
    {
        var possedee = new CartePossedeeSpec
        {
            Id = "copy_123",
            Card = new CarteSpec { Id = "c1", Brand = "Ferrari", Model = "F40" },
            CurrentValue = 1900000m,
            Quantity = 2,
            PurchasePrice = 1750000m
        };

        string json = JsonConvert.SerializeObject(possedee);
        var deserialized = JsonConvert.DeserializeObject<CartePossedeeSpec>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual("copy_123", deserialized.Id);
        Assert.AreEqual(2, deserialized.Quantity);
        Assert.AreEqual(300000m, deserialized.GainLoss); // (1.9M * 2) - (1.75M * 2) = 3.8M - 3.5M = 300K
    }

    [TestMethod]
    public void F05_03_PortefeuilleModel_MustAggregatePositionsAndValuation()
    {
        var portefeuille = new PortefeuilleSpec();
        portefeuille.Positions.Add(new CartePossedeeSpec
        {
            Id = "p1",
            CurrentValue = 222000m,
            Quantity = 1,
            PurchasePrice = 198000m
        });
        portefeuille.Positions.Add(new CartePossedeeSpec
        {
            Id = "p2",
            CurrentValue = 47000m,
            Quantity = 2,
            PurchasePrice = 39500m
        });

        Assert.AreEqual(222000m + (47000m * 2), portefeuille.TotalValuation); // 222000 + 94000 = 316000
        Assert.AreEqual(24000m + 15000m, portefeuille.TotalGainLoss); // 24000 + 15000 = 39000
    }

    [TestMethod]
    public void F05_04_OffreModel_MustRoundTripSerializeViaNewtonsoft()
    {
        var offre = new OffreSpec
        {
            Id = "off_1",
            CopyId = "copy_1",
            Seller = "TurboAce",
            Price = 480000m,
            Status = "active"
        };

        string json = JsonConvert.SerializeObject(offre);
        var deserialized = JsonConvert.DeserializeObject<OffreSpec>(json);

        Assert.IsNotNull(deserialized);
        Assert.AreEqual(offre.Id, deserialized.Id);
        Assert.AreEqual(offre.Price, deserialized.Price);
        Assert.IsFalse(deserialized.IsReserved);
    }

    [TestMethod]
    public void F05_05_ErreurApiModel_MustParseStandardErrorPayload()
    {
        string rawJson = @"{
            ""code"": ""INSUFFICIENT_FUNDS"",
            ""message"": ""Solde insuffisant pour cet achat."",
            ""correlationId"": ""trace_789""
        }";

        var error = JsonConvert.DeserializeObject<ErreurApiSpec>(rawJson);
        Assert.IsNotNull(error);
        Assert.AreEqual("INSUFFICIENT_FUNDS", error.Code);
        Assert.AreEqual("Solde insuffisant pour cet achat.", error.Message);
        Assert.AreEqual("trace_789", error.CorrelationId);
    }

    #endregion

    #region F06: Single ServiceAPI File Constraint

    [TestMethod]
    public void F06_01_ServicesFolder_MustContainOnlyOneFileNamedServiceApi()
    {
        string servicesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Services");
        if (!Directory.Exists(servicesDir))
        {
            Assert.Inconclusive("Milestone M2 pending: Services folder not yet created.");
            return;
        }

        var files = Directory.GetFiles(servicesDir, "*.cs");
        Assert.AreEqual(1, files.Length, $"Services/ must contain strictly ONE C# file (found {files.Length}).");
        string fileName = Path.GetFileName(files[0]);
        Assert.IsTrue(fileName.Equals("ServiceAPI.cs", StringComparison.OrdinalIgnoreCase),
            $"Services file must be named ServiceAPI.cs (found '{fileName}').");
    }

    [TestMethod]
    public void F06_02_ServiceApi_MustAdhereToRegionDiscipline()
    {
        string servicePath = Path.Combine(TestWorkspace.AutoMastersDir, "Services", "ServiceAPI.cs");
        if (!File.Exists(servicePath))
        {
            Assert.Inconclusive("Milestone M2 pending: ServiceAPI.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(servicePath);
        bool valid = TestWorkspace.ValidateRegionDiscipline(code, out string reason);
        Assert.IsTrue(valid, $"ServiceAPI.cs must follow C# region discipline: {reason}");
    }

    [TestMethod]
    public void F06_03_ServiceApi_MustExposeSingletonInstance()
    {
        string servicePath = Path.Combine(TestWorkspace.AutoMastersDir, "Services", "ServiceAPI.cs");
        if (!File.Exists(servicePath))
        {
            Assert.Inconclusive("Milestone M2 pending: ServiceAPI.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(servicePath);
        Assert.IsTrue(code.Contains("Instance"), "ServiceAPI must expose a static 'Instance' property per reference design.");
    }

    [TestMethod]
    public void F06_04_ServiceApi_MustConfigureHttpsBaseAddress()
    {
        string servicePath = Path.Combine(TestWorkspace.AutoMastersDir, "Services", "ServiceAPI.cs");
        if (!File.Exists(servicePath))
        {
            Assert.Inconclusive("Milestone M2 pending: ServiceAPI.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(servicePath);
        Assert.IsTrue(code.Contains("https://api-cartes-g2.fldsio.com"),
            "ServiceAPI must configure base address https://api-cartes-g2.fldsio.com/");
    }

    [TestMethod]
    public void F06_05_ServiceApi_MustSetDefaultTimeout()
    {
        string servicePath = Path.Combine(TestWorkspace.AutoMastersDir, "Services", "ServiceAPI.cs");
        if (!File.Exists(servicePath))
        {
            Assert.Inconclusive("Milestone M2 pending: ServiceAPI.cs not yet created.");
            return;
        }

        string code = File.ReadAllText(servicePath);
        Assert.IsTrue(code.Contains("Timeout"), "ServiceAPI must configure a non-zero timeout.");
    }

    #endregion

    #region F07: Authentication Service (POST /auth/login)

    [TestMethod]
    public async Task F07_01_Login_WithValidCredentials_Returns200AndBearerToken()
    {
        var req = new DemandeConnexionSpec
        {
            Username = MockGroup2ApiServer.DefaultUser,
            Password = MockGroup2ApiServer.DefaultPassword
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/auth/login", content);

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ReponseConnexionSpec>(json);

        Assert.IsNotNull(result);
        Assert.AreEqual(MockGroup2ApiServer.DefaultToken, result.AccessToken);
    }

    [TestMethod]
    public async Task F07_02_Login_WithInvalidCredentials_Returns401Unauthorized()
    {
        var req = new DemandeConnexionSpec
        {
            Username = "wrong_user@sio.lan",
            Password = "BadPassword!"
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/auth/login", content);

        Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        var error = JsonConvert.DeserializeObject<ErreurApiSpec>(json);

        Assert.IsNotNull(error);
        Assert.AreEqual("Identifiants invalides.", error.Message);
    }

    [TestMethod]
    public async Task F07_03_Login_WithEmptyCredentials_Returns400BadRequest()
    {
        var req = new DemandeConnexionSpec
        {
            Username = "",
            Password = ""
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/auth/login", content);

        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [TestMethod]
    public async Task F07_04_Login_AttachesAuthorizationHeaderAfterSuccess()
    {
        var req = new DemandeConnexionSpec
        {
            Username = MockGroup2ApiServer.DefaultUser,
            Password = MockGroup2ApiServer.DefaultPassword
        };

        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var response = await Client.PostAsync("/auth/login", content);
        var result = JsonConvert.DeserializeObject<ReponseConnexionSpec>(await response.Content.ReadAsStringAsync());

        Assert.IsNotNull(result);
        SetAuthorizationToken(result.AccessToken);

        Assert.IsNotNull(Client.DefaultRequestHeaders.Authorization);
        Assert.AreEqual("Bearer", Client.DefaultRequestHeaders.Authorization.Scheme);
        Assert.AreEqual(result.AccessToken, Client.DefaultRequestHeaders.Authorization.Parameter);
    }

    [TestMethod]
    public async Task F07_05_Login_TokenRefreshFlow_ReturnsNewAccessToken()
    {
        SetAuthorizationToken(MockGroup2ApiServer.DefaultToken);
        var response = await Client.PostAsync("/auth/refresh", new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        string json = await response.Content.ReadAsStringAsync();
        var result = JsonConvert.DeserializeObject<ReponseConnexionSpec>(json);

        Assert.IsNotNull(result);
        Assert.IsFalse(string.IsNullOrEmpty(result.AccessToken));
    }

    #endregion

    #region F08: Catalog Service (GET /cards)

    [TestMethod]
    public async Task F08_01_GetCatalog_ReturnsPaginatedCardsEnvelope()
    {
        var response = await Client.GetAsync("/cards");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<CarteSpec>(),
            page = 0,
            pageSize = 0,
            total = 0,
            totalPages = 0
        });

        Assert.IsNotNull(envelope);
        Assert.IsTrue(envelope.total > 0);
        Assert.IsTrue(envelope.items.Count > 0);
    }

    [TestMethod]
    public async Task F08_02_GetCatalog_WithSearchQuery_FiltersByBrandOrModel()
    {
        var response = await Client.GetAsync("/cards?search=Porsche");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<CarteSpec>()
        });

        Assert.IsNotNull(envelope);
        Assert.IsTrue(envelope.items.All(c => c.Brand.Contains("Porsche", StringComparison.OrdinalIgnoreCase) ||
                                              c.Model.Contains("Porsche", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task F08_03_GetCatalog_WithRarityFilter_FiltersByRarity()
    {
        var response = await Client.GetAsync("/cards?rarity=LÉGENDAIRE");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<CarteSpec>()
        });

        Assert.IsNotNull(envelope);
        Assert.IsTrue(envelope.items.All(c => c.Rarity.Equals("LÉGENDAIRE", StringComparison.OrdinalIgnoreCase)));
    }

    [TestMethod]
    public async Task F08_04_GetCatalog_WithNoMatches_ReturnsEmptyItemsList()
    {
        var response = await Client.GetAsync("/cards?search=UnicornVehicleModelX999");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);

        string json = await response.Content.ReadAsStringAsync();
        var envelope = JsonConvert.DeserializeAnonymousType(json, new
        {
            items = new System.Collections.Generic.List<CarteSpec>(),
            total = 0
        });

        Assert.IsNotNull(envelope);
        Assert.AreEqual(0, envelope.total);
        Assert.AreEqual(0, envelope.items.Count);
    }

    [TestMethod]
    public async Task F08_05_GetCatalog_VerifiesNewtonsoftJsonDeserializationContract()
    {
        var response = await Client.GetAsync("/cards");
        string json = await response.Content.ReadAsStringAsync();
        var jObj = Newtonsoft.Json.Linq.JObject.Parse(json);

        Assert.IsNotNull(jObj["items"]);
        var first = jObj["items"]!.First;
        Assert.IsNotNull(first);
        Assert.IsNotNull(first["brand"]);
        Assert.IsNotNull(first["model"]);
        Assert.IsNotNull(first["officialPrice"]);
    }

    #endregion
}
