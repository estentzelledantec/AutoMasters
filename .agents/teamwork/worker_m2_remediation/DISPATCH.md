## 2026-10-07T18:14:01Z
You are worker_m2_remediation.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m2_remediation

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.

Review the exact fix blueprints from the three remediation Explorers:
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_fix_1\report.md and handoff.md
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_fix_2\report.md and handoff.md
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_fix_3\report.md and handoff.md

Your Assignment: Implement Milestone 2 Remediation Fixes:
1. Update `AutoMasters/Services/ServiceAPI.cs`:
   - Enforce single service file constraint: `AutoMasters/Services/` must contain strictly ONE file (`ServiceAPI.cs`).
   - In `LireReponseAsync`:
     * On HTTP 401 Unauthorized: Clear `client.DefaultRequestHeaders.Authorization = null;`, `jetonSession = null;`, and evict `CleStockageJeton` from `SecureStorage` (wrapped in try/catch for headless test resilience) so `EstAuthentifie` becomes `false`.
     * Fix error message fallback: Use `string.IsNullOrWhiteSpace(erreur?.Message) ? fallback : erreur.Message` so fallback error messages are never suppressed by empty strings.
   - On 200 OK across collection/deserialization methods: Catch `JsonReaderException` and `JsonSerializationException` and wrap in clean domain `Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.")`.
   - Ensure all French region blocks remain intact.
2. Update `AutoMasters/Modeles/ErreurApi.cs`:
   - Update backing field `message` to nullable `string?` (default null).
   - Add properties for alternative error keys (`[JsonProperty("error")] public string? Error`, `[JsonProperty("detail")] public string? Detail`, `[JsonProperty("title")] public string? Title`).
   - In `Message` getter: return message if not whitespace, else detail, else error, else title, else null.
3. Update `AutoMasters.Tests/ServiceApiTests.cs`:
   - Add test `Test_401_RevoqueSessionEtAuthentification` verifying session teardown on 401.
   - Add 4 direct array tests (`RecupererCatalogueAsync_TableauBrutDirect_DeserialiseListeCartes`, `RecupererPortefeuilleAsync_TableauBrutDirect_CreePortefeuilleEtCalculeValorisation`, `RecupererOffresAsync_TableauBrutDirect_DeserialiseOffres`, `OuvrirBoosterAsync_TableauBrutDirect_Retourne5Cartes`).
   - Add test `RecupererCatalogueAsync_Reponse200JsonCorrompu_LeveExceptionClaireSansCrash`.
4. Verification:
   - Run `dotnet build AutoMasters.slnx -c Debug` -> verify exit code 0, 0 warnings, 0 errors.
   - Run `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` -> verify all 33 unit tests pass cleanly.
   - Run `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "FullyQualifiedName~F05|FullyQualifiedName~F06|FullyQualifiedName~F07|FullyQualifiedName~F08|FullyQualifiedName~F09|FullyQualifiedName~F10|FullyQualifiedName~F11|FullyQualifiedName~F12|FullyQualifiedName~F13|FullyQualifiedName~F14|FullyQualifiedName~F15|FullyQualifiedName~F16|FullyQualifiedName~F17"` -> verify all pass.
   - Document all command outputs in your handoff report.

Write ownership:
You exclusively own `AutoMasters/Services/ServiceAPI.cs`, `AutoMasters/Modeles/ErreurApi.cs`, and `AutoMasters.Tests/ServiceApiTests.cs`.
