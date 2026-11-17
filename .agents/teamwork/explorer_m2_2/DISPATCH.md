## 2026-10-07T17:30:15Z
[Message] timestamp=2026-10-07T17:30:15Z sender=8d7938f5-8832-4e49-be95-98b85530e5cc priority=MESSAGE_PRIORITY_HIGH content=You are explorer_m2_2.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_2

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.
Also read API survey: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_api\report.md

Scope: Milestone 2 — Unified ServiceAPI Architecture (Features F06–F16).
Design the comprehensive unified HTTP client class in `AutoMasters/Services/ServiceAPI.cs`.
CRITICAL CONSTRAINT: The `AutoMasters/Services/` folder must contain strictly ONE file named `ServiceAPI.cs`.
Features to implement in this single file:
1. BaseAddress: `https://api-cartes-g2.fldsio.com`
2. Injectable `HttpMessageHandler` constructor overload:
   `public ServiceAPI(HttpMessageHandler handler = null)` to support deterministic offline testing and live production.
3. Secure token handling: Attach `Authorization = Bearer <token>` header, save token using `SecureStorage`.
4. Endpoints:
   - `SeConnecterAsync(string email, string password)` -> `POST auth/login`
   - `RecupererCatalogueAsync(string search = null, string rarete = null)` -> `GET cards`
   - `RecupererFicheCarteAsync(string id)` -> `GET cards/{id}`
   - `RecupererPortefeuilleAsync()` -> `GET portfolio` (or `cartes`)
   - `RecupererOffresAsync(string cardId = null)` -> `GET cards/{id}/offers` or `GET offers`
   - `AcheterOffreAsync(string offreId)` -> `POST orders/buy` with `Idempotency-Key` (UUIDv4) header
   - `ReserverOffreAsync(string offreId)` -> `POST offers/{id}/reservations`
   - `CreerOffreAsync(string copyId, decimal prix)` -> `POST offers`
   - `OuvrirBoosterAsync()` -> returns 5 cards
5. Serialization: Strictly `Newtonsoft.Json` (`JsonConvert.DeserializeObject`, `JsonConvert.SerializeObject`).
6. Error handling: Parse `ErreurApi` from error responses.
7. French C# regions (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).

Write your report to:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_2\report.md
and provide handoff.md in your working directory.
When done, notify the orchestrator.
