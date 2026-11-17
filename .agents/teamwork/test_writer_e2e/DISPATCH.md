## 2026-10-07T16:42:19Z
Sender: 8d7938f5-8832-4e49-be95-98b85530e5cc
Priority: MESSAGE_PRIORITY_HIGH

You are test_writer_e2e.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\test_writer_e2e

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.
Also read:
- Cahier des charges: C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\cahier_des_charges.md
- Survey reports in:
  - c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_specs\report.md
  - c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_api\report.md

Mission: E2E Testing Track Orchestration & Test Suite Construction.
You are responsible for designing and implementing the opaque-box requirement-driven E2E test suite covering ALL features F01–F30 in PROJECT.md.
Principles:
1. Opaque-box, requirement-driven derived from ORIGINAL_REQUEST.md and user specs, NOT implementation internals.
2. 4-Tier test methodology:
   - Tier 1: Feature Coverage (>=5 test cases per feature for core features F01–F28)
   - Tier 2: Boundary & Corner Cases (>=5 test cases per feature)
   - Tier 3: Cross-Feature Combinations (pairwise interactions, e.g. Auth + Catalog, Auth + Market + Portfolio, Booster + Garage valuation)
   - Tier 4: Real-World Application Scenarios (>=5 end-to-end user journeys matching Figma workflows)
3. Write `TEST_INFRA.md` in:
   c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\TEST_INFRA.md
   following the template from instructions.
4. Set up the E2E test runner project `AutoMasters.E2ETests` (or test harness CLI) targeting `net10.0-windows10.0.19041.0` that compiles and runs cleanly via `dotnet test`.
5. When the test suite is complete with full test cases implemented, publish:
   c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\TEST_READY.md
   with the test runner command and coverage summary.

Write your report to:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\test_writer_e2e\report.md
and provide handoff.md in your working directory.
When done, notify the orchestrator.
