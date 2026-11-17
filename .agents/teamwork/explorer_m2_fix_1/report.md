# Fix Strategy Report: HTTP 401 Session Teardown in `ServiceAPI.cs`

**Agent:** `explorer_m2_fix_1`  
**Role:** Explorer / Read-Only Investigator  
**Target:** `AutoMasters/Services/ServiceAPI.cs` and `AutoMasters.Tests/ServiceApiTests.cs`  
**Date:** 2026-10-07T18:13:00Z  
**Context:** Resolution of Defect Identified by `challenger_m2_1` in Milestone 2  

---

## 1. Executive Summary

During adversarial review of Milestone 2, agent `challenger_m2_1` identified a critical session lifecycle defect in `AutoMasters/Services/ServiceAPI.cs`:
When an authenticated request encounters an HTTP `401 Unauthorized` response (e.g., expired JWT after 2 hours, revoked credentials, or server-side session termination), `ServiceAPI.LireReponseAsync` throws an exception but **does not clear the session state**. Consequently:
- `jetonSession` remains set in memory.
- `client.DefaultRequestHeaders.Authorization` retains the stale `Bearer <token>` header.
- `SecureStorage` continues to persist the invalid token under key `"automasters_jwt"`.
- `EstAuthentifie` continues to return `true`.

This corrupts downstream MVVM ViewModels in Milestone 3 (such as `ConnexionVueModele`, `CatalogueVueModele`, `GarageVueModele`, and navigation guards in `AppShell`), causing the app to erroneously assume the user is logged in, preventing automatic redirection to `ConnexionPage`, and perpetuating invalid HTTP calls.

This report specifies the root cause, exact code modifications required for `ServiceAPI.cs`, the headless test runner resilience pattern for `SecureStorage`, and new unit test cases for `AutoMasters.Tests`.

---

## 2. Forensic Defect Analysis

### 2.1 The Defect in `ServiceAPI.cs`

In `AutoMasters/Services/ServiceAPI.cs` (lines 91–135), the centralized response handler `LireReponseAsync` processes all HTTP responses across every API endpoint:

```csharp
// AutoMasters/Services/ServiceAPI.cs (lines 111-114)
if (reponse.StatusCode == HttpStatusCode.Unauthorized)
{
    throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
}
```

Upon receiving HTTP 401, the method throws immediately. It fails to trigger any session cleanup.

### 2.2 Mechanism of State Corruption

1. **`EstAuthentifie` evaluation property (lines 76–79):**
   ```csharp
   public bool EstAuthentifie => !string.IsNullOrWhiteSpace(jetonSession) || client.DefaultRequestHeaders.Authorization != null;
   ```
   Because neither `jetonSession` nor `client.DefaultRequestHeaders.Authorization` is set to `null`, `EstAuthentifie` remains `true` even after an unauthorized exception is caught by a ViewModel or caller.

2. **Persistence and Token Restoration (lines 141–162):**
   ```csharp
   public async Task<string?> RestaurerAuthentificationAsync()
   {
       if (client.DefaultRequestHeaders.Authorization != null && !string.IsNullOrWhiteSpace(jetonSession))
           return jetonSession;

       try
       {
           string? token = await SecureStorage.Default.GetAsync(CleStockageJeton);
           if (!string.IsNullOrWhiteSpace(token))
           {
               jetonSession = token;
               client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
               return token;
           }
       }
       catch { /* Fallback */ }
       return null;
   }
   ```
   If the token is not evicted from `SecureStorage`, any subsequent call or app restart restores the dead token into the HTTP headers, trapping the client in repeated 401 failures.

3. **Existing Precedent in `LogoutAsync` (lines 193–208):**
   ```csharp
   public async Task LogoutAsync()
   {
       client.DefaultRequestHeaders.Authorization = null;
       jetonSession = null;

       try
       {
           SecureStorage.Default.Remove(CleStockageJeton);
       }
       catch
       {
           // Fallback silencieux
       }

       await Task.CompletedTask;
   }
   ```
   The codebase already recognizes the exact three operations required for session teardown, but previously only executed them upon explicit user logout, neglecting automatic teardown on 401 responses.

---

## 3. Prescribed Code Modifications

### 3.1 Strict Architecture & File Count Constraints
- **Single File Constraint:** Strictly **ONE** file must remain in `AutoMasters/Services/`, named `ServiceAPI.cs`. No helper classes, utility files, or secondary interfaces in separate files.
- **Convention:** Respect BTS SIO SLAM 2nd year structure and C# region convention (`#region Attributs`, `#region Getter/Setter`, `#region Methodes`).

---

### 3.2 Strategy Comparison: Refactoring vs Inline

Two implementation approaches satisfy the requirements:

#### Option A: DRY Helper Method `ReinitialiserSession()` (Recommended)
Extract the session clearing logic into a private helper method `ReinitialiserSession()` used by both `LireReponseAsync` and `LogoutAsync()`.

**Advantages:**
- Eliminates code duplication between `LogoutAsync` and `LireReponseAsync`.
- Guarantees any future additions to session cleanup (e.g., clearing cached profile data or notifications) are applied uniformly to both explicit logouts and 401 unauthorized responses.
- Maintains clean BTS SIO coding standards.

#### Option B: Inline Teardown in `LireReponseAsync`
Place the 4 lines directly inside the `if (reponse.StatusCode == HttpStatusCode.Unauthorized)` block in `LireReponseAsync`.

**Advantages:**
- Minimal localized diff.

**Recommendation:** Option A is functionally superior and adheres to best practices, but both options produce identical runtime behavior. The exact diff for Option A is detailed below, with Option B provided as an alternative.

---

### 3.3 Exact Code Modifications (Option A — Recommended)

#### File: `AutoMasters/Services/ServiceAPI.cs`

**Modification 1: Update `LireReponseAsync` (around line 111)**

*Before:*
```csharp
            if (reponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
            }
```

*After:*
```csharp
            if (reponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                ReinitialiserSession();
                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
            }
```

**Modification 2: Add `ReinitialiserSession()` and refactor `LogoutAsync` (around lines 190–208)**

*Before:*
```csharp
    /// <summary>
    /// Déconnecte l'utilisateur en réinitialisant l'en-tête Authorization et en vidant SecureStorage.
    /// </summary>
    public async Task LogoutAsync()
    {
        client.DefaultRequestHeaders.Authorization = null;
        jetonSession = null;

        try
        {
            SecureStorage.Default.Remove(CleStockageJeton);
        }
        catch
        {
            // Fallback silencieux
        }

        await Task.CompletedTask;
    }
```

*After:*
```csharp
    /// <summary>
    /// Réinitialise l'état de session local (mémoire, en-tête Authorization et stockage sécurisé).
    /// Sécurisé contre les exceptions hors contexte MAUI / exécution de tests unitaires headless.
    /// </summary>
    private void ReinitialiserSession()
    {
        client.DefaultRequestHeaders.Authorization = null;
        jetonSession = null;

        try
        {
            SecureStorage.Default.Remove(CleStockageJeton);
        }
        catch
        {
            // Fallback silencieux si SecureStorage est indisponible (ex: tests unitaires xUnit sans runtime MAUI)
        }
    }

    /// <summary>
    /// Déconnecte l'utilisateur en réinitialisant l'en-tête Authorization et en vidant SecureStorage.
    /// </summary>
    public async Task LogoutAsync()
    {
        ReinitialiserSession();
        await Task.CompletedTask;
    }
```

---

### 3.4 Alternative Code Modifications (Option B — Inline)

If the implementer prefers a strictly localized edit without introducing `ReinitialiserSession()`:

#### File: `AutoMasters/Services/ServiceAPI.cs`

*Replace lines 111–114:*
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
                    // Fallback silencieux hors environnement MAUI (ex: tests unitaires headless)
                }

                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
            }
```

---

## 4. Headless Test Runner & SecureStorage Resilience

### 4.1 The Challenge
`Microsoft.Maui.Storage.SecureStorage.Default` delegates to platform APIs:
- Under Windows unpackaged execution or xUnit test runners (`dotnet test`), Windows App SDK security primitives or WinRT APIs can throw `PlatformNotSupportedException`, `TypeInitializationException`, or `COMException` when the full MAUI application lifecycle (`Microsoft.Maui.Controls.Application.Current`) is not running.
- In Linux/CI environments, platform keychains are entirely absent.

### 4.2 The Solution
The eviction call `SecureStorage.Default.Remove(CleStockageJeton)` **must** be wrapped in a `try / catch (Exception)` block.
- In production MAUI runtime: `CleStockageJeton` is successfully evicted from the platform keychain.
- In unit test runner / headless CI: The exception is absorbed silently; `client.DefaultRequestHeaders.Authorization` and `jetonSession` are guaranteed to be cleared in memory.
- Under both environments, `EstAuthentifie` evaluates to `false` and no unexpected platform exception masks the intended `Exception("Connexion refusée ou expirée. Reconnectez-vous.")`.

---

## 5. Required Unit Test Suite Additions

To establish regression defense against session leakage on 401 Unauthorized, the worker must add the following unit tests to `AutoMasters.Tests/ServiceApiTests.cs`.

### 5.1 Test 1: Session Teardown on GET (Catalog) 401 Unauthorized

```csharp
[Fact]
public async Task RequeteProtegee_401Unauthorized_ReinitialiseJetonEtEnTeteAuthorization()
{
    // Disposition : session active existante
    string jetonInitial = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.token_actif_123";
    _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
    {
        accessToken = jetonInitial,
        refreshToken = "refresh_123",
        expiresAt = DateTime.UtcNow.AddHours(2)
    });

    await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");
    Assert.True(_service.EstAuthentifie);
    Assert.Equal(jetonInitial, _service.Jeton);
    Assert.NotNull(_service.ClientHttp.DefaultRequestHeaders.Authorization);

    // Simulation d'une expiration de session (401 Unauthorized sur GET /cards)
    _mockHandler.QuandGet("/cards", HttpStatusCode.Unauthorized, new
    {
        code = "UNAUTHORIZED",
        message = "Jeton de session expiré ou invalide."
    });

    // Action & Assertion d'erreur
    var exception = await Assert.ThrowsAsync<Exception>(() =>
        _service.RecupererCatalogueAsync());

    Assert.Contains("Connexion refusée ou expirée", exception.Message);

    // Assertions de réinitialisation de session
    Assert.False(_service.EstAuthentifie, "EstAuthentifie doit être faux après un 401 Unauthorized.");
    Assert.Null(_service.Jeton);
    Assert.Null(_service.ClientHttp.DefaultRequestHeaders.Authorization);
}
```

### 5.2 Test 2: Session Teardown on POST (Buy Order) 401 Unauthorized

```csharp
[Fact]
public async Task AcheterOffreAsync_401Unauthorized_ReinitialiseSessionEtLeveException()
{
    // Disposition : session authentifiée
    _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
    {
        accessToken = "jeton_ordre_achat",
        expiresAt = DateTime.UtcNow.AddHours(1)
    });
    await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");
    Assert.True(_service.EstAuthentifie);

    // Simulation 401 sur achat immédiat
    _mockHandler.QuandPost("/orders/buy", HttpStatusCode.Unauthorized, new
    {
        code = "UNAUTHORIZED",
        message = "Session expirée."
    });

    // Action & Assertion
    var ex = await Assert.ThrowsAsync<Exception>(() =>
        _service.AcheterOffreAsync("offre_test_id"));

    Assert.Contains("Connexion refusée ou expirée", ex.Message);
    Assert.False(_service.EstAuthentifie);
    Assert.Null(_service.Jeton);
    Assert.Null(_service.ClientHttp.DefaultRequestHeaders.Authorization);
}
```

### 5.3 Test 3: Explicit Logout Teardown

```csharp
[Fact]
public async Task LogoutAsync_ReinitialiseJetonEtEnTeteAuthorization()
{
    // Disposition : session authentifiée
    _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
    {
        accessToken = "jeton_deconnexion",
        expiresAt = DateTime.UtcNow.AddHours(1)
    });
    await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");
    Assert.True(_service.EstAuthentifie);

    // Action
    await _service.LogoutAsync();

    // Assertions
    Assert.False(_service.EstAuthentifie);
    Assert.Null(_service.Jeton);
    Assert.Null(_service.ClientHttp.DefaultRequestHeaders.Authorization);
}
```

---

## 6. Implementation Checklist for Worker

1. [ ] **Edit `AutoMasters/Services/ServiceAPI.cs`**:
   - Apply Option A (or Option B) to clear `client.DefaultRequestHeaders.Authorization = null;`, `jetonSession = null;`, and evict `CleStockageJeton` via `try/catch` in `LireReponseAsync` upon `HttpStatusCode.Unauthorized`.
   - Maintain French region markers and XML documentation comments.
   - Confirm directory `AutoMasters/Services/` contains **strictly ONE file**: `ServiceAPI.cs`.
2. [ ] **Edit `AutoMasters.Tests/ServiceApiTests.cs`**:
   - Append the three unit tests from Section 5 under `#region Tests Robustesse, DI & Cas Limites`.
3. [ ] **Build & Test Execution**:
   - Execute `dotnet build AutoMasters.slnx -c Debug` to confirm 0 compilation errors and 0 warnings.
   - Execute `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj -c Debug` to confirm all 30 tests pass.
4. [ ] **Trigger Challenger Re-evaluation**:
   - Provide clean handoff for `challenger_m2_1` verification.
