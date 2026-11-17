## 2026-10-10T18:59:10Z
You are the Worker for Milestone 5 on the AutoMasters project.
Your working directory is: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m5
Original request path: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md
Project specification: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md
Survey findings report: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_survey_ui_bugs\report.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope of Milestone 5 (F31, F32, F33):
1. F31: Premium Dynamic Rarity Cards:
   - In `AutoMasters/Modeles/Carte.cs`: Add calculated presentation properties (`CouleurPrimaireRarete`, `CouleurSecondaireRarete`, `BadgeRareteCourt`, `ScoreAttaque`, `ScoreDefense`) formatted as French/academic standard within proper `#region` blocks.
   - In `AutoMasters/Vues/CataloguePage.xaml`: Redesign vehicle card presentation using standard .NET MAUI controls (`Border`, `LinearGradientBrush`, `Shadow`, `RoundRectangle`, rarity badge pill, automotive combat stats ⚔ / 🛡 / 🪙) as detailed in `report.md`. Keep design responsive and clean for BTS SIO.
2. F32: Booster Opening Reliability & Thread Safety:
   - In `AutoMasters/VuesModeles/BoosterVueModele.cs`:
     * Add `EstFaceCachee` (inverting `EstRetournee`) with `[NotifyPropertyChangedFor(nameof(EstFaceCachee))]`.
     * Expose direct segment colors (`CouleurSegment1`, `CouleurSegment2`, etc.) to prevent MAUI bool-to-Color XAML binding conversion failures.
     * Fix concurrency race condition using `Interlocked.Exchange(ref finDejaDeclenchee, 1) == 0` before triggering `SurBoosterTermine` so concurrent clicks on finish do not double-invoke (resolving `Challenger_Booster_Adversarial_ConcurrentClicksOnFinish_ThreadSafeIdempotency`).
     * Ensure thread safety when invoking events on `MainThread`.
   - In `AutoMasters/Vues/GaragePage.xaml`:
     * Fix visibility bindings for booster cards (`EstFaceCachee` for face cachée, `EstRetournee` for face révélée).
     * Fix segment colors to bind to the ViewModel color properties.
     * Ensure ActivityIndicator is shown during loading.
   - In `AutoMasters/VuesModeles/GarageVueModele.cs`:
     * Ensure booster modal is opened only when cards have been successfully retrieved, and display user-friendly alert on failure.
3. F33: Dedicated "Ordres" Page & ViewModel:
   - Create `AutoMasters/VuesModeles/OrdresVueModele.cs` (MVVM, ObservableObject, managing `MesVentes`, `MesReservations`, `OngletActifIndex`, with strict `#region` blocks: `#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).
   - Create `AutoMasters/Vues/OrdresPage.xaml` and `OrdresPage.xaml.cs` (pure code-behind with `#region Constructeurs`, zero business logic, displaying segmented views for active sales and reservations).
   - In `AutoMasters/AppShell.xaml`: Update Tab 3 ("Ordres") to route to `vues:OrdresPage`.
   - In `AutoMasters/MauiProgram.cs`: Register `OrdresVueModele` and `OrdresPage` in the DI container.

Write Ownership:
You own exclusively:
- `AutoMasters/Modeles/Carte.cs`
- `AutoMasters/VuesModeles/BoosterVueModele.cs`
- `AutoMasters/VuesModeles/GarageVueModele.cs`
- `AutoMasters/VuesModeles/OrdresVueModele.cs`
- `AutoMasters/Vues/OrdresPage.xaml`
- `AutoMasters/Vues/OrdresPage.xaml.cs`
- `AutoMasters/Vues/CataloguePage.xaml`
- `AutoMasters/Vues/GaragePage.xaml`
- `AutoMasters/AppShell.xaml`
- `AutoMasters/MauiProgram.cs`
DO NOT write inside `.agents/teamwork/` (except your working directory metadata `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m5`).

Verification Requirements:
1. Run `dotnet build AutoMasters.slnx` and confirm 0 errors and 0 warnings.
2. Run `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` and confirm all tests pass (including `Challenger_Booster_Adversarial_ConcurrentClicksOnFinish_ThreadSafeIdempotency`).
3. Run `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj` and confirm 300/300 E2E tests pass.
4. Write your completion report in `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m5\handoff.md`.
5. Send a message to parent with summary and file path.
