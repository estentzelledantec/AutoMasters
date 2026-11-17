# Adversarial Challenge Report — Milestone 2: ServiceAPI Architecture & Error Handling

**Agent:** `challenger_m2_1`  
**Role:** Critic / Empirical Challenger  
**Target:** Milestone 2 (`ServiceAPI.cs`, Domain Models, Unit Test Suite `AutoMasters.Tests`)  
**Date:** 2026-10-07T18:08:00Z  
**Verdict:** **REQUEST_CHANGES**

---

## 1. Challenge Summary

**Overall Risk Assessment:** **HIGH**  
**Verdict:** **REQUEST_CHANGES**

While `ServiceAPI.cs` successfully satisfies the single-file constraint (Feature F06 / Requirement R1), handles non-JSON 502/503 HTML error responses without deserialization crashes, and correctly generates unique RFC 4122 UUIDv4 `Idempotency-Key` headers for purchases, an adversarial failure mode was confirmed:
- **Critical Token Retention on 401 Unauthorized:** Receiving an HTTP 401 Unauthorized response on an authenticated endpoint throws an exception but **fails to evict or clear** the expired session token from memory (`jetonSession`), HTTP request headers (`client.DefaultRequestHeaders.Authorization`), or persistent storage (`SecureStorage`). As a result, `EstAuthentifie` continues to return `true`, corrupting application state and preventing clean re-authentication.
- **Missing Test Coverage:** No unit test exists in `AutoMasters.Tests` asserting that a 401 Unauthorized response tears down the active session.

---

## 2. Empirical Verification Findings

### Finding 1: Single File Constraint in `AutoMasters/Services/` (PASSED)
- **Tool Inspection:** Directory `AutoMasters/Services/` was inspected via filesystem listing.
- **Observation:** Exactly ONE file exists: `ServiceAPI.cs` (20,907 bytes). Zero subdirectories or secondary files.
- **Verification:** Requirement R1 ("The services folder contains only one file named ServiceAPI") and Feature F06 are fully satisfied.

### Finding 2: Non-JSON 502/503 HTML Error Handling (PASSED)
- **Mechanism:** In `ServiceAPI.cs` lines 98–109:
  ```csharp
  try
  {
      if (!string.IsNullOrWhiteSpace(json))
      {
          erreur = JsonConvert.DeserializeObject<ErreurApi>(json);
      }
  }
  catch (JsonException)
  {
      // La réponse peut être du HTML (ex: 502/503 reverse proxy) ou non-JSON
      erreur = null;
  }
  ```
- **Observation:** When an upstream reverse proxy returns an HTML page (e.g. `<html>...502 Bad Gateway...</html>`), the exception is caught, `erreur` is null, and the method falls through to throw `new Exception($"Une erreur est survenue lors de la communication avec le serveur ({reponse.StatusCode}).")`.
- **Unit Test:** Verified by `ServiceApiTests.ServiceAPI_ReponseHtml502_GereErreurServeurSansCrashJson` (line 631).

### Finding 3: `Idempotency-Key` UUIDv4 Generation & Attachment (PASSED)
- **Mechanism:** In `AcheterOffreAsync` (lines 424–436):
  ```csharp
  string cleIdempotence = Guid.NewGuid().ToString();
  var demande = new { offerId = offreId.Trim(), idempotencyKey = cleIdempotence };
  requete.Headers.Add("Idempotency-Key", cleIdempotence);
  ```
- **Observation:** A fresh RFC 4122 v4 UUID is generated for every buy call, attached to both the HTTP header and the JSON request body.
- **Unit Tests:** Verified by `AcheterOffreAsync_Succes_TransmetEnTeteIdempotencyKey` and `AcheterOffreAsync_FormatCleIdempotence_EstUuidV4Valide`.

### Finding 4: Bearer Token Authorization Header Attachment (PASSED)
- **Mechanism:** In `SauvegarderJetonAsync` (line 170):
  `client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);`
  And on each protected method call:
  `await RestaurerAuthentificationAsync();`
- **Unit Test:** Verified by `SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer`.

---

## 3. Specific Challenges & Bugs Found

### [High] Challenge 1: 401 Unauthorized Fails to Clear Token / Session State

- **Challenged Assumption:** The system assumes that handling 401 Unauthorized merely requires throwing a user-facing exception (`"Connexion refusée ou expirée. Reconnectez-vous."`).
- **Attack Scenario:**
  1. A user logs in successfully via `SeConnecterAsync(...)`. `jetonSession` is assigned, `DefaultRequestHeaders.Authorization` is set to `Bearer ey...`, and the token is saved in `SecureStorage`.
  2. The JWT token expires on the server after 2 hours (or is revoked/invalidated).
  3. The user or background polling executes any protected endpoint (e.g., `RecupererPortfolioAsync()` or `RecupererCatalogueAsync()`).
  4. The API returns `401 Unauthorized`.
  5. `LireReponseAsync` executes lines 111–114:
     ```csharp
     if (reponse.StatusCode == HttpStatusCode.Unauthorized)
     {
         throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
     }
     ```
  6. **Bug Behavior:** The exception is thrown, BUT `jetonSession` is NOT set to `null`, `client.DefaultRequestHeaders.Authorization` is NOT cleared, and `SecureStorage` still retains the stale token!
  7. **Blast Radius:**
     - `_service.EstAuthentifie` still evaluates to `true`.
     - In Milestone 3, ViewModels or navigation guards querying `ServiceAPI.Instance.EstAuthentifie` will believe the session is active.
     - Subsequent HTTP requests will continue attaching the expired Bearer header.
     - On mobile app resume/restart, `RestaurerAuthentificationAsync()` will read the dead token and skip prompting for login.
- **Mitigation / Required Fix in `ServiceAPI.cs`:**
  Modify `LireReponseAsync` in `AutoMasters/Services/ServiceAPI.cs`:
  ```csharp
  if (reponse.StatusCode == HttpStatusCode.Unauthorized)
  {
      client.DefaultRequestHeaders.Authorization = null;
      jetonSession = null;
      try
      {
          SecureStorage.Default.Remove(CleStockageJeton);
      }
      catch
      {
          // Fallback silencieux hors environnement MAUI
      }

      throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
  }
  ```
- **Required Unit Test in `AutoMasters.Tests/ServiceApiTests.cs`:**
  ```csharp
  [Fact]
  public async Task RequeteProtegee_401Unauthorized_ReinitialiseJetonEtEnTeteAuthorization()
  {
      // Disposition : session authentifiée existante
      _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
      {
          accessToken = "jeton_expirable_123"
      });
      await _service.SeConnecterAsync("user@bts.sio", "pass");
      Assert.True(_service.EstAuthentifie);

      // Simulation 401 sur catalogue
      _mockHandler.QuandGet("/cards", HttpStatusCode.Unauthorized, new
      {
          code = "UNAUTHORIZED",
          message = "Jeton expiré."
      });

      // Action & Assertion
      await Assert.ThrowsAsync<Exception>(() => _service.RecupererCatalogueAsync());

      Assert.False(_service.EstAuthentifie);
      Assert.Null(_service.Jeton);
      Assert.Null(_service.ClientHttp.DefaultRequestHeaders.Authorization);
  }
  ```

---

### [Low] Challenge 2: HTTP 200 OK with Non-JSON HTML Payload (Captive Portals)

- **Challenged Assumption:** `LireReponseAsync` assumes that all HTTP 200 responses contain valid JSON.
- **Attack Scenario:** On public Wi-Fi or hotel captive portals, unauthenticated HTTP requests return HTTP 200 OK with an HTML splash screen.
- **Blast Radius:** Callers calling `JToken.Parse(json)` (in `RecupererCatalogueAsync`, `RecupererPortefeuilleAsync`, `RecupererOffresAsync`, `OuvrirBoosterAsync`) will throw raw `JsonReaderException`.
- **Mitigation:** Wrap `JToken.Parse` in `try ... catch (JsonException)` returning empty collections or an explanatory exception.

---

### [Low] Challenge 3: Mutation of `HttpClient.DefaultRequestHeaders` Across Concurrent Calls

- **Challenged Assumption:** Mutating `client.DefaultRequestHeaders.Authorization` is safe during runtime.
- **Attack Scenario:** In .NET `HttpClient`, modifying `DefaultRequestHeaders` while concurrent requests are in flight can cause concurrency exceptions (`InvalidOperationException`).
- **Blast Radius:** Low for student-level single-dispatcher MAUI apps, but relevant if background tasks fetch data during login/logout transitions.
- **Mitigation:** Recommended for future hardening: attach authorization per `HttpRequestMessage` or guard token modifications.

---

## 4. Stress Test Results Matrix

| Scenario | Expected Behavior | Actual Behavior | Result |
|---|---|---|---|
| Single file in `AutoMasters/Services/` | Exactly 1 file (`ServiceAPI.cs`) | Exactly 1 file found | **PASS** |
| 502 HTML Bad Gateway on GET `/cards` | Throws `Exception` with 502 message; no `JsonException` leak | Handled in `try/catch(JsonException)` | **PASS** |
| 502 HTML Bad Gateway on POST `/orders/buy` | Throws `Exception` with 502 message; no `JsonException` leak | Handled in `try/catch(JsonException)` | **PASS** |
| `AcheterOffreAsync` Idempotency-Key uniqueness | Unique RFC 4122 v4 UUID generated per buy request | Generates unique `Guid.NewGuid()` | **PASS** |
| `AcheterOffreAsync` Header attachment | `Idempotency-Key` header present in HTTP request | Header attached and verified | **PASS** |
| Bearer token attachment on protected requests | `Authorization: Bearer <token>` attached to outbound HTTP request | Set on `DefaultRequestHeaders` | **PASS** |
| **401 Unauthorized token eviction** | **Token cleared from memory, headers, and storage; `EstAuthentifie` becomes false** | **Token retained in memory, headers, and storage; `EstAuthentifie` remains true** | **FAIL** |

---

## 5. Unchallenged Areas

- **MAUI Visual Layout & XAML UI (M3 Scope):** Views and ViewModels are scheduled for Milestone 3 and were not tested here.
- **Live Remote API Backend:** Live testing against `https://api-cartes-g2.fldsio.com/` was not executed to preserve offline determinism.

---

## 6. Actionable Resolution Checklist for Worker

1. In `AutoMasters/Services/ServiceAPI.cs` (`LireReponseAsync`):
   When `reponse.StatusCode == HttpStatusCode.Unauthorized`, reset `client.DefaultRequestHeaders.Authorization = null;`, set `jetonSession = null;`, and call `SecureStorage.Default.Remove(CleStockageJeton);` inside a try/catch before throwing.
2. In `AutoMasters.Tests/ServiceApiTests.cs`:
   Add unit test `RequeteProtegee_401Unauthorized_ReinitialiseJetonEtEnTeteAuthorization` verifying that an active session is dismantled when a 401 is received.
3. Re-run `dotnet build AutoMasters.slnx -c Debug` to confirm clean compilation with 0 warnings.
