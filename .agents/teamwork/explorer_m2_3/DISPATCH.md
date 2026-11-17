## 2026-10-07T17:30:15Z
You are explorer_m2_3.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_3

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.
Also read:
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_api\report.md
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\TEST_READY.md

Scope: Milestone 2 — Unit Test Project Design (Feature F17).
Design the dedicated unit testing project `AutoMasters.Tests`:
1. `AutoMasters.Tests/AutoMasters.Tests.csproj` targeting `net10.0-windows10.0.19041.0` (matching MAUI TFM to allow direct ProjectReference to `AutoMasters.csproj`), referencing `xunit`, `Microsoft.NET.Test.Sdk`, `xunit.runner.visualstudio`, and `Newtonsoft.Json`.
2. Add `AutoMasters.Tests` to `AutoMasters.slnx`.
3. Design deterministic `MockHttpMessageHandler` to simulate Group 2 API HTTP responses without live network sandbox dependency.
4. Design comprehensive unit tests covering:
   - Authentication (success 200 with JWT, failure 401, error parsing)
   - Catalog retrieval and deserialization with `Newtonsoft.Json`
   - Card details retrieval
   - Portfolio retrieval and valuation calculations
   - Market offers, immediate purchase with idempotency key, and reservations
5. Ensure verification command: `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` passes cleanly with 0 failures.

Write your report to:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_3\report.md
and provide handoff.md in your working directory.
When done, notify the orchestrator.
