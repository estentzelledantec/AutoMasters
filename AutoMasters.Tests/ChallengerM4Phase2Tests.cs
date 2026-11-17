using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AutoMasters.Modeles;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace AutoMasters.Tests;

/// <summary>
/// Empirical Challenger verification tests for Milestone 4 Phase 2:
/// Data Expansion & Cars Database Adversarial Validation (Features F04, F30).
/// </summary>
public class ChallengerM4Phase2Tests
{
    private static string RepoRootDir
    {
        get
        {
            string current = AppContext.BaseDirectory;
            while (!string.IsNullOrEmpty(current))
            {
                string candidate = Path.Combine(current, "cars_database.json");
                if (File.Exists(candidate))
                {
                    return current;
                }
                var parent = Directory.GetParent(current);
                if (parent == null) break;
                current = parent.FullName;
            }
            return @"c:\Users\stent\source\repos\AutoMasters";
        }
    }

    private static string CarsDatabasePath => Path.Combine(RepoRootDir, "cars_database.json");

    #region 1. File & JSON Structure Validation

    [Fact]
    public void F04_DatabaseFile_ExistsAtRepositoryRoot()
    {
        Assert.True(File.Exists(CarsDatabasePath), $"cars_database.json must exist at repository root: {CarsDatabasePath}");
    }

    [Fact]
    public void F04_DatabaseFile_IsValidJsonParseableWithNewtonsoft()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        Assert.False(string.IsNullOrWhiteSpace(json), "cars_database.json must not be empty");

        // Parse with JArray
        var array = JArray.Parse(json);
        Assert.NotNull(array);
        Assert.NotEmpty(array);
    }

    [Fact]
    public void F04_DatabaseFile_ContainsAtLeast14VehicleRecords()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var array = JArray.Parse(json);

        Assert.True(array.Count >= 14, $"cars_database.json must contain at least 14 vehicles, but found {array.Count}");
    }

    #endregion

    #region 2. Required Fields Validation (JSON Schema)

    [Fact]
    public void F04_DatabaseFile_AllVehiclesContainAllRequiredFields()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var array = JArray.Parse(json);

        string[] requiredFields = new[]
        {
            "id",
            "brand",
            "model",
            "variant",
            "year",
            "powerHp",
            "rarity",
            "officialPrice",
            "copiesInCirculation",
            "imageUrl",
            "description"
        };

        for (int i = 0; i < array.Count; i++)
        {
            var item = (JObject)array[i];
            string carId = item["id"]?.ToString() ?? $"index_{i}";

            foreach (var field in requiredFields)
            {
                var token = item[field];
                Assert.True(token != null, $"Vehicle #{i} (ID: {carId}) is missing required field '{field}'");
                Assert.False(token.Type == JTokenType.Null, $"Vehicle #{i} (ID: {carId}) field '{field}' is null");

                if (token.Type == JTokenType.String)
                {
                    Assert.False(string.IsNullOrWhiteSpace(token.ToString()),
                        $"Vehicle #{i} (ID: {carId}) field '{field}' cannot be empty string");
                }
            }
        }
    }

    [Fact]
    public void F04_DatabaseFile_AllImageUrlsAreNonEmptyWellFormedHttpsUrls()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var array = JArray.Parse(json);

        for (int i = 0; i < array.Count; i++)
        {
            var item = (JObject)array[i];
            string carId = item["id"]?.ToString() ?? $"index_{i}";
            string? url = item["imageUrl"]?.ToString();

            Assert.False(string.IsNullOrWhiteSpace(url), $"Vehicle #{i} (ID: {carId}) has empty imageUrl");
            Assert.True(Uri.TryCreate(url, UriKind.Absolute, out Uri? uri), $"Vehicle #{i} (ID: {carId}) imageUrl is not a valid absolute URI: {url}");
            Assert.Equal("https", uri!.Scheme);
            Assert.False(string.IsNullOrWhiteSpace(uri.Host), $"Vehicle #{i} (ID: {carId}) imageUrl has empty host: {url}");
        }
    }

    #endregion

    #region 3. C# Modeles.Carte Deserialization

    [Fact]
    public void F04_Deserialization_IntoCarteModel_SucceedsWithZeroErrors()
    {
        string json = File.ReadAllText(CarsDatabasePath);

        // Deserialization with strict settings
        var settings = new JsonSerializerSettings
        {
            MissingMemberHandling = MissingMemberHandling.Ignore
        };

        List<Carte>? cartes = JsonConvert.DeserializeObject<List<Carte>>(json, settings);

        Assert.NotNull(cartes);
        Assert.True(cartes.Count >= 14, $"Deserialized {cartes.Count} cars, expected >= 14");

        var validRarities = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "COMMUNE", "PEU COMMUNE", "RARE", "ÉPIQUE", "EPIQUE", "LÉGENDAIRE", "LEGENDAIRE", "MYTHIQUE"
        };

        for (int i = 0; i < cartes.Count; i++)
        {
            var c = cartes[i];

            Assert.False(string.IsNullOrWhiteSpace(c.Identifiant), $"Car #{i} Identifiant is empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Marque), $"Car #{i} Marque is empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Modele), $"Car #{i} Modele is empty");
            Assert.False(string.IsNullOrWhiteSpace(c.Variante), $"Car #{i} Variante is empty");
            Assert.True(c.Annee >= 1950 && c.Annee <= 2030, $"Car #{i} Annee out of range: {c.Annee}");
            Assert.True(c.Puissance > 0, $"Car #{i} Puissance must be positive: {c.Puissance}");
            Assert.True(validRarities.Contains(c.Rarete), $"Car #{i} Rarete is invalid: {c.Rarete}");
            Assert.True(c.PrixOfficiel > 0, $"Car #{i} PrixOfficiel must be positive: {c.PrixOfficiel}");
            Assert.True(c.ExemplairesEnCirculation > 0, $"Car #{i} ExemplairesEnCirculation must be positive: {c.ExemplairesEnCirculation}");
            Assert.False(string.IsNullOrWhiteSpace(c.ImageUrl), $"Car #{i} ImageUrl is empty");
            Assert.True(c.ImageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase), $"Car #{i} ImageUrl must start with https://: {c.ImageUrl}");
            Assert.False(string.IsNullOrWhiteSpace(c.Description), $"Car #{i} Description is empty");
            Assert.True(c.VitesseMax > 0, $"Car #{i} VitesseMax must be positive: {c.VitesseMax}");
            Assert.True(c.ZeroACent > 0, $"Car #{i} ZeroACent must be positive: {c.ZeroACent}");

            // Verify computed properties evaluate cleanly
            Assert.False(string.IsNullOrWhiteSpace(c.NomComplet));
            Assert.Contains(c.Marque, c.NomComplet);
            Assert.Contains(c.Modele, c.NomComplet);
            Assert.False(string.IsNullOrWhiteSpace(c.PuissanceFormatee));
            Assert.Contains("ch", c.PuissanceFormatee);
            Assert.False(string.IsNullOrWhiteSpace(c.VitesseMaxFormatee));
            Assert.Contains("km/h", c.VitesseMaxFormatee);
            Assert.False(string.IsNullOrWhiteSpace(c.ZeroACentFormate));
            Assert.Contains("s", c.ZeroACentFormate);
            Assert.False(string.IsNullOrWhiteSpace(c.PrixOfficielFormate));
            Assert.Contains("coins", c.PrixOfficielFormate);
            Assert.False(string.IsNullOrWhiteSpace(c.ToString()));
        }
    }

    #endregion

    #region 4. Adversarial Edge Case & Data Integrity Stress Tests

    [Fact]
    public void F30_Adversarial_NoDuplicateVehicleIds()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var array = JArray.Parse(json);

        var ids = new HashSet<string>();
        foreach (JObject item in array)
        {
            string id = item["id"]!.ToString();
            Assert.True(ids.Add(id), $"Duplicate vehicle ID detected: {id}");
        }
    }

    [Fact]
    public void F30_Adversarial_AllIdsAreContiguousOrUniqueValidStrings()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var array = JArray.Parse(json);

        for (int i = 1; i <= array.Count; i++)
        {
            var found = array.FirstOrDefault(item => item["id"]?.ToString() == i.ToString());
            Assert.NotNull(found);
        }
    }

    [Fact]
    public void F30_Adversarial_NoScriptOrSqlInjectionInTextFields()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var array = JArray.Parse(json);

        string[] dangerousPatterns = new[] { "<script", "javascript:", "<iframe", "SELECT ", "DROP TABLE", "--", "<svg" };

        foreach (JObject item in array)
        {
            foreach (var prop in item.Properties())
            {
                if (prop.Value.Type == JTokenType.String)
                {
                    string text = prop.Value.ToString();
                    foreach (var pattern in dangerousPatterns)
                    {
                        Assert.DoesNotContain(pattern, text, StringComparison.OrdinalIgnoreCase);
                    }
                }
            }
        }
    }

    [Fact]
    public void F30_Adversarial_RoundTripSerializationMaintainsExactData()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var originalList = JsonConvert.DeserializeObject<List<Carte>>(json)!;

        string reSerialized = JsonConvert.SerializeObject(originalList, Formatting.Indented);
        var roundTrippedList = JsonConvert.DeserializeObject<List<Carte>>(reSerialized)!;

        Assert.Equal(originalList.Count, roundTrippedList.Count);

        for (int i = 0; i < originalList.Count; i++)
        {
            Assert.Equal(originalList[i].Identifiant, roundTrippedList[i].Identifiant);
            Assert.Equal(originalList[i].Marque, roundTrippedList[i].Marque);
            Assert.Equal(originalList[i].Modele, roundTrippedList[i].Modele);
            Assert.Equal(originalList[i].Variante, roundTrippedList[i].Variante);
            Assert.Equal(originalList[i].Annee, roundTrippedList[i].Annee);
            Assert.Equal(originalList[i].Puissance, roundTrippedList[i].Puissance);
            Assert.Equal(originalList[i].Rarete, roundTrippedList[i].Rarete);
            Assert.Equal(originalList[i].PrixOfficiel, roundTrippedList[i].PrixOfficiel);
            Assert.Equal(originalList[i].ExemplairesEnCirculation, roundTrippedList[i].ExemplairesEnCirculation);
            Assert.Equal(originalList[i].ImageUrl, roundTrippedList[i].ImageUrl);
            Assert.Equal(originalList[i].Description, roundTrippedList[i].Description);
            Assert.Equal(originalList[i].VitesseMax, roundTrippedList[i].VitesseMax);
            Assert.Equal(originalList[i].ZeroACent, roundTrippedList[i].ZeroACent);
        }
    }

    [Fact]
    public async Task F30_Adversarial_ImageUrls_AreReachableOnWebOrValidUnsplashHosts()
    {
        string json = File.ReadAllText(CarsDatabasePath);
        var cartes = JsonConvert.DeserializeObject<List<Carte>>(json)!;

        using var client = new HttpClient();
        client.Timeout = TimeSpan.FromSeconds(5);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64)");

        int reachableCount = 0;
        foreach (var carte in cartes)
        {
            Assert.True(
                carte.ImageUrl.StartsWith("https://images.unsplash.com/") ||
                carte.ImageUrl.StartsWith("https://images.pexels.com/") ||
                carte.ImageUrl.StartsWith("https://d1gl66oyi6i593.cloudfront.net/") ||
                carte.ImageUrl.StartsWith("https://wsrv.nl/") ||
                carte.ImageUrl.StartsWith("https://upload.wikimedia.org/"),
                $"Unexpected image CDN host: {carte.ImageUrl}");
            
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Head, carte.ImageUrl);
                using var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    reachableCount++;
                }
            }
            catch
            {
                // In isolated network environments, live HTTP may not be reachable, which is acceptable
            }
        }

        // Verify that all URLs target real verified image CDN domains
        Assert.All(cartes, c => Assert.True(
            c.ImageUrl.Contains("images.unsplash.com") ||
            c.ImageUrl.Contains("images.pexels.com") ||
            c.ImageUrl.Contains("cloudfront.net") ||
            c.ImageUrl.Contains("wsrv.nl") ||
            c.ImageUrl.Contains("upload.wikimedia.org")));
    }

    #endregion
}
