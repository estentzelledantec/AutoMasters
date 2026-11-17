# Investigation & Remediation Strategy Report — Finding 1.2

**Target Finding:** Auditor Finding 1.2 — `SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer` fails with `System.ObjectDisposedException`  
**Investigating Agent:** `explorer_m2_audit_1`  
**Date:** 2026-10-07T18:37:00Z  
**Target File:** `c:\Users\stent\source\repos\AutoMasters\AutoMasters\Services\ServiceAPI.cs`  

---

## 1. Executive Summary

During the Milestone 2 remediation audit, independent test execution revealed a fatal crash in the unit test suite:
- **Test:** `AutoMasters.Tests.ServiceApiTests.SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer`
- **Exception:** `System.ObjectDisposedException : Cannot access a disposed object. Object name: 'System.Net.Http.StringContent'.` at `ServiceApiTests.cs:line 63`.

The root cause is the premature disposal of `StringContent` via `using var contenu` at line 245 of `AutoMasters/Services/ServiceAPI.cs`. When `SeConnecterAsync` exits, `contenu.Dispose()` executes, leaving the `HttpRequestMessage.Content` captured by `MockHttpMessageHandler` in a disposed state before the test assertion `await requete.Content!.ReadAsStringAsync()` can inspect it.

This report establishes the exact fix strategy:
1. **Remove `using` on `contenu`** in `ServiceAPI.cs` line 245.
2. Standardize all other `StringContent` instantiations in `ServiceAPI.cs` (lines 491, 519, 552, 589) to eliminate identical latent disposal risks.
3. Validate that no resource leak occurs, the single-file constraint in `Services/` is preserved, and test execution passes.

---

## 2. Root Cause Analysis

### 2.1 Code Trace & Failure Sequence

In `AutoMasters/Services/ServiceAPI.cs` (lines 244–248):
```csharp
244: string json = JsonConvert.SerializeObject(demande);
245: using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
246: using var reponse = await client.PostAsync("auth/login", contenu);
247: 
248: string resultat = await LireReponseAsync(reponse);
...
263: return connexion;
264: }
```

In `AutoMasters.Tests/ServiceApiTests.cs` (lines 52–66):
```csharp
52:  await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");
...
60:  var requete = _mockHandler.DerniereRequete;
61:  Assert.NotNull(requete);
62:  Assert.Equal(HttpMethod.Post, requete.Method);
63:  string corpsEnvoye = await requete.Content!.ReadAsStringAsync(); // <-- ObjectDisposedException
64:  Assert.Contains("etudiant@bts.sio", corpsEnvoye);
65:  Assert.Contains("MotDePasse123!", corpsEnvoye);
```

### 2.2 Execution Mechanics
1. **C# 8+ Scope Disposal:** The variable declaration `using var contenu = ...` creates a local variable whose `.Dispose()` method is automatically called when exiting `SeConnecterAsync` (line 264).
2. **Object Sharing with Mock Handler:** `HttpClient.PostAsync("auth/login", contenu)` attaches `contenu` directly to the internal `HttpRequestMessage.Content`.
3. **Capture by Test Spy:** `MockHttpMessageHandler.SendAsync` appends the `HttpRequestMessage` into `_requetesEnregistrees`.
4. **Scope Exit Disposes Content:** `SeConnecterAsync` finishes execution and returns the `ReponseConnexion` object. At this exact moment, `contenu.Dispose()` runs, setting internal `_disposed = true` and invalidating underlying buffers.
5. **Assertion Failure:** Line 63 in `ServiceApiTests.cs` invokes `await requete.Content!.ReadAsStringAsync()`. Because the underlying `StringContent` is disposed, .NET throws `System.ObjectDisposedException`.

---

## 3. Analysis of Fix Strategies

### Strategy 1: Remove `using` on `contenu` in `ServiceAPI.cs` (RECOMMENDED)

Change line 245 from:
```csharp
using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
```
to:
```csharp
var contenu = new StringContent(json, Encoding.UTF8, "application/json");
```

#### Why this is the correct .NET pattern:
1. **Managed Memory Lifecycle:** `StringContent` wraps an in-memory byte array (`ByteArrayContent`). It does NOT hold unmanaged OS resources (such as native file descriptors, sockets, or window handles).
2. **Garbage Collection Safety:** In managed .NET, in-memory `HttpContent` is cleaned up automatically by the Garbage Collector once references to the `HttpRequestMessage` are dropped. There is zero risk of memory leak or socket exhaustion.
3. **HTTP Ownership Model:** In idiomatic .NET HTTP client programming, when content is passed to `HttpClient` or assigned to an `HttpRequestMessage`, the caller delegates lifecycle responsibility. Prematurely disposing the content before downstream middleware, telemetry handlers, or test fixtures have inspected the request breaks standard request observation.

### Strategy 2: Defensive Audit of Other Methods in `ServiceAPI.cs`

An inspection of all HTTP mutation methods in `AutoMasters/Services/ServiceAPI.cs` identified four other places where `using` is used on request content or request messages:

| Line # | Method | Code | Risk Assessment | Recommendation |
|---|---|---|---|---|
| **245** | `SeConnecterAsync` | `using var contenu = new StringContent(...);` | **CRITICAL (FAILS TEST)** | Change to `var contenu` (Mandatory) |
| **491** | `AcheterOffreAsync` | `using var requete = new HttpRequestMessage(...)` | **LATENT** | Change to `var requete` to avoid disposing content |
| **519** | `ReserverOffreAsync` | `using var contenu = new StringContent(...);` | **LATENT** | Change to `var contenu` |
| **552** | `CreerOffreAsync` | `using var contenu = new StringContent(...);` | **LATENT** | Change to `var contenu` |
| **589** | `OuvrirBoosterAsync` | `using var contenu = new StringContent(...);` | **LATENT** | Change to `var contenu` |

*Note on Response Disposal:* In contrast, `using var reponse = await client.PostAsync(...)` in lines 246, 520, 553, 590 is completely fine and safe, because `LireReponseAsync(reponse)` immediately reads the entire response content before the method exits.

---

## 4. Single-File & Architecture Constraints Verification

1. **Single File Rule (`Services/`):**
   - The fix modifies only `AutoMasters/Services/ServiceAPI.cs`.
   - No additional classes, helper files, or interfaces are introduced in `AutoMasters/Services/`.
   - Layout compliance is strictly maintained.

2. **No Regression:**
   - Removing `using` does not alter HTTP headers, payload formatting, serialization, error handling, or token persistence.
   - All other 30 passing unit tests remain unaffected.

---

## 5. Exact Code Modifications for Worker

### Modification 1: `AutoMasters/Services/ServiceAPI.cs` (Line 245) — Primary Fix

**File:** `c:\Users\stent\source\repos\AutoMasters\AutoMasters\Services\ServiceAPI.cs`  
**Method:** `SeConnecterAsync` (Lines 244–247)

```csharp
<<<< BEFORE (Line 244-247)
        string json = JsonConvert.SerializeObject(demande);
        using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync("auth/login", contenu);
==== AFTER
        string json = JsonConvert.SerializeObject(demande);
        var contenu = new StringContent(json, Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync("auth/login", contenu);
>>>>
```

### Modification 2: `AutoMasters/Services/ServiceAPI.cs` (Lines 491, 519, 552, 589) — Hardening

To ensure all POST endpoints in `ServiceAPI.cs` behave consistently and do not fail if future tests or logging interceptors inspect `DerniereRequete.Content`:

```csharp
<<<< BEFORE (Line 491-494)
        using var requete = new HttpRequestMessage(HttpMethod.Post, "orders/buy")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
==== AFTER
        var requete = new HttpRequestMessage(HttpMethod.Post, "orders/buy")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
>>>>

<<<< BEFORE (Line 519)
        using var contenu = new StringContent("{}", Encoding.UTF8, "application/json");
==== AFTER
        var contenu = new StringContent("{}", Encoding.UTF8, "application/json");
>>>>

<<<< BEFORE (Line 552)
        using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
==== AFTER
        var contenu = new StringContent(json, Encoding.UTF8, "application/json");
>>>>

<<<< BEFORE (Line 589)
        using var contenu = new StringContent("{}", Encoding.UTF8, "application/json");
==== AFTER
        var contenu = new StringContent("{}", Encoding.UTF8, "application/json");
>>>>
```

---

## 6. Independent Verification Method

Execute the target unit test in isolation:
```powershell
dotnet test c:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\AutoMasters.Tests.csproj --filter "FullyQualifiedName~SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer"
```
**Expected Result:**
```text
Réussite!  - échec : 0, réussite : 1, ignorée(s) : 0, total : 1
```

Confirm build cleanliness:
```powershell
dotnet build c:\Users\stent\source\repos\AutoMasters\AutoMasters.slnx -c Debug
```
**Expected Result:**
`0 avertissement(s), 0 erreur(s)`
