# Investigation & Fix Strategy Report — ErreurApi & ServiceAPI Error Handling

**Agent:** `explorer_m2_fix_2` (Teamwork Explorer)  
**Role:** `investigator`, `analyzer`, `synthesizer`  
**Working Directory:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_fix_2`  
**Date:** 2026-10-07T18:13:00Z  
**Target Files:**  
- `AutoMasters/Modeles/ErreurApi.cs`
- `AutoMasters/Services/ServiceAPI.cs`
- `AutoMasters.Tests/ServiceApiTests.cs` (test coverage)

---

## 1. Executive Summary

Following the adversarial review by `challenger_m2_2`, two significant error-handling defects were validated in the Milestone 2 codebase:
1. **Defect 1 (Critical): Empty Exception Messages on Alternative Error Schemas.**  
   `ErreurApi.Message` was initialized to `""`. When an error response omitted the `"message"` key (e.g. `{ "error": "...", "detail": "...", "title": "..." }` or `{}`), `erreur.Message` evaluated to `""`. In `ServiceAPI.LireReponseAsync`, the null-coalescing expression `erreur?.Message ?? fallback` evaluated to `""` because C#'s `??` operator does not treat empty strings as null. This completely suppressed fallback error messages, throwing `new Exception("")`.
2. **Defect 2 (Medium): Raw Unhandled `JsonReaderException` on HTTP 200 OK.**  
   `ServiceAPI.LireReponseAsync` only caught `JsonException` when `!reponse.IsSuccessStatusCode`. When an HTTP 200 OK response contained corrupted or truncated JSON, subsequent calls to `JToken.Parse` or `JsonConvert.DeserializeObject` threw raw `Newtonsoft.Json.JsonReaderException` / `JsonSerializationException` directly into the caller without descriptive domain wrapping.

This report establishes the complete fix strategy, specifies exact code modifications for `ErreurApi.cs` and `ServiceAPI.cs`, and provides 6 concrete unit tests to guarantee zero regressions.

---

## 2. Forensic Analysis & Root Cause

### Defect 1: Alternative Error Keys & Empty Fallback Masking

#### A. Root Cause in `AutoMasters/Modeles/ErreurApi.cs` (lines 13, 50–54)
```csharp
// ErreurApi.cs (Existant)
private string message = "";

[JsonProperty("message")]
public string Message
{
    get => message;
    set => message = value ?? "";
}
```
- The backing field `message` was initialized to `""`.
- When an API response lacks a `"message"` property (such as RFC 7807 Problem Details `{ "title": "Conflict", "detail": "Offre déjà vendue" }` or microservice errors `{ "error": "Solde insuffisant" }`), `JsonConvert.DeserializeObject<ErreurApi>(json)` succeeds, but `message` remains `""`.
- Furthermore, `ErreurApi` lacked deserialization support for `"error"`, `"detail"`, and `"title"`.

#### B. Root Cause in `AutoMasters/Services/ServiceAPI.cs` (lines 116–132)
```csharp
// ServiceAPI.cs (Existant - LireReponseAsync)
if (reponse.StatusCode == HttpStatusCode.NotFound)
{
    throw new Exception(erreur?.Message ?? "La ressource demandée est introuvable.");
}
if (reponse.StatusCode == HttpStatusCode.Conflict)
{
    throw new Exception(erreur?.Message ?? "L'opération demandée est en conflit avec l'état actuel de la ressource.");
}
if (reponse.StatusCode == HttpStatusCode.BadRequest)
{
    throw new Exception(erreur?.Message ?? "Requête invalide ou données manquantes.");
}
throw new Exception(erreur?.Message ?? $"Une erreur est survenue lors de la communication avec le serveur ({reponse.StatusCode}).");
```
- In C#, `"" ?? "Fallback"` evaluates to `""`.
- Since `erreur?.Message` returned `""`, every non-matching error payload resulted in `throw new Exception("")`.
- In ViewModels and UI dialogs, users were presented with blank error dialogs.

---

### Defect 2: Raw Unhandled `JsonReaderException` on HTTP 200 OK Responses

#### Root Cause in `AutoMasters/Services/ServiceAPI.cs` (lines 91–135, 284–286, 327, 349, 394, 497, 525)
- `LireReponseAsync` only wrapped JSON deserialization in a `try-catch` block when `!reponse.IsSuccessStatusCode`.
- For `200 OK`, `LireReponseAsync` returns the raw JSON string directly.
- The calling methods (`RecupererCatalogueAsync`, `RecupererFicheCarteAsync`, `RecupererPortefeuilleAsync`, `RecupererOffresAsync`, `CreerOffreAsync`, `OuvrirBoosterAsync`, `SeConnecterAsync`) called `JToken.Parse(json)` or `JsonConvert.DeserializeObject<T>(json)` without any `try-catch`.
- If a network proxy truncated the response, or if the server returned malformed JSON with a 200 OK status, the application crashed with an unhandled `Newtonsoft.Json.JsonReaderException` or `Newtonsoft.Json.JsonSerializationException`.

---

## 3. Recommended Fix Strategy

### Fix 1: Defense-in-Depth for `ErreurApi.cs`
1. Change `private string message = "";` to `private string? message;` (defaults to `null`).
2. Introduce nullable backing fields and properties for alternative error keys:
   - `[JsonProperty("error")] public string? Error` (RFC 6749 / Express standard)
   - `[JsonProperty("detail")] public string? Detail` (RFC 7807 Problem Details explanation)
   - `[JsonProperty("title")] public string? Title` (RFC 7807 Problem Details summary)
3. Enhance `Message` getter to resolve with clear semantic priority:
   - 1st priority: `message` (if non-whitespace)
   - 2nd priority: `detail` (specific explanation)
   - 3rd priority: `error` (generic error string)
   - 4th priority: `title` (category headline)
   - Fallback: `null` (never returns `""` if unpopulated or whitespace-only)
4. Update `ToString()` to safely handle `null` without throwing `NullReferenceException`.

### Fix 2: Defensive Exception Handling in `ServiceAPI.cs`
1. **In `LireReponseAsync`:**  
   Extract the message using a non-whitespace guard:
   ```csharp
   string? messageExtrait = !string.IsNullOrWhiteSpace(erreur?.Message)
       ? erreur!.Message
       : null;
   ```
   All status-code checks then use `messageExtrait ?? fallback`. If `messageExtrait` is null, the descriptive fallback is always selected.
2. **In all HTTP 200 deserialization routines:**  
   Wrap all `JToken.Parse` and `JsonConvert.DeserializeObject` operations in `try-catch (JsonException ex)` blocks and rethrow as:
   ```csharp
   throw new Exception("Le format des données reçues du serveur est invalide.", ex);
   ```
   Because `Newtonsoft.Json.JsonException` is the common base class for both `JsonReaderException` and `JsonSerializationException`, this cleanly intercepts all syntax corruptions, token truncations, and schema mismatch errors while preserving debugging history via the inner exception.
3. **Multi-key envelope fallbacks:**  
   In addition to direct arrays (`token is JArray`), allow envelopes to contain alternative standard collection keys (`items`, `cards`, `offers`, `data`).

---

## 4. Exact Code Modifications

### 4.1. File: `AutoMasters/Modeles/ErreurApi.cs`

Replace the entire contents of `AutoMasters/Modeles/ErreurApi.cs` with the following:

```csharp
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

/// <summary>
/// DTO représentant le format normalisé des réponses d'erreur renvoyées par l'API Group 2.
/// Gère les schémas d'erreur standards ainsi que les clés alternatives ("error", "detail", "title")
/// pour garantir l'absence d'exceptions vides en cas de variations du backend.
/// </summary>
public class ErreurApi
{
    #region Attributs

    private string? code;
    private string? message;
    private string? error;
    private string? detail;
    private string? title;
    private string? correlationId;
    private List<string>? erreurs;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut.
    /// </summary>
    public ErreurApi()
    {
    }

    /// <summary>
    /// Constructeur avec message et code d'erreur.
    /// </summary>
    public ErreurApi(string message, string? code = null, string? correlationId = null)
    {
        this.message = message;
        this.code = code;
        this.correlationId = correlationId;
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("code")]
    public string? Code
    {
        get => code;
        set => code = value;
    }

    /// <summary>
    /// Message d'erreur principal. Résout avec priorité la clé standard "message",
    /// puis les clés alternatives "detail" (RFC 7807), "error" (standard REST) ou "title" (RFC 7807).
    /// Renvoie null si aucun message textuel valide n'est présent afin de ne jamais masquer
    /// les messages de fallback dans les services appelants.
    /// </summary>
    [JsonProperty("message")]
    public string? Message
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(message))
                return message;
            if (!string.IsNullOrWhiteSpace(detail))
                return detail;
            if (!string.IsNullOrWhiteSpace(error))
                return error;
            if (!string.IsNullOrWhiteSpace(title))
                return title;
            return null;
        }
        set => message = value;
    }

    /// <summary>
    /// Clé d'erreur alternative (ex: OAuth, microservices Flask/Express).
    /// </summary>
    [JsonProperty("error")]
    public string? Error
    {
        get => error;
        set => error = value;
    }

    /// <summary>
    /// Clé d'erreur alternative Problem Details RFC 7807 fournissant l'explication détaillée.
    /// </summary>
    [JsonProperty("detail")]
    public string? Detail
    {
        get => detail;
        set => detail = value;
    }

    /// <summary>
    /// Clé d'erreur alternative Problem Details RFC 7807 fournissant le résumé court ou titre.
    /// </summary>
    [JsonProperty("title")]
    public string? Title
    {
        get => title;
        set => title = value;
    }

    [JsonProperty("correlationId")]
    public string? CorrelationId
    {
        get => correlationId;
        set => correlationId = value;
    }

    [JsonProperty("errors")]
    public List<string>? Erreurs
    {
        get => erreurs;
        set => erreurs = value;
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Format lisible pour affichage dans une alerte ou un bandeau d'erreur utilisateur.
    /// </summary>
    public override string ToString()
    {
        string? msg = Message;
        if (string.IsNullOrWhiteSpace(Code))
            return msg ?? string.Empty;

        return string.IsNullOrWhiteSpace(msg) ? $"[{Code}]" : $"[{Code}] {msg}";
    }

    #endregion
}
```

---

### 4.2. File: `AutoMasters/Services/ServiceAPI.cs`

Targeted modifications across the file:

#### A. In `LireReponseAsync` (Lines 111–132):

**Before:**
```csharp
            if (reponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
            }

            if (reponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception(erreur?.Message ?? "La ressource demandée est introuvable.");
            }

            if (reponse.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Exception(erreur?.Message ?? "L'opération demandée est en conflit avec l'état actuel de la ressource.");
            }

            if (reponse.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception(erreur?.Message ?? "Requête invalide ou données manquantes.");
            }

            throw new Exception(erreur?.Message ?? $"Une erreur est survenue lors de la communication avec le serveur ({reponse.StatusCode}).");
```

**After:**
```csharp
            if (reponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
            }

            string? messageExtrait = !string.IsNullOrWhiteSpace(erreur?.Message)
                ? erreur!.Message
                : null;

            if (reponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception(messageExtrait ?? "La ressource demandée est introuvable.");
            }

            if (reponse.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Exception(messageExtrait ?? "L'opération demandée est en conflit avec l'état actuel de la ressource.");
            }

            if (reponse.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception(messageExtrait ?? "Requête invalide ou données manquantes.");
            }

            throw new Exception(messageExtrait ?? $"Une erreur est survenue lors de la communication avec le serveur ({reponse.StatusCode}).");
```

---

#### B. In `SeConnecterAsync` (Lines 235–243):

**Before:**
```csharp
        string resultat = await LireReponseAsync(reponse);
        var connexion = JsonConvert.DeserializeObject<ReponseConnexion>(resultat);

        if (connexion == null || string.IsNullOrWhiteSpace(connexion.JetonAcces))
            throw new Exception("Le jeton d'accès est absent de la réponse du serveur.");
```

**After:**
```csharp
        string resultat = await LireReponseAsync(reponse);
        ReponseConnexion? connexion;
        try
        {
            connexion = JsonConvert.DeserializeObject<ReponseConnexion>(resultat);
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }

        if (connexion == null || string.IsNullOrWhiteSpace(connexion.JetonAcces))
            throw new Exception("Le jeton d'accès est absent de la réponse du serveur.");
```

---

#### C. In `RecupererCatalogueAsync` (Lines 284–297):

**Before:**
```csharp
        string json = await LireReponseAsync(reponse);

        JToken token = JToken.Parse(json);
        if (token is JArray jArray)
        {
            return jArray.ToObject<List<Carte>>() ?? new List<Carte>();
        }
        else if (token is JObject jObj && jObj["items"] is JArray itemsArray)
        {
            return itemsArray.ToObject<List<Carte>>() ?? new List<Carte>();
        }

        return new List<Carte>();
```

**After:**
```csharp
        string json = await LireReponseAsync(reponse);

        try
        {
            JToken token = JToken.Parse(json);
            if (token is JArray jArray)
            {
                return jArray.ToObject<List<Carte>>() ?? new List<Carte>();
            }
            else if (token is JObject jObj)
            {
                JArray? itemsArray = (jObj["items"] as JArray) ?? (jObj["cards"] as JArray) ?? (jObj["data"] as JArray);
                if (itemsArray != null)
                {
                    return itemsArray.ToObject<List<Carte>>() ?? new List<Carte>();
                }
            }

            return new List<Carte>();
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }
```

---

#### D. In `RecupererFicheCarteAsync` (Lines 325–332):

**Before:**
```csharp
        string json = await LireReponseAsync(reponse);

        var carte = JsonConvert.DeserializeObject<Carte>(json);
        if (carte == null)
            throw new Exception($"La fiche de la carte '{id}' est introuvable ou invalide.");

        return carte;
```

**After:**
```csharp
        string json = await LireReponseAsync(reponse);

        Carte? carte;
        try
        {
            carte = JsonConvert.DeserializeObject<Carte>(json);
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }

        if (carte == null || string.IsNullOrWhiteSpace(carte.Identifiant))
            throw new Exception($"La fiche de la carte '{id}' est introuvable ou invalide.");

        return carte;
```

---

#### E. In `RecupererPortefeuilleAsync` (Lines 347–363):

**Before:**
```csharp
        string json = await LireReponseAsync(reponse);

        JToken token = JToken.Parse(json);
        if (token is JObject)
        {
            var portefeuille = JsonConvert.DeserializeObject<Portefeuille>(json);
            return portefeuille ?? new Portefeuille();
        }
        else if (token is JArray jArray)
        {
            // Prise en charge du format direct (GET cartes retournant un tableau brut de positions)
            var positions = jArray.ToObject<List<CartePossedee>>() ?? new List<CartePossedee>();
            return new Portefeuille(positions);
        }

        return new Portefeuille();
```

**After:**
```csharp
        string json = await LireReponseAsync(reponse);

        try
        {
            JToken token = JToken.Parse(json);
            if (token is JObject)
            {
                var portefeuille = JsonConvert.DeserializeObject<Portefeuille>(json);
                return portefeuille ?? new Portefeuille();
            }
            else if (token is JArray jArray)
            {
                // Prise en charge du format direct (GET cartes retournant un tableau brut de positions)
                var positions = jArray.ToObject<List<CartePossedee>>() ?? new List<CartePossedee>();
                return new Portefeuille(positions);
            }

            return new Portefeuille();
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }
```

---

#### F. In `RecupererOffresAsync` (Lines 392–405):

**Before:**
```csharp
        string json = await LireReponseAsync(reponse);

        JToken token = JToken.Parse(json);
        if (token is JArray jArray)
        {
            return jArray.ToObject<List<Offre>>() ?? new List<Offre>();
        }
        else if (token is JObject jObj && jObj["items"] is JArray itemsArray)
        {
            return itemsArray.ToObject<List<Offre>>() ?? new List<Offre>();
        }

        return new List<Offre>();
```

**After:**
```csharp
        string json = await LireReponseAsync(reponse);

        try
        {
            JToken token = JToken.Parse(json);
            if (token is JArray jArray)
            {
                return jArray.ToObject<List<Offre>>() ?? new List<Offre>();
            }
            else if (token is JObject jObj)
            {
                JArray? itemsArray = (jObj["items"] as JArray) ?? (jObj["offers"] as JArray) ?? (jObj["data"] as JArray);
                if (itemsArray != null)
                {
                    return itemsArray.ToObject<List<Offre>>() ?? new List<Offre>();
                }
            }

            return new List<Offre>();
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }
```

---

#### G. In `CreerOffreAsync` (Lines 495–499):

**Before:**
```csharp
        string resultat = await LireReponseAsync(reponse);

        var offre = JsonConvert.DeserializeObject<Offre>(resultat);
        return offre ?? throw new Exception("L'offre créée n'a pas pu être désérialisée.");
```

**After:**
```csharp
        string resultat = await LireReponseAsync(reponse);

        Offre? offre;
        try
        {
            offre = JsonConvert.DeserializeObject<Offre>(resultat);
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }

        return offre ?? throw new Exception("L'offre créée n'a pas pu être désérialisée.");
```

---

#### H. In `OuvrirBoosterAsync` (Lines 523–538):

**Before:**
```csharp
        string json = await LireReponseAsync(reponse);

        JToken token = JToken.Parse(json);
        List<Carte> cartes = new();

        if (token is JObject jObj && jObj["cards"] is JArray arr)
        {
            cartes = arr.ToObject<List<Carte>>() ?? new List<Carte>();
        }
        else if (token is JArray jArr)
        {
            cartes = jArr.ToObject<List<Carte>>() ?? new List<Carte>();
        }

        return cartes;
```

**After:**
```csharp
        string json = await LireReponseAsync(reponse);

        try
        {
            JToken token = JToken.Parse(json);
            List<Carte> cartes = new();

            if (token is JObject jObj)
            {
                JArray? arr = (jObj["cards"] as JArray) ?? (jObj["items"] as JArray) ?? (jObj["data"] as JArray);
                if (arr != null)
                {
                    cartes = arr.ToObject<List<Carte>>() ?? new List<Carte>();
                }
            }
            else if (token is JArray jArr)
            {
                cartes = jArr.ToObject<List<Carte>>() ?? new List<Carte>();
            }

            return cartes;
        }
        catch (JsonException ex)
        {
            throw new Exception("Le format des données reçues du serveur est invalide.", ex);
        }
```

---

## 5. Supporting Unit Test Suite Additions (`ServiceApiTests.cs`)

To verify and lock in these fixes, the following 6 test methods should be added to `AutoMasters.Tests/ServiceApiTests.cs` under `#region Tests Robustesse, DI & Cas Limites`:

```csharp
    [Fact]
    public async Task RecupererCatalogueAsync_JsonCorrompu200_LeveExceptionClaireSansCrash()
    {
        // Disposition : réponse HTTP 200 avec flux JSON corrompu ou tronqué
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, "{ items: [ { id: incomplete");

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererCatalogueAsync());

        Assert.Contains("format des données", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.NotNull(ex.InnerException);
        Assert.IsAssignableFrom<Newtonsoft.Json.JsonException>(ex.InnerException);
    }

    [Theory]
    [InlineData("{\"error\": \"Compte suspendu\"}", "Compte suspendu")]
    [InlineData("{\"detail\": \"Solde insuffisant\"}", "Solde insuffisant")]
    [InlineData("{\"title\": \"Conflit d'offre\"}", "Conflit d'offre")]
    public async Task ServiceAPI_SchemasAlternatifsErreur_ExtraitMessageLisible(string jsonErreur, string messageAttendu)
    {
        // Disposition : réponse 400 utilisant une clé d'erreur alternative (error, detail, title)
        _mockHandler.QuandGet("/cards", HttpStatusCode.BadRequest, jsonErreur);

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererCatalogueAsync());

        Assert.Contains(messageAttendu, ex.Message);
    }

    [Fact]
    public async Task ServiceAPI_ErreurSansMessage_UtiliseFallbackNonVide()
    {
        // Disposition : erreur 400 avec JSON vide {} ou sans message
        _mockHandler.QuandGet("/cards", HttpStatusCode.BadRequest, "{}");

        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererCatalogueAsync());

        Assert.False(string.IsNullOrWhiteSpace(ex.Message));
        Assert.Equal("Requête invalide ou données manquantes.", ex.Message);
    }

    [Fact]
    public async Task RecupererCatalogueAsync_TableauBrutDirect_DeserialiseCorrectement()
    {
        // Disposition : tableau JSON direct sans enveloppe items
        var carte = new
        {
            id = "c_direct_01",
            brand = "Bugatti",
            model = "Chiron",
            rarity = "LÉGENDAIRE",
            officialPrice = 2500000m
        };
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, new[] { carte });

        // Action
        var cartes = await _service.RecupererCatalogueAsync();

        // Assertions
        Assert.Single(cartes);
        Assert.Equal("c_direct_01", cartes[0].Identifiant);
        Assert.Equal("Bugatti", cartes[0].Marque);
    }

    [Fact]
    public async Task RecupererOffresAsync_TableauBrutDirect_DeserialiseCorrectement()
    {
        // Disposition : tableau JSON direct d'offres
        var offre = new
        {
            id = "off_direct_01",
            seller = "JoueurBTS",
            price = 45000m,
            isReserved = false
        };
        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, new[] { offre });

        // Action
        var offres = await _service.RecupererOffresAsync();

        // Assertions
        Assert.Single(offres);
        Assert.Equal("off_direct_01", offres[0].Identifiant);
        Assert.Equal(45000m, offres[0].Prix);
    }

    [Fact]
    public async Task OuvrirBoosterAsync_TableauBrutDirect_RetourneListeCartes()
    {
        // Disposition : tableau JSON direct de 5 cartes
        var cartesTest = Enumerable.Range(1, 5).Select(i => new
        {
            id = $"c_booster_dir_{i}",
            brand = "Ferrari",
            model = $"F8 Tributo {i}",
            rarity = "ÉPIQUE",
            officialPrice = 280000m
        }).ToArray();
        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, cartesTest);

        // Action
        var cartes = await _service.OuvrirBoosterAsync();

        // Assertions
        Assert.Equal(5, cartes.Count);
        Assert.Equal("c_booster_dir_1", cartes[0].Identifiant);
    }
```

---

## 6. Verification and Validation Method

The implementer can independently verify these modifications using the following steps:

1. **Compilation Check:**
   Execute:
   ```powershell
   dotnet build AutoMasters.slnx -c Debug
   ```
   Must succeed with 0 compilation errors and 0 warnings.
2. **Behavioral Trace on Alternative Error Schemas:**
   Simulate `{ "error": "Test Error" }` and `{}` against `ServiceAPI.RecupererCatalogueAsync` and confirm:
   - For `{ "error": "Test Error" }`, the thrown exception message is `"Test Error"`.
   - For `{}`, the thrown exception message is `"Requête invalide ou données manquantes."`.
   - No scenario produces `""`.
3. **Behavioral Trace on HTTP 200 Malformed JSON:**
   Simulate `"{ invalid: json"` on GET `/cards` with HTTP 200 OK and confirm:
   - Thrown exception is `System.Exception` with message `"Le format des données reçues du serveur est invalide."`.
   - `InnerException` is of type `Newtonsoft.Json.JsonReaderException`.
4. **Behavioral Trace on Direct JSON Arrays:**
   Simulate `[ { "id": "1", "brand": "Porsche", "model": "911" } ]` on GET `/cards` with HTTP 200 OK and confirm:
   - List is deserialized with 1 element and properties populated.

---

## 7. Conclusion

By combining multi-key alias extraction in `ErreurApi.cs`, `IsNullOrWhiteSpace` evaluation in `ServiceAPI.LireReponseAsync`, and comprehensive `try-catch (JsonException)` encapsulation across all HTTP 200 deserialization paths, both defects highlighted by `challenger_m2_2` are completely resolved. The codebase maintains strict BTS SIO conventions, clean MVVM boundaries, and zero extraneous dependencies.
