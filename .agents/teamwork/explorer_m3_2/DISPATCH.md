## 2026-10-09T14:39:26Z
You are explorer_m3_2.
Your Working Directory: c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_2

Mandatory: Read c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md and c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\PROJECT.md first.
Also read specification survey report:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_specs\report.md

Scope: Milestone 3 — Catalogue & Fiche Carte Presentation Layer (Features F21, F22).
Design the Catalog and Card Detail presentation:
1. `CataloguePage.xaml`, `CataloguePage.xaml.cs` & `CatalogueVueModele.cs`:
   - Match Figma Screen 2 / Catalog specs:
     * Header with title "CATALOGUE" and search bar (live text search).
     * Filter chips / Picker for rarity (TOUTES, COMMUNE, RARE, ÉPIQUE, LÉGENDAIRE).
     * Vehicle CollectionView (grid or list) with card presentation: car photo (`ImageUrl`), brand, model, variant, year, rarity badge with color-coded chip, horsepower (`PowerHp` / `Puissance`), circulation count (`ExemplairesEnCirculation`), official price (`PrixOfficiel` in coins).
     * Pull-to-refresh (`RefreshCommand`).
     * Tapping a card triggers `SelectionnerCarteCommand` and navigates to `FicheCartePage` passing card ID or card object.
     * Pure code-behind in `CataloguePage.xaml.cs` (constructor with `InitializeComponent()` only).
2. `FicheCartePage.xaml`, `FicheCartePage.xaml.cs` & `FicheCarteVueModele.cs`:
   - Detailed car view: full resolution car photo, prominent specs grid (Top Speed, 0-100 km/h, Power Hp, Year), rarity badge, circulation count, official price vs market estimate, description.
   - Action buttons: "Voir les offres sur le Marché" (navigates to Marché tab filtered by this card).
   - Pure code-behind in `FicheCartePage.xaml.cs`.
3. French regions in all C# ViewModels (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).

Provide full specification and proposed XAML/C# code in your report. Do NOT write source files directly.
Write report to:
c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m3_2\report.md
and provide handoff.md in your working directory.
When done, notify the orchestrator.
