## 2026-10-09T14:51:56Z
You are worker_m3.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m3

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.

Review the exact implementation blueprints from the three Milestone 3 Explorers:
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_1\report.md and handoff.md
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_2\report.md and handoff.md
- c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_3\report.md and handoff.md

Your Assignment: Implement Milestone 3 — MVVM Presentation Layer & Figma UI (Features F18–F28).
1. Dark Automotive Theme & Colors (F18, F27):
   - Update `AutoMasters/Resources/Styles/Colors.xaml`: Add `ColorBackground` (#10080B), `ColorSurfaceCard` (#221215), `ColorPrimaryRed` (#E63946), `ColorSecondaryGreen` (#2ECC71), `SecondaryGreen` (#10B981 / #2ECC71), high-contrast text tokens.
   - Update `AutoMasters/Resources/Styles/Styles.xaml`: Configure Shell, ContentPage, Border, Entry, Button styles, `CardSurfaceStyle`, `PrimaryButtonStyle`, `SecondaryButtonStyle`, `ReserveButtonStyle`.
2. Shell Navigation (F19):
   - Update `AutoMasters/AppShell.xaml` & `AppShell.xaml.cs`:
     * Unauthenticated route `ConnexionPage`.
     * Authenticated `TabBar` with 3 tabs: Catalogue (`CataloguePage`), Marché (`MarchePage`), Garage (`GaragePage`).
     * Registered detail routes: `FicheCartePage`, `CreationOffrePage`.
     * Strict pure code-behind in `AppShell.xaml.cs` (constructor with `InitializeComponent()` only, zero business logic).
3. Connexion Page & ViewModel (F20):
   - Implement `AutoMasters/Vues/ConnexionPage.xaml` matching Figma Screen 1: Title "AutoMasters" (AUTO white / MASTERS red), subtitle, email Entry with `Keyboard="Email"`, password Entry, `SE CONNECTER` button, loading spinner, error banner.
   - Pure code-behind in `AutoMasters/Vues/ConnexionPage.xaml.cs`.
   - Implement `AutoMasters/VuesModeles/ConnexionVueModele.cs`: French regions, validate non-empty and email format, call `ServiceAPI.SeConnecterAsync`, navigate to `//MainTabs/Catalogue` on success, clear password and preserve email on failure.
4. Catalogue & Fiche Carte Pages (F21, F22):
   - Implement `AutoMasters/Vues/CataloguePage.xaml` and `CataloguePage.xaml.cs` (pure code-behind).
   - Implement `AutoMasters/VuesModeles/CatalogueVueModele.cs`: SearchBar filtering, rarity picker/chips, vehicle CollectionView, pull-to-refresh, navigation to `FicheCartePage`.
   - Implement `AutoMasters/Vues/FicheCartePage.xaml` and `FicheCartePage.xaml.cs` (pure code-behind).
   - Implement `AutoMasters/VuesModeles/FicheCarteVueModele.cs`: detailed vehicle specs grid, circulation count, official price vs market, "Voir les offres sur le Marché" button.
5. Marché, Création d'Offre, Garage & Booster Flow (F23, F24, F25, F26):
   - Implement `AutoMasters/Vues/MarchePage.xaml` and `MarchePage.xaml.cs` (pure code-behind): "🟢 EN DIRECT" badge, offers list, immediate buy with confirmation recap dialog and idempotency, reserve button with 15-min countdown, FAB `+` to sell.
   - Implement `AutoMasters/VuesModeles/MarcheVueModele.cs`.
   - Implement `AutoMasters/Vues/CreationOffrePage.xaml`, `CreationOffrePage.xaml.cs`, and `CreationOffreVueModele.cs`: owned car selector, asking price entry with price > 0 validation, submit offer.
   - Implement `AutoMasters/Vues/GaragePage.xaml` and `GaragePage.xaml.cs` (pure code-behind): total valuation, total P&L ("+32 500 € P&L total" in green, red if negative), owned cards with quantity badge (`x2`), booster section with quota ("3/10 disponibles"), refill timer ("Prochain dans 09:55"), "⚡ OUVRIR" button.
   - Implement `AutoMasters/VuesModeles/GarageVueModele.cs` and `AutoMasters/VuesModeles/BoosterVueModele.cs`: 5-card reveal sequence ("Carte 1 sur 5", "RETOURNER ▶" button, rarity glow, garage sync).
6. Dependency Injection (F28):
   - Update `AutoMasters/MauiProgram.cs`: register `ServiceAPI` (singleton), ViewModels, and Pages.
7. Verification:
   - Run `dotnet build AutoMasters.slnx -c Debug` -> ensure 0 errors, 0 warnings.
   - Run `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` -> all 33 unit tests must pass.
   - Run `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "FullyQualifiedName~F18|FullyQualifiedName~F19|FullyQualifiedName~F20|FullyQualifiedName~F21|FullyQualifiedName~F22|FullyQualifiedName~F23|FullyQualifiedName~F24|FullyQualifiedName~F25|FullyQualifiedName~F26|FullyQualifiedName~F27|FullyQualifiedName~F28"` -> verify 100% pass!
   - Ensure `AutoMasters/Services/` contains ONLY `ServiceAPI.cs`.
   - Ensure `.agents/teamwork/` contains zero `.cs` files.
   - Document all command outputs verbatim in your report and handoff.

Write ownership:
You exclusively own all files in `AutoMasters/Resources/Styles/`, `AutoMasters/Vues/`, `AutoMasters/VuesModeles/`, `AutoMasters/AppShell.xaml*`, and `AutoMasters/MauiProgram.cs`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Write your report to:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\worker_m3\report.md
and provide handoff.md in your working directory.
When done, notify the orchestrator.
