## 2026-10-09T14:39:26Z
[Message] timestamp=2026-10-09T14:39:26Z sender=8d7938f5-8832-4e49-be95-98b85530e5cc priority=MESSAGE_PRIORITY_HIGH content=You are explorer_m3_1.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_1

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.
Also read specification survey report:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_specs\report.md

Scope: Milestone 3 — Shell Navigation, Dark Theme Styling, and Connexion Page (Features F18, F19, F20, F28).
Design the foundation for the presentation layer:
1. Dark Automotive Theme styling tokens (`Resources/Styles/Colors.xaml` and `Styles.xaml`):
   - Background `#10080B`, card surface `#221215`, primary accent red `#E63946`, secondary green `#2ECC71`, text colors `#FFFFFF`, `#A0A0A0`, border radius 12.
2. `AppShell.xaml` & `AppShell.xaml.cs`:
   - Authentication-aware routing: Unauthenticated root (`//ConnexionPage`), Authenticated tab bar (`//MainTabs`) with 3 tabs: Catalogue (`CataloguePage`), Marché (`MarchePage`), Garage (`GaragePage`).
   - Registered detail routes: `FicheCartePage`, `CreationOffrePage`.
   - Pure code-behind in `AppShell.xaml.cs` (constructor with `InitializeComponent()` only).
3. `ConnexionPage.xaml`, `ConnexionPage.xaml.cs` & `ConnexionVueModele.cs`:
   - Match Figma Screen 1 (`media_1791389223586_1f733aab.png`): Title "AutoMasters", subtitle, email input, password input, login button ("SE CONNECTER"), loading spinner / `IsBusy`, error message banner.
   - `ConnexionVueModele.cs`: uses `CommunityToolkit.Mvvm` (`[ObservableProperty]`, `[RelayCommand]`), calls `ServiceAPI.SeConnecterAsync(Email, Password)`, navigates to `//MainTabs/Catalogue` on success, displays friendly French error on failure.
   - French regions (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).
4. DI Container in `MauiProgram.cs`:
   - Register `ServiceAPI` (singleton), ViewModels, and Pages.

Provide full specification and proposed XAML/C# code in your report. Do NOT write source files directly.
Write report to:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_1\report.md
and provide handoff.md in your working directory.
When done, notify the orchestrator.
