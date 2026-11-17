# Forensic Analysis and Remediation Strategy: Findings 1.1 and 1.3

**Agent:** `explorer_m2_audit_2`  
**Roles:** `explorer`, `investigator`, `synthesizer`  
**Working Directory:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_audit_2`  
**Date:** 2026-10-07T18:36:00Z  
**Target File for Remediation:** `AutoMasters.Tests/ServiceApiTests.cs`  
**Status:** COMPLETE  

---

## 1. Executive Summary

Empirical execution of `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` confirmed that 3 out of 33 unit tests failed. This report provides the definitive forensic analysis, architectural rationale, and precise line-by-line code replacement instructions for:
- **Finding 1.1**: `RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl` (line 239) — URL encoding mismatch for query string assertion (`%C3%89PIQUE` vs `"ÉPIQUE"`).
- **Finding 1.3**: `CreerOffreAsync_PrixNegatif_400_LeveException` (line 801) — xUnit exact type mismatch (`typeof(ArgumentException)` vs expected `typeof(System.Exception)`).

Both issues are located entirely within `AutoMasters.Tests/ServiceApiTests.cs`. Applying the recommended code replacements will resolve both test failures cleanly while adhering strictly to existing codebase conventions.

---

## 2. Forensic Analysis & Fix Strategy

### 2.1 Finding 1.1: `RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl`

#### A. Direct Observation & Failure Signature
- **File:** `AutoMasters.Tests/ServiceApiTests.cs`
- **Line:** 239
- **Empirical Execution Error:**
  ```text
  [FAIL] AutoMasters.Tests.ServiceApiTests.RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl
  Message d'erreur :
    Assert.Contains() Failure: Sub-string not found
  String:    "?search=Porsche&rarity=%C3%89PIQUE"
  Not found: "ÉPIQUE"
     at AutoMasters.Tests.ServiceApiTests.RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 239
  ```

#### B. Root Cause Analysis
In `AutoMasters/Services/ServiceAPI.cs` (lines 300–303):
```csharp
if (!string.IsNullOrWhiteSpace(rarete))
    queryParams.Add($"rarity={Uri.EscapeDataString(rarete.Trim())}");

string url = queryParams.Count > 0 ? $"cards?{string.Join("&", queryParams)}" : "cards";
```
When `rarete` is passed as `"ÉPIQUE"`, `Uri.EscapeDataString("ÉPIQUE")` converts the accented UTF-8 character `'É'` (`0xC3 0x89`) to percent-encoded `%C3%89PIQUE`. The resultant request URI query string is:
```text
?search=Porsche&rarity=%C3%89PIQUE
```
In `AutoMasters.Tests/ServiceApiTests.cs` (lines 235–240):
```csharp
// Assertions
var requete = _mockHandler.DerniereRequete;
Assert.NotNull(requete);
string queryString = requete.RequestUri?.Query ?? string.Empty;
Assert.Contains("Porsche", queryString);
Assert.Contains("ÉPIQUE", queryString); // Line 239: FAILS!
```
The test asserts that the raw unescaped string `"ÉPIQUE"` is present in `queryString`. Because `queryString` contains the properly encoded representation `%C3%89PIQUE`, the assertion fails.

#### C. Codebase Reference Pattern
In the same test file, line 939–940 in method `RecupererCatalogueAsync_CaracteresSpeciauxRecherche_EchappeUrl`:
```csharp
// Lines 937-940 of ServiceApiTests.cs:
string queryString = requete.RequestUri?.Query ?? string.Empty;
Assert.Contains(Uri.EscapeDataString("Porsche & Ferrari"), queryString);
Assert.Contains(Uri.EscapeDataString("LÉGENDAIRE"), queryString);
```
Line 940 clearly establishes that query string assertions for non-ASCII or reserved characters MUST use `Uri.EscapeDataString(...)`.

#### D. Fix Strategy
Replace line 239 with `Assert.Contains(Uri.EscapeDataString("ÉPIQUE"), queryString);`.
This guarantees that:
1. The test verifies that `ServiceAPI` safely and correctly percent-encodes URL query parameters before transmitting them over HTTP.
2. The assertion perfectly matches the canonical pattern already used on line 940.

---

### 2.2 Finding 1.3: `CreerOffreAsync_PrixNegatif_400_LeveException`

#### A. Direct Observation & Failure Signature
- **File:** `AutoMasters.Tests/ServiceApiTests.cs`
- **Line:** 801
- **Empirical Execution Error:**
  ```text
  [FAIL] AutoMasters.Tests.ServiceApiTests.CreerOffreAsync_PrixNegatif_400_LeveException
  Message d'erreur :
    Assert.Throws() Failure: Exception type was not an exact match
  Expected: typeof(System.Exception)
  Actual:   typeof(System.ArgumentException)
  ---- System.ArgumentException : Le prix de vente doit être strictement supérieur à zéro. (Parameter 'prix')
     at AutoMasters.Tests.ServiceApiTests.CreerOffreAsync_PrixNegatif_400_LeveException() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 801
  ----- Inner Stack Trace -----
     at AutoMasters.Services.ServiceAPI.CreerOffreAsync(String copyId, Decimal prix) in C:\Users\stent\source\repos\AutoMasters\AutoMasters\Services\ServiceAPI.cs:line 541
  ```

#### B. Root Cause Analysis
In `AutoMasters/Services/ServiceAPI.cs` (lines 535–543):
```csharp
public async Task<Offre> CreerOffreAsync(string copyId, decimal prix)
{
    if (string.IsNullOrWhiteSpace(copyId))
        throw new ArgumentException("L'identifiant de l'exemplaire ne peut pas être vide.", nameof(copyId));

    if (prix <= 0)
        throw new ArgumentException("Le prix de vente doit être strictement supérieur à zéro.", nameof(prix));
```
When `_service.CreerOffreAsync("copy_toyota_123", -100m)` is called:
1. The client-side guard clause evaluates `prix <= 0` (`-100m <= 0`), which is `true`.
2. `ServiceAPI` synchronously throws `new ArgumentException("Le prix de vente doit être strictement supérieur à zéro.", nameof(prix))`.
3. In xUnit, `Assert.ThrowsAsync<T>` tests for **exact runtime type equality**:
   ```csharp
   if (exception.GetType() != typeof(T))
       throw new ThrowsException(typeof(T), exception);
   ```
4. Because `typeof(ArgumentException) != typeof(System.Exception)`, `Assert.ThrowsAsync<Exception>` fails, even though `ArgumentException` is a subclass of `Exception`.

#### C. Comparison of Remediation Options

| Dimension | Option A: `Assert.ThrowsAsync<ArgumentException>` (**Recommended**) | Option B: `Assert.ThrowsAnyAsync<Exception>` |
|---|---|---|
| **Mechanism** | Exact type check (`actual.GetType() == typeof(ArgumentException)`) | Subtype check (`typeof(Exception).IsAssignableFrom(actual.GetType())`) |
| **Contract Rigor** | **High**: Verifies that input validation throws standard .NET `ArgumentException` | **Medium**: Accepts any exception type inheriting from `Exception` |
| **Resilience** | Strict to client-side guard design | Tolerates both client-side `ArgumentException` and server-side `Exception` |
| **Message Assertion** | Works seamlessly: `ex.Message` contains the required message | Works seamlessly: `ex.Message` contains the required message |
| **xUnit Idiom** | Idiomatic for testing guard clauses on invalid arguments | Idiomatic for polymorphic exception handling |

#### D. Fix Strategy Recommendation
**Primary Recommendation: Option A (`Assert.ThrowsAsync<ArgumentException>`)**  
Why: `ServiceAPI.CreerOffreAsync` intentionally validates method arguments before dispatching HTTP calls. In standard .NET and BTS SIO curriculum guidelines, invalid arguments MUST produce `ArgumentException`. Testing for `ArgumentException` directly reflects this design contract and is the most rigorous test.

**Acceptable Alternative: Option B (`Assert.ThrowsAnyAsync<Exception>`)**  
If the team desires a polymorphic assertion that passes regardless of whether the exception is an `ArgumentException` or a generic `Exception`, `Assert.ThrowsAnyAsync<Exception>` is a valid alternative.

---

## 3. Line-by-Line Code Replacement Instructions

### 3.1 Replacement for Finding 1.1 (Line 239)

- **Target File:** `c:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs`
- **Target Line Range:** Lines 234–240

#### Original Code:
```csharp
        // Assertions
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        string queryString = requete.RequestUri?.Query ?? string.Empty;
        Assert.Contains("Porsche", queryString);
        Assert.Contains("ÉPIQUE", queryString);
```

#### Replacement Code:
```csharp
        // Assertions
        var requete = _mockHandler.DerniereRequete;
        Assert.NotNull(requete);
        string queryString = requete.RequestUri?.Query ?? string.Empty;
        Assert.Contains("Porsche", queryString);
        Assert.Contains(Uri.EscapeDataString("ÉPIQUE"), queryString);
```

---

### 3.2 Replacement for Finding 1.3 (Line 801)

- **Target File:** `c:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs`
- **Target Line Range:** Lines 800–805

#### Option A (Recommended — Exact Type `ArgumentException`):

##### Original Code:
```csharp
        // Action & Assertion
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.CreerOffreAsync("copy_toyota_123", -100m));

        Assert.Contains("strictement supérieur à zéro", ex.Message);
```

##### Replacement Code:
```csharp
        // Action & Assertion
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreerOffreAsync("copy_toyota_123", -100m));

        Assert.Contains("strictement supérieur à zéro", ex.Message);
```

#### Option B (Alternative — Polymorphic `ThrowsAnyAsync<Exception>`):

##### Replacement Code:
```csharp
        // Action & Assertion
        var ex = await Assert.ThrowsAnyAsync<Exception>(() =>
            _service.CreerOffreAsync("copy_toyota_123", -100m));

        Assert.Contains("strictement supérieur à zéro", ex.Message);
```

---

## 4. Contextual Note: Finding 1.2 (`ObjectDisposedException`)

For total situational awareness during Milestone 2 remediation:
- **Test:** `SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer` (line 63 in `ServiceApiTests.cs`)
- **Cause:** In `AutoMasters/Services/ServiceAPI.cs` line 245:
  `using var contenu = new StringContent(json, Encoding.UTF8, "application/json");`
  `contenu` is disposed when `SeConnecterAsync` exits, leaving `requete.Content` disposed when line 63 calls `await requete.Content!.ReadAsStringAsync()`.
- **Location of Fix:** This must be addressed in `AutoMasters/Services/ServiceAPI.cs` line 245 (by removing `using` on `var contenu`) or in `MockHttpMessageHandler.cs` (by pre-reading/buffering content). It is outside the scope of `ServiceApiTests.cs` modifications.

---

## 5. Verification Protocol

After applying the changes to `AutoMasters.Tests/ServiceApiTests.cs`, verify by running:

```powershell
dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl|FullyQualifiedName~CreerOffreAsync_PrixNegatif_400_LeveException"
```

Expected output:
```text
Passé! - échec : 0, réussite : 2, ignorée(s) : 0, total : 2
```

Once Finding 1.2 is also remediated, execute the entire suite:
```powershell
dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj
```
Expected output:
```text
Passé! - échec : 0, réussite : 33, ignorée(s) : 0, total : 33
```
