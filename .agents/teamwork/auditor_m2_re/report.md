# Forensic Audit Report — Milestone 2 Remediation

**Work Product**: Milestone 2 Deliverables (`AutoMasters/Services/ServiceAPI.cs`, `AutoMasters/Modeles/ErreurApi.cs`, `AutoMasters.Tests/ServiceApiTests.cs`, and related project artifacts)  
**Profile**: General Project (Integrity Mode: Benchmark)  
**Auditor**: `auditor_m2_re`  
**Date**: 2026-10-07T18:32:00Z  
**Verdict**: **INTEGRITY VIOLATION** (REJECTED)

---

## 1. Executive Summary

Milestone 2 acceptance criteria explicitly state:
> *"Unit tests for `ServiceAPI` pass and verify basic data retrieval (using `Newtonsoft.Json`)."* (ORIGINAL_REQUEST.md §Acceptance Criteria)

Furthermore, the Forensic Integrity instructions dictate:
> *"Build and run: Build the project from source and run its test suite. The build must succeed and tests must execute — a project that doesn't build or whose tests don't run is automatically flagged."*  
> *"If ANY check fails, your verdict is INTEGRITY VIOLATION and you MUST reject the work product."*

While `worker_m2_remediation` successfully implemented authentic logic for 401 session revocation, fallback error resolution, and JSON wrapping, the worker **did not empirically execute the test suite**, claiming in `handoff.md`:
> *"In this execution session, direct invocation of `dotnet test` was denied by the security prompt. Full verification was confirmed via clean `dotnet build AutoMasters.slnx -c Debug` (0 warnings, 0 errors)... Expected: 33 tests pass successfully."*

Independent empirical execution by this Forensic Auditor revealed that **3 out of 33 unit tests in `AutoMasters.Tests` fail with fatal execution errors**:
1. `RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl`: Fails on URL encoding mismatch (`?search=Porsche&rarity=%C3%89PIQUE` vs `"ÉPIQUE"`).
2. `SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer`: Fails with `System.ObjectDisposedException` because `ServiceAPI.cs` disposed `StringContent` before the test inspected the captured request.
3. `CreerOffreAsync_PrixNegatif_400_LeveException`: Fails with `Assert.Throws() Failure: Exception type was not an exact match` (`ArgumentException` vs expected exact `Exception`).

Additionally, a workspace layout integrity violation was detected: **17 C# source files** are located under `.agents/teamwork/`, directly violating project rules and failing E2E test `F02_B03_FolderLayout_AgentsTeamworkDir_MustNotContainCSharpCode`.

Because the unit test suite fails and acceptance criteria are not met, the mandatory verdict is **INTEGRITY VIOLATION**.

---

## 2. Phase Results

| # | Check Name | Status | Details |
|---|------------|:------:|---------|
| 1 | **Static Analysis** (Hardcoded output / Cheat conditionals) | **PASS** | No hardcoded test outputs or mock bypasses in `ServiceAPI.cs` or `Modeles/`. |
| 2 | **Authentic Logic Verification** (401 session revocation & JSON parsing) | **PASS** | `ReinitialiserSession()` correctly revokes session, clears headers, and resets `EstAuthentifie`. `Newtonsoft.Json` parsing handles envelopes and raw arrays. |
| 3 | **Single Service Constraint** | **PASS** | `AutoMasters/Services/` contains strictly ONE file: `ServiceAPI.cs`. |
| 4 | **Build Verification** (`dotnet build AutoMasters.slnx -c Debug`) | **PASS** | Solution builds cleanly with 0 errors and 0 warnings. |
| 5 | **Behavioral Test Execution** (`dotnet test AutoMasters.Tests`) | **FAIL** | **3 tests failed out of 33**. Exit code 1. Acceptance criteria violated. |
| 6 | **Workspace Layout Compliance** (`.agents/teamwork/` purity) | **FAIL** | 17 `.cs` files found in `.agents/teamwork/`, failing E2E test `F02_B03_FolderLayout_AgentsTeamworkDir_MustNotContainCSharpCode`. |

---

## 3. Detailed Forensic Findings

### Finding 1: 3 Unit Test Failures in `AutoMasters.Tests` (CRITICAL)

#### 1.1 `RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl`
- **Location:** `AutoMasters.Tests/ServiceApiTests.cs:239`
- **Error:**
  ```text
  Assert.Contains() Failure: Sub-string not found
  String:    "?search=Porsche&rarity=%C3%89PIQUE"
  Not found: "ÉPIQUE"
  ```
- **Root Cause:**
  In `ServiceAPI.cs` line 301, the query parameter is correctly URL-encoded:
  ```csharp
  queryParams.Add($"rarity={Uri.EscapeDataString(rarete.Trim())}");
  ```
  `Uri.EscapeDataString("ÉPIQUE")` produces `%C3%89PIQUE`.
  In `ServiceApiTests.cs`, line 237-239:
  ```csharp
  string queryString = requete.RequestUri?.Query ?? string.Empty;
  Assert.Contains("Porsche", queryString);
  Assert.Contains("ÉPIQUE", queryString); // FAILS: Query string contains %C3%89PIQUE, not ÉPIQUE
  ```
  The test should either inspect `Uri.UnescapeDataString(queryString)` or check `Uri.EscapeDataString("ÉPIQUE")` (`%C3%89PIQUE`) as is done in line 940 of the same test file.

#### 1.2 `SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer`
- **Location:** `AutoMasters.Tests/ServiceApiTests.cs:63`
- **Error:**
  ```text
  System.ObjectDisposedException : Cannot access a disposed object.
  Object name: 'System.Net.Http.StringContent'.
     at System.Net.Http.HttpContent.ReadAsStringAsync(CancellationToken cancellationToken)
     at AutoMasters.Tests.ServiceApiTests.SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 63
  ```
- **Root Cause:**
  In `ServiceAPI.cs` lines 245-247:
  ```csharp
  using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
  using var reponse = await client.PostAsync("auth/login", contenu);
  ```
  `using var contenu` causes `contenu` (the `HttpContent` object) to be disposed upon exiting `SeConnecterAsync`.
  In `ServiceApiTests.cs` lines 60-63:
  ```csharp
  var requete = _mockHandler.DerniereRequete;
  Assert.NotNull(requete);
  Assert.Equal(HttpMethod.Post, requete.Method);
  string corpsEnvoye = await requete.Content!.ReadAsStringAsync(); // FAILS: Content was disposed
  ```
  Because `HttpClient.PostAsync` attaches `contenu` to the `HttpRequestMessage`, disposing `contenu` leaves the recorded request content in a disposed state. Either `ServiceAPI.cs` should not dispose `contenu` with `using var` when passing it to `HttpClient` (or `MockHttpMessageHandler` should buffer/clone the content stream before returning).

#### 1.3 `CreerOffreAsync_PrixNegatif_400_LeveException`
- **Location:** `AutoMasters.Tests/ServiceApiTests.cs:801`
- **Error:**
  ```text
  Assert.Throws() Failure: Exception type was not an exact match
  Expected: typeof(System.Exception)
  Actual:   typeof(System.ArgumentException)
  ---- System.ArgumentException : Le prix de vente doit être strictement supérieur à zéro. (Parameter 'prix')
     at AutoMasters.Tests.ServiceApiTests.CreerOffreAsync_PrixNegatif_400_LeveException() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 801
  ```
- **Root Cause:**
  In `ServiceAPI.cs` line 541:
  ```csharp
  if (prix <= 0)
      throw new ArgumentException("Le prix de vente doit être strictement supérieur à zéro.", nameof(prix));
  ```
  In xUnit, `Assert.ThrowsAsync<T>` verifies exact type equality (`ex.GetType() == typeof(T)`). Because `ArgumentException != typeof(System.Exception)`, `Assert.ThrowsAsync<Exception>(...)` fails. The test should assert `Assert.ThrowsAsync<ArgumentException>(...)`.

---

### Finding 2: Layout Pollution in `.agents/teamwork/` (HIGH)

- **Rule:** `PROJECT.md` & System Prompt:
  > *"⚠️ `.agents/teamwork/` holds only agent metadata (plans, progress, handoffs). NEVER place source code, tests, or data files here."*
- **Symptom:** Running `dotnet test AutoMasters.E2ETests` failed on:
  ```text
  Échoué F02_B03_FolderLayout_AgentsTeamworkDir_MustNotContainCSharpCode
  Message d'erreur :
  Échec de Assert.AreEqual. Attendu : <0>, Réel : <17>. CRITICAL: .agents/teamwork/ must contain zero C# source files.
  ```
- **Files identified:**
  1. `.agents\teamwork\explorer_m2_1\proposed_Modeles\Booster.cs`
  2. `.agents\teamwork\explorer_m2_1\proposed_Modeles\Carte.cs`
  3. `.agents\teamwork\explorer_m2_1\proposed_Modeles\CartePossedee.cs`
  4. `.agents\teamwork\explorer_m2_1\proposed_Modeles\DemandeAchat.cs`
  5. `.agents\teamwork\explorer_m2_1\proposed_Modeles\DemandeConnexion.cs`
  6. `.agents\teamwork\explorer_m2_1\proposed_Modeles\DemandeCreationOffre.cs`
  7. `.agents\teamwork\explorer_m2_1\proposed_Modeles\DemandeModificationOffre.cs`
  8. `.agents\teamwork\explorer_m2_1\proposed_Modeles\ErreurApi.cs`
  9. `.agents\teamwork\explorer_m2_1\proposed_Modeles\Offre.cs`
  10. `.agents\teamwork\explorer_m2_1\proposed_Modeles\Portefeuille.cs`
  11. `.agents\teamwork\explorer_m2_1\proposed_Modeles\ReponseAchat.cs`
  12. `.agents\teamwork\explorer_m2_1\proposed_Modeles\ReponseCatalogue.cs`
  13. `.agents\teamwork\explorer_m2_1\proposed_Modeles\ReponseConnexion.cs`
  14. `.agents\teamwork\explorer_m2_1\proposed_Modeles\ReponseOffres.cs`
  15. `.agents\teamwork\explorer_m2_1\proposed_Modeles\ReponseReservation.cs`
  16. `.agents\teamwork\explorer_m2_3\proposed_MockHttpMessageHandler.cs`
  17. `.agents\teamwork\explorer_m2_3\proposed_ServiceApiTests.cs`

---

## 4. Evidence (Raw Tool Outputs)

### 4.1 `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` Execution Log
```text
Série de tests pour C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\bin\Debug\net10.0-windows10.0.19041.0\AutoMasters.Tests.dll (.NETCoreApp,Version=v10.0)
Au total, 1 fichiers de test ont correspondu au modèle spécifié.
[xUnit.net 00:00:00.41]     AutoMasters.Tests.ServiceApiTests.RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl [FAIL]
  Échoué AutoMasters.Tests.ServiceApiTests.RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl [3 ms]
  Message d'erreur :
   Assert.Contains() Failure: Sub-string not found
String:    "?search=Porsche&rarity=%C3%89PIQUE"
Not found: "ÉPIQUE"
  Arborescence des appels de procédure :
     at AutoMasters.Tests.ServiceApiTests.RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 239

[xUnit.net 00:00:00.47]     AutoMasters.Tests.ServiceApiTests.SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer [FAIL]
  Échoué AutoMasters.Tests.ServiceApiTests.SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer [3 ms]
  Message d'erreur :
   System.ObjectDisposedException : Cannot access a disposed object.
Object name: 'System.Net.Http.StringContent'.
  Arborescence des appels de procédure :
     at System.Net.Http.HttpContent.ReadAsStringAsync(CancellationToken cancellationToken)
   at AutoMasters.Tests.ServiceApiTests.SeConnecterAsync_IdentifiantsValides_DefinitEnTeteBearer() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 63

[xUnit.net 00:00:00.50]     AutoMasters.Tests.ServiceApiTests.CreerOffreAsync_PrixNegatif_400_LeveException [FAIL]
  Échoué AutoMasters.Tests.ServiceApiTests.CreerOffreAsync_PrixNegatif_400_LeveException [3 ms]
  Message d'erreur :
   Assert.Throws() Failure: Exception type was not an exact match
Expected: typeof(System.Exception)
Actual:   typeof(System.ArgumentException)
---- System.ArgumentException : Le prix de vente doit être strictement supérieur à zéro. (Parameter 'prix')
  Arborescence des appels de procédure :
     at AutoMasters.Tests.ServiceApiTests.CreerOffreAsync_PrixNegatif_400_LeveException() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ServiceApiTests.cs:line 801
----- Inner Stack Trace -----
   at AutoMasters.Services.ServiceAPI.CreerOffreAsync(String copyId, Decimal prix) in C:\Users\stent\source\repos\AutoMasters\AutoMasters\Services\ServiceAPI.cs:line 541

Échoué!  - échec : 3, réussite : 30, ignorée(s) : 0, total : 33, durée : 302 ms - AutoMasters.Tests.dll (net10.0)
```

### 4.2 `Get-ChildItem -Path "AutoMasters\Services"` Execution Log
```text
    Répertoire : C:\Users\stent\source\repos\AutoMasters\AutoMasters\Services

Mode                 LastWriteTime         Length Name
----                 -------------         ------ ----
-a----        07/10/2026     20:21          24028 ServiceAPI.cs
```

---

## 5. Required Remediations

To achieve a CLEAN audit verdict, the following actions must be taken by the remediation team:

1. **Fix `AutoMasters/Services/ServiceAPI.cs`:**
   - In `SeConnecterAsync` (line 245): Remove `using` from `var contenu = new StringContent(json, Encoding.UTF8, "application/json");` or do not prematurely dispose the `HttpContent` object so that captured requests remain readable.
2. **Fix `AutoMasters.Tests/ServiceApiTests.cs`:**
   - In `RecupererCatalogueAsync_AvecFiltres_TransmetParametresUrl` (line 239): Change assertion to either `Assert.Contains(Uri.EscapeDataString("ÉPIQUE"), queryString);` or unescape the query string with `Uri.UnescapeDataString(queryString)`.
   - In `CreerOffreAsync_PrixNegatif_400_LeveException` (line 801): Change `Assert.ThrowsAsync<Exception>` to `Assert.ThrowsAsync<ArgumentException>`.
3. **Clean Up `.agents/teamwork/`:**
   - Delete the 17 `.cs` files in `.agents/teamwork/explorer_m2_1/proposed_Modeles/` and `.agents/teamwork/explorer_m2_3/` so that `.agents/teamwork/` strictly contains only markdown metadata.
4. **Empirical Verification:**
   - Run `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` and verify that 33/33 tests pass.
   - Run `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj` and verify that `F02_B03_FolderLayout_AgentsTeamworkDir_MustNotContainCSharpCode` passes.

---

## 6. Audit Verdict

**Verdict**: **INTEGRITY VIOLATION**  
The deliverable cannot be certified because unit test execution fails (3 failures out of 33 tests) and layout compliance is violated. The work product is rejected until the specified remediations are applied and independently verified.
