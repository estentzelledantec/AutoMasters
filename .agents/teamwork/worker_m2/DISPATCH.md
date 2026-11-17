## 2026-10-07T17:45:30Z
[Message] timestamp=2026-10-07T17:45:30Z sender=8d7938f5-8832-4e49-be95-98b85530e5cc priority=MESSAGE_PRIORITY_HIGH content=You are worker_m2.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m2

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.

Review blueprints and ready-to-deploy code from Explorers:
- Domain Models: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_1\proposed_Modeles\ (and report.md / handoff.md)
- Unified ServiceAPI: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_2\report.md (and handoff.md)
- Unit Test Project: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_3\ (proposed_AutoMasters.Tests.csproj, proposed_AutoMasters.slnx, proposed_MockHttpMessageHandler.cs, proposed_ServiceApiTests.cs)

Your Assignment: Implement Milestone 2 — Domain Models, Unified ServiceAPI Layer & Unit Test Suite (Features F05–F17).
1. Deploy all domain models and DTOs into `AutoMasters/Modeles/` from `explorer_m2_1/proposed_Modeles/`:
   - `Carte.cs`, `CartePossedee.cs`, `Portefeuille.cs`, `Offre.cs`, `Booster.cs`
   - `DemandeConnexion.cs`, `ReponseConnexion.cs`, `DemandeCreationOffre.cs`, `DemandeModificationOffre.cs`
   - `ReponseOffres.cs`, `ErreurApi.cs`, `ReponseCatalogue.cs`, `DemandeAchatImmediat.cs`, `ReponseAchatImmediat.cs`, `DemandeReservation.cs`
2. Deploy the unified `AutoMasters/Services/ServiceAPI.cs` from `explorer_m2_2/report.md`:
   - STRICT CONSTRAINT: `AutoMasters/Services/` must contain ONLY ONE file named `ServiceAPI.cs`.
   - Implement all endpoints (auth, catalog, card detail, portfolio, offers, immediate buy with UUIDv4 Idempotency-Key header, reservations, sell offer, booster opening).
   - Support `public ServiceAPI(HttpMessageHandler? handler = null)` overload for offline unit tests and mock handlers.
   - Use `Newtonsoft.Json` exclusively.
   - Strictly adhere to French regions (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).
3. Deploy the unit test project `AutoMasters.Tests`:
   - Create `AutoMasters.Tests/AutoMasters.Tests.csproj` (targeting `net10.0-windows10.0.19041.0`, referencing `AutoMasters.csproj`, `xunit`, `Microsoft.NET.Test.Sdk`, `Newtonsoft.Json`).
   - Create `AutoMasters.Tests/MockHttpMessageHandler.cs`.
   - Create `AutoMasters.Tests/ServiceApiTests.cs` (27 comprehensive unit tests).
   - Update `AutoMasters.slnx` at repo root to include `<Project Path="AutoMasters.Tests/AutoMasters.Tests.csproj" />`.
4. Verification:
   - Run `dotnet build AutoMasters.slnx -c Debug` -> verify exit code 0, 0 warnings, 0 errors.
   - Run `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` -> verify all unit tests pass with 0 failures!
   - Run `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "FullyQualifiedName~F05|FullyQualifiedName~F06|FullyQualifiedName~F07|FullyQualifiedName~F08|FullyQualifiedName~F09|FullyQualifiedName~F10|FullyQualifiedName~F11|FullyQualifiedName~F12|FullyQualifiedName~F13|FullyQualifiedName~F14|FullyQualifiedName~F15|FullyQualifiedName~F16|FullyQualifiedName~F17"` -> verify all pass.
   - Document all command outputs in your handoff report.

Write ownership:
You exclusively own `AutoMasters/Modeles/*`, `AutoMasters/Services/ServiceAPI.cs`, `AutoMasters.Tests/*`, and `AutoMasters.slnx`.
