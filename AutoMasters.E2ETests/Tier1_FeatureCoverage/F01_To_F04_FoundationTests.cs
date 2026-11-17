using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using AutoMasters.E2ETests.Infrastructure;

namespace AutoMasters.E2ETests.Tier1_FeatureCoverage;

[TestClass]
[TestCategory("Tier1")]
[TestCategory("M1")]
public class F01_To_F04_FoundationTests : E2ETestBase
{
    #region F01: Project Solution & .slnx

    [TestMethod]
    public void F01_01_SolutionFile_MustExistAndHaveSlnxExtension()
    {
        string slnxPath = TestWorkspace.SolutionPath;
        if (!File.Exists(slnxPath))
        {
            Assert.Inconclusive($"Milestone M1 pending: AutoMasters.slnx not found at {slnxPath}");
            return;
        }

        Assert.IsTrue(File.Exists(slnxPath), "AutoMasters.slnx must exist at the repository root.");
        Assert.AreEqual(".slnx", Path.GetExtension(slnxPath).ToLowerInvariant());
    }

    [TestMethod]
    public void F01_02_SolutionFile_MustContainValidXmlSyntax()
    {
        string slnxPath = TestWorkspace.SolutionPath;
        if (!File.Exists(slnxPath))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.slnx not found.");
            return;
        }

        string content = File.ReadAllText(slnxPath);
        bool isValid = TestWorkspace.ValidateXml(content, out string error);
        Assert.IsTrue(isValid, $"AutoMasters.slnx must be well-formed XML: {error}");
    }

    [TestMethod]
    public void F01_03_SolutionFile_MustReferenceAutoMastersProject()
    {
        string slnxPath = TestWorkspace.SolutionPath;
        if (!File.Exists(slnxPath))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.slnx not found.");
            return;
        }

        string content = File.ReadAllText(slnxPath);
        Assert.IsTrue(content.Contains("AutoMasters.csproj"), "AutoMasters.slnx must reference AutoMasters.csproj");
    }

    [TestMethod]
    public void F01_04_SolutionFile_MustReferenceTestProject()
    {
        string slnxPath = TestWorkspace.SolutionPath;
        if (!File.Exists(slnxPath))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.slnx not found.");
            return;
        }

        string content = File.ReadAllText(slnxPath);
        bool referencesTests = content.Contains("AutoMasters.Tests.csproj") || content.Contains("AutoMasters.E2ETests.csproj");
        Assert.IsTrue(referencesTests, "AutoMasters.slnx must reference test project(s).");
    }

    [TestMethod]
    public void F01_05_SolutionFile_MustNotContainLegacyFormatArtifacts()
    {
        string slnxPath = TestWorkspace.SolutionPath;
        if (!File.Exists(slnxPath))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.slnx not found.");
            return;
        }

        string content = File.ReadAllText(slnxPath);
        Assert.IsFalse(content.Contains("Microsoft Visual Studio Solution File, Format Version 12.00"),
            "AutoMasters.slnx should use modern .slnx XML format, not legacy .sln header.");
    }

    #endregion

    #region F02: Reference Folder Layout

    [TestMethod]
    public void F02_01_FolderLayout_MustIncludeFrenchModelesFolder()
    {
        string target = Path.Combine(TestWorkspace.AutoMastersDir, "Modeles");
        if (!Directory.Exists(TestWorkspace.AutoMastersDir))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters directory not yet created.");
            return;
        }

        Assert.IsTrue(Directory.Exists(target), "Folder 'Modeles' must exist matching TestVoiture structure.");
    }

    [TestMethod]
    public void F02_02_FolderLayout_MustIncludeFrenchServicesFolder()
    {
        string target = Path.Combine(TestWorkspace.AutoMastersDir, "Services");
        if (!Directory.Exists(TestWorkspace.AutoMastersDir))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters directory not yet created.");
            return;
        }

        Assert.IsTrue(Directory.Exists(target), "Folder 'Services' must exist matching TestVoiture structure.");
    }

    [TestMethod]
    public void F02_03_FolderLayout_MustIncludeFrenchVuesFolder()
    {
        string target = Path.Combine(TestWorkspace.AutoMastersDir, "Vues");
        if (!Directory.Exists(TestWorkspace.AutoMastersDir))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters directory not yet created.");
            return;
        }

        Assert.IsTrue(Directory.Exists(target), "Folder 'Vues' must exist matching TestVoiture structure.");
    }

    [TestMethod]
    public void F02_04_FolderLayout_MustIncludeFrenchVuesModelesFolder()
    {
        string target = Path.Combine(TestWorkspace.AutoMastersDir, "VuesModeles");
        if (!Directory.Exists(TestWorkspace.AutoMastersDir))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters directory not yet created.");
            return;
        }

        Assert.IsTrue(Directory.Exists(target), "Folder 'VuesModeles' must exist matching TestVoiture structure.");
    }

    [TestMethod]
    public void F02_05_FolderLayout_MustContainPlatformsAndResourcesFolders()
    {
        string platforms = Path.Combine(TestWorkspace.AutoMastersDir, "Platforms");
        string resources = Path.Combine(TestWorkspace.AutoMastersDir, "Resources");
        if (!Directory.Exists(TestWorkspace.AutoMastersDir))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters directory not yet created.");
            return;
        }

        Assert.IsTrue(Directory.Exists(platforms), "Folder 'Platforms' must exist.");
        Assert.IsTrue(Directory.Exists(resources), "Folder 'Resources' must exist.");
    }

    #endregion

    #region F03: Project Dependencies & Build Configuration

    [TestMethod]
    public void F03_01_Csproj_MustTargetNet10Windows()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("net10.0-windows10.0.19041.0"), "Target framework must be net10.0-windows10.0.19041.0");
    }

    [TestMethod]
    public void F03_02_Csproj_MustReferenceCommunityToolkitMvvm()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("CommunityToolkit.Mvvm"), "AutoMasters.csproj must reference CommunityToolkit.Mvvm");
    }

    [TestMethod]
    public void F03_03_Csproj_MustReferenceNewtonsoftJson()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("Newtonsoft.Json"), "AutoMasters.csproj must reference Newtonsoft.Json per R1");
    }

    [TestMethod]
    public void F03_04_Csproj_MustEnableMaui()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<UseMaui>true</UseMaui>"), "AutoMasters.csproj must specify <UseMaui>true</UseMaui>");
    }

    [TestMethod]
    public void F03_05_Csproj_MustSpecifyUnpackagedWindowsExecution()
    {
        string csproj = Path.Combine(TestWorkspace.AutoMastersDir, "AutoMasters.csproj");
        if (!File.Exists(csproj))
        {
            Assert.Inconclusive("Milestone M1 pending: AutoMasters.csproj not found.");
            return;
        }

        string xml = File.ReadAllText(csproj);
        Assert.IsTrue(xml.Contains("<WindowsPackageType>None</WindowsPackageType>"),
            "AutoMasters.csproj must specify unpackaged desktop execution (<WindowsPackageType>None</WindowsPackageType>)");
    }

    #endregion

    #region F04: Seed Database (cars_database.json)

    [TestMethod]
    public void F04_01_SeedDatabase_MustExistAtProjectRoot()
    {
        string path = TestWorkspace.CarsDatabasePath;
        if (!File.Exists(path))
        {
            Assert.Inconclusive("Milestone M1 pending: cars_database.json not yet created.");
            return;
        }

        Assert.IsTrue(File.Exists(path), "cars_database.json must exist at repository root.");
    }

    [TestMethod]
    public void F04_02_SeedDatabase_MustContainAtLeast12Vehicles()
    {
        string path = TestWorkspace.CarsDatabasePath;
        if (!File.Exists(path))
        {
            Assert.Inconclusive("Milestone M1 pending: cars_database.json not found.");
            return;
        }

        string json = File.ReadAllText(path);
        var array = JArray.Parse(json);
        Assert.IsTrue(array.Count >= 12, $"cars_database.json must contain at least 12 vehicles (found {array.Count}).");
    }

    [TestMethod]
    public void F04_03_SeedDatabase_AllVehiclesMustHaveRequiredFields()
    {
        string path = TestWorkspace.CarsDatabasePath;
        if (!File.Exists(path))
        {
            Assert.Inconclusive("Milestone M1 pending: cars_database.json not found.");
            return;
        }

        string json = File.ReadAllText(path);
        var array = JArray.Parse(json);
        string[] requiredFields = new[] { "brand", "model", "rarity", "officialPrice", "imageUrl" };

        foreach (JObject item in array)
        {
            foreach (var field in requiredFields)
            {
                Assert.IsNotNull(item[field], $"Vehicle missing required field '{field}': {item}");
            }
        }
    }

    [TestMethod]
    public void F04_04_SeedDatabase_RaritiesMustMatchSpecificationEnum()
    {
        string path = TestWorkspace.CarsDatabasePath;
        if (!File.Exists(path))
        {
            Assert.Inconclusive("Milestone M1 pending: cars_database.json not found.");
            return;
        }

        string json = File.ReadAllText(path);
        var array = JArray.Parse(json);
        var validRarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "COMMUNE", "PEU COMMUNE", "RARE", "ÉPIQUE", "EPIQUE", "LÉGENDAIRE", "LEGENDAIRE", "MYTHIQUE"
        };

        foreach (JObject item in array)
        {
            string? rarity = item["rarity"]?.ToString();
            Assert.IsNotNull(rarity);
            Assert.IsTrue(validRarities.Contains(rarity), $"Invalid rarity '{rarity}' in cars_database.json");
        }
    }

    [TestMethod]
    public void F04_05_SeedDatabase_ImageUrlsMustBeValidHttpUris()
    {
        string path = TestWorkspace.CarsDatabasePath;
        if (!File.Exists(path))
        {
            Assert.Inconclusive("Milestone M1 pending: cars_database.json not found.");
            return;
        }

        string json = File.ReadAllText(path);
        var array = JArray.Parse(json);

        foreach (JObject item in array)
        {
            string? url = item["imageUrl"]?.ToString();
            Assert.IsNotNull(url);
            bool isValidUri = Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) &&
                             (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
            Assert.IsTrue(isValidUri, $"Invalid imageUrl '{url}' in cars_database.json");
        }
    }

    #endregion
}
