# BRIEFING — 2026-10-07T17:43:00Z

## Mission
Design the dedicated unit testing project AutoMasters.Tests (Feature F17) targeting net10.0-windows10.0.19041.0 with deterministic MockHttpMessageHandler and comprehensive test suite.

## 🔒 My Identity
- Archetype: explorer
- Roles: explorer, synthesis
- Working directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m2_3
- Original parent: 8d7938f5-8832-4e49-be95-98b85530e5cc
- Milestone: Milestone 2 — Unit Test Project Design (Feature F17)

## 🔒 Key Constraints
- Read-only investigation — do NOT implement directly in source code unless instructed/allowed
- Design dedicated unit test project AutoMasters.Tests targeting net10.0-windows10.0.19041.0
- Match MAUI TFM to allow direct ProjectReference to AutoMasters.csproj
- Reference xunit, Microsoft.NET.Test.Sdk, xunit.runner.visualstudio, Newtonsoft.Json
- Add AutoMasters.Tests to AutoMasters.slnx
- Design deterministic MockHttpMessageHandler without live network sandbox dependency
- Comprehensive unit tests covering Auth, Catalog, Card Details, Portfolio, Market/Purchase/Reservation
- Ensure verification command `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` passes cleanly with 0 failures
- Output report to .agents/teamwork/explorer_m2_3/report.md and handoff.md

## Current Parent
- Conversation ID: 8d7938f5-8832-4e49-be95-98b85530e5cc
- Updated: not yet

## Investigation State
- **Explored paths**:
  - `AutoMasters/AutoMasters.csproj` & `AutoMasters.slnx`
  - `AutoMasters.E2ETests` (infrastructure, F17_01–F17_05, F17_B01–F17_B05)
  - `spec_miner_survey_api/report.md`, `PROJECT.md`, `ORIGINAL_REQUEST.md`, `TEST_READY.md`
  - Student reference repositories: `AtelierCartes`, `CartesAutomobilesConsole`
- **Key findings**:
  - `AutoMasters.Tests.csproj` must target `net10.0-windows10.0.19041.0` to reference `AutoMasters.csproj` without NETSDK1005 framework mismatch.
  - Required package references: `Microsoft.NET.Test.Sdk 17.13.0`, `xunit 2.9.3`, `xunit.runner.visualstudio 3.0.2`, `coverlet.collector 6.0.4`, `Newtonsoft.Json 13.0.4`.
  - Solution file `AutoMasters.slnx` updated cleanly with modern XML `<Project Path="AutoMasters.Tests/AutoMasters.Tests.csproj" />`.
  - Designed `MockHttpMessageHandler` supporting fluent route registration, request audit trail, and fault injection (network drop, timeout, 502 HTML).
  - Designed 27 unit tests across Auth, Catalog, Details, Portfolio, Market, Boosters, and Robustness.
- **Unexplored areas**: None. Milestone design complete.

## Key Decisions Made
- Chose xUnit 2.9.3 + xunit.runner.visualstudio 3.0.2 with coverlet.collector for standard, clean test execution.
- Added `<NoWarn>$(NoWarn);MVVMTK0045</NoWarn>` to ensure zero compiler warnings.
- Structured unit tests with BTS SIO C# regions and French method names matching educational standards.
- Generated 4 proposed files (`proposed_AutoMasters.Tests.csproj`, `proposed_AutoMasters.slnx`, `proposed_MockHttpMessageHandler.cs`, `proposed_ServiceApiTests.cs`).

## Artifact Index
- `DISPATCH.md` — Incoming dispatch message
- `BRIEFING.md` — Working memory
- `progress.md` — Heartbeat and status
- `report.md` — Comprehensive Unit Test Project Design Report (F17)
- `handoff.md` — 5-Component handoff report
- `proposed_AutoMasters.Tests.csproj` — Concrete project file proposal
- `proposed_AutoMasters.slnx` — Solution file proposal
- `proposed_MockHttpMessageHandler.cs` — Mock HTTP handler proposal
- `proposed_ServiceApiTests.cs` — Unit test suite proposal (27 tests)
