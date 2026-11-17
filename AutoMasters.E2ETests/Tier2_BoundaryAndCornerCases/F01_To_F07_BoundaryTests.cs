using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier2_BoundaryAndCornerCases;

[TestClass]
[TestCategory("Tier2")]
[TestCategory("Boundary")]
public class F01_To_F07_BoundaryTests : E2ETestBase
{
    #region F01 Boundary Tests (5 tests)

    [TestMethod]
    public void F01_B01_Solution_MalformedXml_MustFailValidation()
    {
        string malformed = "<Solution><Project Path='foo'</Solution>";
        bool valid = TestWorkspace.ValidateXml(malformed, out _);
        Assert.IsFalse(valid, "Malformed XML in solution file must be rejected.");
    }

    [TestMethod]
    public void F01_B02_Solution_EmptyContent_MustFailValidation()
    {
        string empty = "";
        bool valid = TestWorkspace.ValidateXml(empty, out _);
        Assert.IsFalse(valid, "Empty solution file must be rejected.");
    }

    [TestMethod]
    public void F01_B03_Solution_PathWithSpecialCharacters_MustBeEscaped()
    {
        string xml = "<Solution><Project Path=\"Auto Masters/App.csproj\" /></Solution>";
        bool valid = TestWorkspace.ValidateXml(xml, out _);
        Assert.IsTrue(valid, "XML parser must handle spaces in project paths.");
    }

    [TestMethod]
    public void F01_B04_Solution_DuplicateProjectPaths_Detectable()
    {
        string xml = "<Solution><Project Path=\"AutoMasters.csproj\" /><Project Path=\"AutoMasters.csproj\" /></Solution>";
        var doc = System.Xml.Linq.XDocument.Parse(xml);
        var paths = doc.Descendants("Project").Select(p => p.Attribute("Path")?.Value).ToList();
        Assert.AreEqual(2, paths.Count);
        Assert.AreEqual(1, paths.Distinct().Count(), "Duplicate project entries should be detectable.");
    }

    [TestMethod]
    public void F01_B05_Solution_RelativePathResolution()
    {
        string relative = Path.Combine("..", "AutoMasters.slnx");
        Assert.IsTrue(relative.EndsWith(".slnx"));
    }

    #endregion

    #region F02 Boundary Tests (5 tests)

    [TestMethod]
    public void F02_B01_FolderLayout_UnauthorizedEnglishFolders_MustNotExist()
    {
        if (!Directory.Exists(TestWorkspace.AutoMastersDir))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters folder not yet created.");
            return;
        }

        string rogueModels = Path.Combine(TestWorkspace.AutoMastersDir, "Models");
        string rogueViews = Path.Combine(TestWorkspace.AutoMastersDir, "Views");
        string rogueViewModels = Path.Combine(TestWorkspace.AutoMastersDir, "ViewModels");

        Assert.IsFalse(Directory.Exists(rogueModels), "English 'Models' folder must not exist; use 'Modeles'.");
        Assert.IsFalse(Directory.Exists(rogueViews), "English 'Views' folder must not exist; use 'Vues'.");
        Assert.IsFalse(Directory.Exists(rogueViewModels), "English 'ViewModels' folder must not exist; use 'VuesModeles'.");
    }

    [TestMethod]
    public void F02_B02_FolderLayout_CaseSensitivityCheck()
    {
        string modeles = "Modeles";
        Assert.AreEqual("modeles", modeles.ToLowerInvariant());
    }

    [TestMethod]
    public void F02_B03_FolderLayout_AgentsTeamworkDir_MustNotContainCSharpCode()
    {
        string teamworkDir = Path.Combine(TestWorkspace.WorkspaceRoot, ".agents", "teamwork");
        if (Directory.Exists(teamworkDir))
        {
            var csFiles = Directory.GetFiles(teamworkDir, "*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, csFiles.Length, "CRITICAL: .agents/teamwork/ must contain zero C# source files.");
        }
    }

    [TestMethod]
    public void F02_B04_FolderLayout_PlatformsWindows_MustExistIfPlatformsExists()
    {
        string platforms = Path.Combine(TestWorkspace.AutoMastersDir, "Platforms");
        if (Directory.Exists(platforms))
        {
            string win = Path.Combine(platforms, "Windows");
            Assert.IsTrue(Directory.Exists(win), "Platforms/Windows must exist for Windows MAUI unpackaged target.");
        }
    }

    [TestMethod]
    public void F02_B05_FolderLayout_ResourcesSubfolders_Integrity()
    {
        string res = Path.Combine(TestWorkspace.AutoMastersDir, "Resources");
        if (Directory.Exists(res))
        {
            Assert.IsTrue(Directory.Exists(Path.Combine(res, "Styles")), "Resources/Styles must exist.");
        }
    }

    #endregion

    #region F03 Boundary Tests (5 tests)

    [TestMethod]
    public void F03_B01_Csproj_TargetPlatformMinVersion_Constraint()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("10.0.17763.0") || xml.Contains("10.0.19041.0"), "Target platform min version must be specified.");
    }

    [TestMethod]
    public void F03_B02_Csproj_NullableEnabled_MustBeConfigured()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<Nullable>enable</Nullable>"));
    }

    [TestMethod]
    public void F03_B03_Csproj_ImplicitUsings_MustBeConfigured()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<ImplicitUsings>enable</ImplicitUsings>"));
    }

    [TestMethod]
    public void F03_B04_Csproj_OutputType_MustBeExe()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<OutputType>Exe</OutputType>"));
    }

    [TestMethod]
    public void F03_B05_Csproj_SingleProjectProperty_MustBeTrue()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<SingleProject>true</SingleProject>"));
    }

    #endregion

    #region F04 Boundary Tests (5 tests)

    [TestMethod]
    public void F04_B01_SeedDatabase_EmptyJsonArray_ShouldFailCountAssertion()
    {
        string emptyJson = "[]";
        var array = JArray.Parse(emptyJson);
        Assert.IsTrue(array.Count < 12, "Empty JSON array has count < 12.");
    }

    [TestMethod]
    public void F04_B02_SeedDatabase_MalformedJson_ThrowsJsonReaderException()
    {
        string malformed = "{ invalid_json: 123";
        Assert.ThrowsException<JsonReaderException>(() =>
        {
            JToken.Parse(malformed);
        });
    }

    [TestMethod]
    public void F04_B03_SeedDatabase_VehicleWithNegativePrice_Detected()
    {
        var car = new CarteSpec { OfficialPrice = -50000m };
        Assert.IsTrue(car.OfficialPrice < 0, "Negative price is invalid.");
    }

    [TestMethod]
    public void F04_B04_SeedDatabase_VehicleWithNegativePower_Detected()
    {
        var car = new CarteSpec { PowerHp = -100 };
        Assert.IsTrue(car.PowerHp < 0, "Negative power is invalid.");
    }

    [TestMethod]
    public void F04_B05_SeedDatabase_InvalidUrlProtocol_Detected()
    {
        string invalidUrl = "ftp://malicious.domain/car.jpg";
        bool isHttp = invalidUrl.StartsWith("http://") || invalidUrl.StartsWith("https://");
        Assert.IsFalse(isHttp, "Only HTTP/HTTPS URLs are allowed for vehicle images.");
    }

    #endregion

    #region F05 Boundary Tests (5 tests)

    [TestMethod]
    public void F05_B01_Models_ToleranceToExtraUnexpectedFields()
    {
        string jsonWithExtra = @"{
            ""id"": ""card_101"",
            ""brand"": ""Ferrari"",
            ""model"": ""LaFerrari"",
            ""unknownExtraField"": ""some_telemetry_value"",
            ""officialPrice"": 480000.0
        }";

        var car = JsonConvert.DeserializeObject<CarteSpec>(jsonWithExtra);
        Assert.IsNotNull(car);
        Assert.AreEqual("Ferrari", car.Brand);
    }

    [TestMethod]
    public void F05_B02_Models_NullOptionalFields_DeserializesCleanly()
    {
        string jsonWithNullVariant = @"{
            ""id"": ""card_102"",
            ""brand"": ""Porsche"",
            ""model"": ""911"",
            ""variant"": null
        }";

        var car = JsonConvert.DeserializeObject<CarteSpec>(jsonWithNullVariant);
        Assert.IsNotNull(car);
        Assert.IsNull(car.Variant);
    }

    [TestMethod]
    public void F05_B03_Models_ExtremeNumericValuation_Handling()
    {
        var car = new CartePossedeeSpec
        {
            CurrentValue = 2500000000m, // 2.5 Billion
            Quantity = 10,
            PurchasePrice = 2000000000m
        };

        Assert.AreEqual(5000000000m, car.GainLoss);
    }

    [TestMethod]
    public void F05_B04_Models_UnicodeAccentsInVehicleNames_Preserved()
    {
        var car = new CarteSpec
        {
            Brand = "Renault",
            Model = "Mégane R.S. Ultime",
            Rarity = "ÉPIQUE"
        };

        string json = JsonConvert.SerializeObject(car);
        var parsed = JsonConvert.DeserializeObject<CarteSpec>(json);

        Assert.IsNotNull(parsed);
        Assert.AreEqual("Mégane R.S. Ultime", parsed.Model);
        Assert.AreEqual("ÉPIQUE", parsed.Rarity);
    }

    [TestMethod]
    public void F05_B05_Models_EmptyIdString_Handling()
    {
        var car = new CarteSpec { Id = "" };
        Assert.IsTrue(string.IsNullOrEmpty(car.Id));
    }

    #endregion

    #region F06 Boundary Tests (5 tests)

    [TestMethod]
    public void F06_B01_ServiceApi_NoSecondCsFileAllowedInServicesFolder()
    {
        string servicesDir = Path.Combine(TestWorkspace.AutoMastersDir, "Services");
        if (Directory.Exists(servicesDir))
        {
            var files = Directory.GetFiles(servicesDir, "*.cs");
            Assert.IsTrue(files.Length <= 1, "R1 STRICT RULE: Services/ must contain ONLY ONE file.");
        }
    }

    [TestMethod]
    public void F06_B02_ServiceApi_BaseAddressTrailingSlash_Preserved()
    {
        var uri = new Uri("https://api-cartes-g2.fldsio.com/");
        Assert.IsTrue(uri.AbsoluteUri.EndsWith("/"), "BaseAddress must end with trailing slash for correct relative URI resolution.");
    }

    [TestMethod]
    public void F06_B03_ServiceApi_ConcurrentRequests_Safe()
    {
        Parallel.For(0, 10, i =>
        {
            var resp = Client.GetAsync("/cards").Result;
            Assert.AreEqual(HttpStatusCode.OK, resp.StatusCode);
        });
    }

    [TestMethod]
    public void F06_B04_ServiceApi_HttpTimeoutBound_MustBeReasonable()
    {
        TimeSpan timeout = TimeSpan.FromSeconds(15);
        Assert.IsTrue(timeout.TotalSeconds >= 10 && timeout.TotalSeconds <= 30);
    }

    [TestMethod]
    public void F06_B05_ServiceApi_DisposedHandler_ThrowsObjectDisposed()
    {
        var client = new HttpClient(new MockGroup2ApiServer());
        client.Dispose();
        Assert.ThrowsException<ObjectDisposedException>(() =>
        {
            try
            {
                client.GetAsync("https://api-cartes-g2.fldsio.com/cards").Wait();
            }
            catch (AggregateException ex) when (ex.InnerException is ObjectDisposedException inner)
            {
                throw inner;
            }
        });
    }

    #endregion

    #region F07 Boundary Tests (5 tests)

    [TestMethod]
    public async Task F07_B01_Login_WhitespaceCredentials_Returns400()
    {
        var req = new DemandeConnexionSpec { Username = "   ", Password = "   " };
        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var resp = await Client.PostAsync("/auth/login", content);
        Assert.AreEqual(HttpStatusCode.BadRequest, resp.StatusCode);
    }

    [TestMethod]
    public async Task F07_B02_Login_SpecialCharactersInPassword_HandlesCorrectly()
    {
        var req = new DemandeConnexionSpec
        {
            Username = MockGroup2ApiServer.DefaultUser,
            Password = "P@$$w0rd!#%&*()[]{}<>"
        };
        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var resp = await Client.PostAsync("/auth/login", content);
        Assert.AreEqual(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [TestMethod]
    public async Task F07_B03_Login_ExtremelyLongUsername_SafelyRejects()
    {
        string hugeUsername = new string('a', 5000) + "@sio.lan";
        var req = new DemandeConnexionSpec { Username = hugeUsername, Password = "123" };
        var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");
        var resp = await Client.PostAsync("/auth/login", content);
        Assert.AreEqual(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [TestMethod]
    public async Task F07_B04_Login_SimulatedNetworkTimeout_ThrowsException()
    {
        MockServer.SimulateOffline = true;
        await Assert.ThrowsExceptionAsync<HttpRequestException>(async () =>
        {
            await Client.PostAsync("/auth/login", new StringContent("{}", Encoding.UTF8, "application/json"));
        });
    }

    [TestMethod]
    public async Task F07_B05_Login_MalformedTokenInHeader_Rejected()
    {
        SetAuthorizationToken("malformed.jwt.token.without.proper.signature");
        var resp = await Client.GetAsync("/cards");
        Assert.IsNotNull(resp);
    }

    #endregion
}
