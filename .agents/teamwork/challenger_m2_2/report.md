# Adversarial Challenge Report — Milestone 2 Unit Test Suite & Model Deserialization

**Agent:** `challenger_m2_2` (Empirical Challenger)  
**Role:** `critic`, `specialist`  
**Working Directory:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\challenger_m2_2`  
**Date:** 2026-10-07T18:06:00Z  
**Verdict:** **REQUEST_CHANGES**

---

## 1. Executive Summary

Milestone 2 was delivered by `worker_m2` comprising 18 domain models in `AutoMasters/Modeles/`, a single `Services/ServiceAPI.cs` file, and an xUnit test project `AutoMasters.Tests` with 27 unit tests and `MockHttpMessageHandler`.

An adversarial audit and forensic stress test of the unit test suite and model deserialization was conducted across four dimensions:
1. **Test Assertion Fidelity:** Evaluated all 27 unit tests in `ServiceApiTests.cs` for tautological or superficial asserts.
2. **Error Handling & Resilience:** Evaluated behavior when receiving corrupted JSON or unexpected API error schemas.
3. **Model Deserialization Flexibility:** Evaluated handling and test coverage for direct JSON arrays versus nested envelopes.
4. **Test Execution Reliability:** Evaluated project configuration, CLI execution constraints, and environment behavior.

**Final Verdict: REQUEST_CHANGES.**  
While the existing 27 unit tests are genuine and contain 0 tautological asserts, critical defects and significant verification gaps were identified:
- **Critical Bug in Unexpected API Error Handling:** `ErreurApi.Message` defaults to `""` (empty string). In `ServiceAPI.LireReponseAsync`, `throw new Exception(erreur?.Message ?? fallback)` evaluates to `""` when the API returns an unexpected error schema (e.g. `{ "error": "...", "detail": "..." }` or `{}`). The fallback message is completely bypassed, throwing an exception with an empty string message.
- **Zero Unit Tests for Direct Array Payloads:** `ServiceAPI.cs` includes branches for direct JSON arrays (`token is JArray`), but in `ServiceApiTests.cs`, 100% of collection tests use nested envelopes (`items`, `positions`, `cards`). Direct array deserialization is completely unexercised by unit tests.
- **Unhandled Corrupted JSON on Success Responses:** HTTP 200 OK responses with corrupted JSON throw unhandled `Newtonsoft.Json.JsonReaderException` across all methods, with zero unit test coverage for malformed payloads on 200 OK.
- **Envelope Key Rigidity:** Endpoints strictly check hardcoded property names (`items` in catalog and market, `cards` in booster) and silently return empty lists if standard REST alternatives (`cards`, `offers`, `data`) are returned.

---

## 2. Detailed Findings & Challenge Analysis

### Challenge 1 (CRITICAL): Unexpected API Error Schemas Cause Empty Exception Messages
- **File & Lines:** `AutoMasters/Modeles/ErreurApi.cs` (lines 13, 50-55), `AutoMasters/Services/ServiceAPI.cs` (lines 116-132)
- **Mechanism:**
  In `ErreurApi.cs`:
  ```csharp
  private string message = "";

  [JsonProperty("message")]
  public string Message
  {
      get => message;
      set => message = value ?? "";
  }
  ```
  In `ServiceAPI.cs` (`LireReponseAsync`):
  ```csharp
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
- **Failure Scenario:**
  If the Group 2 API or an upstream reverse proxy returns an error schema that does not have a top-level `"message"` key (such as RFC 7807 ProblemDetails `{ "title": "Validation Error", "detail": "Paramètre invalide" }`, `{ "error": "Requête invalide" }`, or `{}`):
  1. `JsonConvert.DeserializeObject<ErreurApi>(json)` succeeds and returns a non-null `ErreurApi` instance.
  2. Because `"message"` was not in the JSON, `erreur.Message` retains its default value `""`.
  3. In C#, the null-coalescing operator `??` only checks for `null`. It does NOT check for empty strings! `"" ?? "Fallback"` evaluates to `""`.
  4. The exception thrown is `new Exception("")`.
  5. The fallback message (`"Requête invalide ou données manquantes."`, etc.) is completely lost! ViewModels or UI dialogues displaying `ex.Message` display blank text to the user.
- **Blast Radius:** High — Any API error with an alternate schema produces silent, blank exceptions in the UI.
- **Mitigation:**
  1. In `ErreurApi.cs`: Initialize `private string? message;` as nullable (`null` by default). Add aliases:
     ```csharp
     [JsonProperty("error")]
     private string? ErrorAlias { set { if (string.IsNullOrWhiteSpace(message)) message = value; } }

     [JsonProperty("detail")]
     private string? DetailAlias { set { if (string.IsNullOrWhiteSpace(message)) message = value; } }
     ```
  2. In `ServiceAPI.cs`: Use `!string.IsNullOrWhiteSpace(erreur?.Message) ? erreur!.Message : fallback` rather than `??`.

---

### Challenge 2 (HIGH): Complete Absence of Direct Array Deserialization Tests
- **File & Lines:** `AutoMasters.Tests/ServiceApiTests.cs` (lines 134-222, 277-402, 406-444, 584-626), `AutoMasters/Services/ServiceAPI.cs` (lines 287-295, 350-362, 395-403, 528-536)
- **Mechanism:**
  `ServiceAPI.cs` contains code to support both direct arrays and nested envelopes:
  - `RecupererCatalogueAsync`: `if (token is JArray jArray) return jArray.ToObject<List<Carte>>() ...`
  - `RecupererPortefeuilleAsync`: `else if (token is JArray jArray) return new Portefeuille(positions) ...`
  - `RecupererOffresAsync`: `if (token is JArray jArray) return jArray.ToObject<List<Offre>>() ...`
  - `OuvrirBoosterAsync`: `else if (token is JArray jArr) cartes = jArr.ToObject<List<Carte>>() ...`
- **Failure Scenario:**
  In `ServiceApiTests.cs`, every single test for these 4 methods exclusively supplies a nested envelope:
  - Catalog test (line 138): `{ items = new[] { ... }, page = 1, ... }`
  - Portfolio test (line 282): `{ positions = new[] { ... } }`
  - Market offers test (line 410): `{ items = new[] { ... }, total = 1 }`
  - Booster unpack test (line 588): `{ cards = new[] { ... }, remainingQuota = 2 }`
  **Zero unit tests** supply a direct array (`[ { ... } ]`).
- **Blast Radius:** Medium to High — If the real backend or an endpoint version returns direct arrays (as `cars_database.json` and many BTS SIO prototypes do), that code path has zero regression protection or test validation.
- **Mitigation:**
  Add 4 dedicated xUnit tests in `ServiceApiTests.cs`:
  - `RecupererCatalogueAsync_TableauBrutDirect_DeserialiseCorrectement`
  - `RecupererOffresAsync_TableauBrutDirect_DeserialiseCorrectement`
  - `RecupererPortefeuilleAsync_TableauBrutDirect_CreePortefeuilleEtCalculeValorisation`
  - `OuvrirBoosterAsync_TableauBrutDirect_RetourneListeCartes`

---

### Challenge 3 (MEDIUM): Unhandled Corrupted JSON on HTTP 200 OK Responses
- **File & Lines:** `AutoMasters/Services/ServiceAPI.cs` (lines 91-135, 284-286, 348-350, 392-394, 524-526)
- **Mechanism:**
  `LireReponseAsync` only wraps JSON parsing in a `try-catch (JsonException)` block when `!reponse.IsSuccessStatusCode`. When `reponse.IsSuccessStatusCode == true`, the raw JSON string is returned directly.
  Methods like `RecupererCatalogueAsync` then call `JToken.Parse(json)` directly without any `try-catch`.
- **Failure Scenario:**
  If the HTTP server returns 200 OK with truncated JSON, malformed syntax (e.g. `{"items": [ {`), or invalid characters:
  `JToken.Parse(json)` throws an unhandled `Newtonsoft.Json.JsonReaderException`.
  In `ServiceApiTests.cs`, Test 24 only tested HTML on an HTTP 502 Bad Gateway. No unit test verifies corrupted JSON behavior on a 200 OK response.
- **Blast Radius:** Medium — Causes unhandled framework parser exceptions rather than user-friendly error messages when network stream truncates or proxy corrupts data.
- **Mitigation:**
  Wrap `JToken.Parse` and `JsonConvert.DeserializeObject` calls in `try-catch (JsonException ex)` in `ServiceAPI.cs` and throw a descriptive `Exception("Le format des données reçues du serveur est invalide.", ex)`. Add a test `RecupererCatalogueAsync_JsonCorrompu200_LeveExceptionClaireSansCrash`.

---

### Challenge 4 (MEDIUM): Envelope Key Rigidity in Collection Endpoints
- **File & Lines:** `AutoMasters/Services/ServiceAPI.cs` (lines 291, 399, 528)
- **Mechanism:**
  - `RecupererCatalogueAsync` only checks `jObj["items"]`. If an envelope uses `{ "cards": [...] }` or `{ "data": [...] }`, it silently returns an empty list (`new List<Carte>()`).
  - `RecupererOffresAsync` only checks `jObj["items"]`. If an envelope uses `{ "offers": [...] }`, it silently returns an empty list.
  - `OuvrirBoosterAsync` only checks `jObj["cards"]`. If an envelope uses `{ "items": [...] }`, it silently returns an empty list.
- **Mitigation:**
  Support fallback keys:
  - Catalog: `jObj["items"] as JArray ?? jObj["cards"] as JArray ?? jObj["data"] as JArray`
  - Offers: `jObj["items"] as JArray ?? jObj["offers"] as JArray ?? jObj["data"] as JArray`
  - Booster: `jObj["cards"] as JArray ?? jObj["items"] as JArray ?? jObj["data"] as JArray`

---

### Challenge 5 (LOW): `RecupererFicheCarteAsync` Silently Returns Default Empty Model on Incompatible JSON
- **File & Lines:** `AutoMasters/Services/ServiceAPI.cs` (lines 327-331)
- **Mechanism:**
  ```csharp
  var carte = JsonConvert.DeserializeObject<Carte>(json);
  if (carte == null)
      throw new Exception($"La fiche de la carte '{id}' est introuvable ou invalide.");
  return carte;
  ```
  If the server returns `{}` or `{ "card": { ... } }`, `JsonConvert.DeserializeObject<Carte>(json)` instantiates `new Carte()`. `carte` is non-null, but `carte.Identifiant` is `""`. The null check does not trigger.
- **Mitigation:**
  Check `if (carte == null || string.IsNullOrWhiteSpace(carte.Identifiant)) throw new Exception(...)`.

---

## 3. Test Assertion Quality Audit

| Test Name | Assertion Type | Target Verified | Verdict |
|---|---|---|---|
| `SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer` | State & Payload | Bearer header, token value, HTTP POST, credentials | GENUINE |
| `SeConnecterAsync_IdentifiantsInvalides_401_LeveException` | Exception | HTTP 401 message "Connexion refusée" | GENUINE |
| `SeConnecterAsync_ErreurServeur_500_ExtraitMessageErreurApi` | Exception | HTTP 500 error extraction from `ErreurApi` | GENUINE |
| `SeConnecterAsync_JetonManquantDansReponse_LeveException` | Exception | Missing JWT validation | GENUINE |
| `SeConnecterAsync_PanneReseau_LeveHttpRequestException` | Exception | `HttpRequestException` on offline / socket drop | GENUINE |
| `RecupererCatalogueAsync_Succes_DeserialiseListeCartesNewtonsoft` | Object State | 9 distinct properties on deserialized `Carte` | GENUINE |
| `RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl` | HTTP Query | URI query parameters `search` and `rarity` | GENUINE |
| `RecupererCatalogueAsync_CatalogueVide_RetourneListeNonNullEtVide` | Object State | Empty list handling | GENUINE |
| `RecupererFicheCarteAsync_CarteExistante_RetourneCaracteristiquesTechniques` | Object State | Technical specs (`topSpeedKmH`, `zeroToHundred`, etc.) | GENUINE |
| `RecupererFicheCarteAsync_CarteInexistante_404_LeveException` | Exception | HTTP 404 message extraction | GENUINE |
| `RecupererPortefeuilleAsync_Succes_DeserialisePositionsEtCalculeValorisation` | Model Math | Positions deserialization and computed aggregate valuations | GENUINE |
| `RecupererPortefeuilleAsync_CalculPlusMoinsValue_CalculExactParQuantite` | Model Math | Multiplier logic for quantity * unit P&L | GENUINE |
| `RecupererPortefeuilleAsync_GarageVide_RetourneZeroCoins` | Object State | 0 valuation and empty collection | GENUINE |
| `RecupererOffresAsync_Succes_DeserialiseOffresActives` | Object State | Offer deserialization (id, seller, price, status) | GENUINE |
| `AcheterOffreAsync_Succes_TransmetEnTeteIdempotencyKey` | HTTP Header | Return bool, `Idempotency-Key` header, UUID format | GENUINE |
| `AcheterOffreAsync_SoldeInsuffisant_400_LeveException` | Exception | HTTP 400 insufficient funds error | GENUINE |
| `AcheterOffreAsync_OffreDejaVendue_409_LeveException` | Exception | HTTP 409 conflict error | GENUINE |
| `ReserverOffreAsync_Succes_VerrouilleOffreEtConfirme` | HTTP Request | Endpoint path containing "reservations" | GENUINE |
| `ReserverOffreAsync_DejaReservee_409_LeveException` | Exception | HTTP 409 reservation conflict error | GENUINE |
| `CreerOffreAsync_DonneesValides_RetourneOffreCreee` | Object State | Created offer properties | GENUINE |
| `CreerOffreAsync_PrixNegatif_400_LeveException` | Validation | Client/server negative price validation | GENUINE |
| `OuvrirBoosterAsync_QuotaDisponible_Retourne5Cartes` | Collection | Exactly 5 cards returned | GENUINE |
| `OuvrirBoosterAsync_QuotaEpuise_400_LeveException` | Exception | HTTP 400 quota exhaustion message | GENUINE |
| `ServiceAPI_ReponseHtml502_GereErreurServeurSansCrashJson` | Exception | HTML error page handled without raw JsonReaderException | GENUINE |
| `ServiceAPI_InjectableHandler_PermetTestsCompletsHorsLigne` | DI & Config | HttpClient BaseAddress and Timeout | GENUINE |
| `RecupererCatalogueAsync_CaracteresSpeciauxRecherche_EchappeUrl` | URL Encoding | Proper URI encoding for special characters | GENUINE |
| `AcheterOffreAsync_FormatCleIdempotence_EstUuidV4Valide` | Cryptography/UUID | RFC 4122 UUIDv4 validation | GENUINE |

**Tautological Assertion Rate:** 0 / 27 (0%) — All existing assertions test genuine business logic.

---

## 4. Test Execution Reliability & CLI Verification

- **Command Attempted:** `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj`
- **Result:** Environment permission check denied execution:
  `Encountered error in tool execution: permission check failed for command "dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj": Permission prompt for action 'command' on target 'dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj' was denied.`
- **Inspection Findings:**
  - Solution file `AutoMasters.slnx` properly maps `AutoMasters.Tests/AutoMasters.Tests.csproj`.
  - Project target frameworks match (`net10.0-windows10.0.19041.0`).
  - `AutoMasters.Tests.csproj` properly specifies `<IsTestProject>true</IsTestProject>`, references `xunit` 2.9.3, `xunit.runner.visualstudio` 3.0.2, and `Microsoft.NET.Test.Sdk` 17.13.0.
  - As verified by `worker_m2`, `dotnet build AutoMasters.slnx -c Debug` succeeded with 0 errors and 0 warnings.

---

## 5. Required Remediations (Actionable Next Steps)

1. **Fix `ErreurApi.cs`:**
   - Make `message` nullable (`private string? message;`) so it defaults to `null`.
   - Add alias setters for `error`, `detail`, `title`.
2. **Fix `ServiceAPI.cs` (`LireReponseAsync`):**
   - Replace `throw new Exception(erreur?.Message ?? fallback)` with:
     ```csharp
     string messageErreur = !string.IsNullOrWhiteSpace(erreur?.Message)
         ? erreur!.Message
         : fallback;
     throw new Exception(messageErreur);
     ```
3. **Add Direct Array Unit Tests in `ServiceApiTests.cs`:**
   - Add tests for `RecupererCatalogueAsync`, `RecupererOffresAsync`, `RecupererPortefeuilleAsync`, and `OuvrirBoosterAsync` with raw JSON array payloads (`[ { ... } ]`).
4. **Add Unexpected Error Schema Unit Tests in `ServiceApiTests.cs`:**
   - Add a test verifying that when the server returns `{ "error": "Détail de l'erreur" }` or `{}`, `ServiceAPI` extracts the error or falls back cleanly to the default message rather than throwing an empty string `""`.
5. **Add Corrupted JSON Unit Test in `ServiceApiTests.cs`:**
   - Add a test verifying behavior when an HTTP 200 OK response contains truncated/malformed JSON.
