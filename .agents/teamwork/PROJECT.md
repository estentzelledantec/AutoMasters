# Project: AutoMasters

## Architecture
- **Framework & Runtime**: .NET MAUI targeting Windows (`net10.0-windows10.0.19041.0`, unpackaged desktop execution for BTS SIO evaluation) with Visual Studio 2026 `.slnx` solution format.
- **Structural Pattern**: Strictly MVVM (Model - View - ViewModel) using `CommunityToolkit.Mvvm` (`ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`) and `Newtonsoft.Json` (`JsonConvert`, `[JsonProperty]`).
- **Educational Alignment**: Replicating the exact reference structure of `TestVoiture.git` (BTS SIO 2nd year SLAM standard):
  - French folder naming: `Modeles/`, `Services/`, `Vues/`, `VuesModeles/`, `Platforms/Windows/`, `Resources/`
  - Explicit C# region convention: `#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`
  - Code-behind discipline: Constructor initialization and BindingContext assignment only, zero business logic.
- **Single Service Constraint**: `Services/` contains ONLY one file named `ServiceAPI.cs` (or `ServiceApi.cs`), encapsulating all HTTP communication with Group 2 API (`https://api-cartes-g2.fldsio.com`), session management (`SecureStorage`), and supporting an injectable `HttpMessageHandler` for deterministic unit testing.
- **Dual Track Organization**:
  - Implementation Track: Delivery of M1–M4 (Phase 1 Baseline), followed by M5 (Premium UI/UX, Dynamic Cards & Core Bug Fixes), M6 (Data Expansion & Realistic Luxury Base Prices), and M7 (Final Phase 2 E2E Validation & Hardening).
  - E2E Testing Track: Independent opaque-box test suite (Tiers 1-4) published via `TEST_READY.md`.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| F01 | Project Solution & .slnx | .NET MAUI solution file (`AutoMasters.slnx`) and project matching VS 2026 format | M1 | ORIGINAL_REQUEST §R1 |
| F02 | Reference Folder Layout | Project structure matching `TestVoiture.git` (`Modeles`, `Services`, `Vues`, `VuesModeles`, `Platforms`, `Resources`) | M1 | ORIGINAL_REQUEST §R1 |
| F03 | Project Dependencies & Build | `CommunityToolkit.Mvvm`, `Newtonsoft.Json`, MAUI Controls configured, compiles with 0 errors via `dotnet build` | M1 | ORIGINAL_REQUEST §R1 |
| F04 | Seed Database (`cars_database.json`) | Standalone JSON database of >=12 vehicles matching API schema with real web image URLs and technical specs | M1 | ORIGINAL_REQUEST §R3 |
| F05 | API Domain Models (DTOs) | C# model classes with `Newtonsoft.Json` attributes (`Carte`, `CartePossedee`, `Portefeuille`, `Offre`, `ErreurApi`, etc.) | M2 | Spec Survey & Ref Repo |
| F06 | Single `ServiceAPI` File | Centralized HTTP service in `Services/ServiceAPI.cs` covering all API operations | M2 | ORIGINAL_REQUEST §R1, Acceptance Criteria |
| F07 | Authentication Service (`POST /auth/login`) | Authentication endpoint consuming credentials, parsing token, and saving to `SecureStorage` | M2 | ORIGINAL_REQUEST §R2 |
| F08 | Catalog Service (`GET /cards`) | Retrieval of available cards with pagination, search, and filtering support | M2 | Cahier des charges §Annexe A |
| F09 | Card Detail Service (`GET /cards/{id}`) | Fetching technical specifications, rarity, official price, and circulation count for a card | M2 | Cahier des charges §Annexe A |
| F10 | Portfolio Service (`GET /portfolio`) | Fetching owned cards collection, count, total valuation, and net P&L | M2 | Cahier des charges §Annexe A |
| F11 | Market Offers Service (`GET /cards/{id}/offers`) | Retrieving active sell offers on the marketplace | M2 | Cahier des charges §Annexe A |
| F12 | Immediate Purchase Service (`POST /orders/buy`) | Executing immediate buy order with idempotency key and balance deduction | M2 | Cahier des charges §Annexe A |
| F13 | Offer Reservation Service (`POST /offers/{id}/reservations`) | Reserving a market listing with expiry locking | M2 | Cahier des charges §Annexe A |
| F14 | Offer Creation Service (`POST /offers`) | Listing an owned card for sale on the marketplace | M2 | Cahier des charges §Annexe A |
| F15 | Booster Opening Service | Booster unpack mechanism delivering 5 car cards to player garage | M2 | Figma Mockup 2 & 3 |
| F16 | Injectable HTTP Handler & Mockability | Support for injectable `HttpMessageHandler` enabling offline testing without live network sandbox dependency | M2 | Acceptance Criteria |
| F17 | ServiceAPI Unit Tests | Unit test suite (`AutoMasters.Tests`) verifying authentication, catalog deserialization, and portfolio logic using `Newtonsoft.Json` | M2 | ORIGINAL_REQUEST §Acceptance Criteria |
| F18 | Login Page (`ConnexionPage`) | Figma-matching authentication screen with car badge, AUTO MASTERS title, email/password inputs, and red button | M3 | Figma Mockup 1 |
| F19 | Login ViewModel (`ConnexionVueModele`) | MVVM binding for credentials, error validation message, loading indicator, and navigation | M3 | Figma Mockup 1, MVVM |
| F20 | Navigation Structure (AppShell / TabBar) | 5-tab navigation (Catalogue, Marché, Ordres, Garage, Activité) with dark automotive theme | M3 | Figma Mockup 2 & 4 |
| F21 | Catalog Page (`CataloguePage`) & ViewModel | Responsive grid/list of vehicle cards with search bar, rarity filter, and detail view | M3 | Cahier des charges, MVVM |
| F22 | Market Page (`MarchePage`) & ViewModel | Marketplace feed with "🟢 EN DIRECT" badge, offer cards, seller info, and FAB (`+`) to sell | M3 | Figma Mockup 4 |
| F23 | Immediate Buy & Reserve UX | Confirmation recap dialog for immediate purchase and card reservation interaction | M3 | Figma Mockup 4 |
| F24 | Sell Card Modal UX | Modal dialog allowing user to select owned card, set price, and list on market | M3 | Figma Mockup 4 |
| F25 | Garage Page (`GaragePage`) & ViewModel | Collection display with total valuation ("482 500 coins"), P&L banner, card list with "x2" multiplier badges | M3 | Figma Mockup 2 |
| F26 | Booster Pack Dialog & Reveal Flow | Booster modal ("3/10 disponibles", 09:55 countdown, "⚡ OUVRIR") and 5-card reveal sequence ("Carte 1 sur 5", "RETOURNER ▶") | M3 | Figma Mockup 2 & 3 |
| F27 | Automotive Design Tokens & Styling | Dark theme palette (`#10080B`, `#E63946`, `#221215`, `#1E1014`, `#10B981`), custom card styles, and button templates in `Styles/` | M3 | Figma Design System |
| F28 | Code-Behind MVVM Purity | Verification that all `.xaml.cs` files contain zero business logic or HTTP calls | M3 | Acceptance Criteria |
| F29 | E2E Opaque-Box Test Suite Validation | Passing 100% of E2E test cases across Tiers 1-4 published in `TEST_READY.md` | M4 | Project Pattern Dual Track |
| F30 | Adversarial Coverage Hardening (Tier 5) | White-box stress tests, edge case validation, and forensic integrity audit clearance | M4 | Project Pattern Final Milestone |
| F31 | Premium Dynamic Rarity Cards | Standard MAUI dynamic styling (`Border`, `LinearGradientBrush`, `Shadow`, `CornerRadius="18"`, badges UR/SR/R/UC/C, combat/automotive stats) based on card rarity | M5 | ORIGINAL_REQUEST 2026-10-10 §R1 |
| F32 | Booster Opening Reliability & Thread Safety | Fix face cachée/révélée visibility logic (`EstFaceCachee`), segment color bindings, atomic finish completion (`Interlocked.Exchange`), and network/modal error handling | M5 | ORIGINAL_REQUEST 2026-10-10 §R2 |
| F33 | Dedicated "Ordres" Page & ViewModel | Independent `OrdresPage.xaml`, `OrdresPage.xaml.cs`, and `OrdresVueModele.cs` displaying user's active sales and reservations, registered in AppShell Tab 3 and MauiProgram | M5 | ORIGINAL_REQUEST 2026-10-10 §R2 |
| F34 | Realistic Hypercar & Luxury Base Pricing | Update official prices in `cars_database.json` across 6 rarity tiers (LaFerrari at 3.8M, Jesko at 3.6M, GT-R NISMO at 230k, etc.) | M6 | ORIGINAL_REQUEST 2026-10-10 §R2 |
| F35 | Massive Car Catalog Expansion (50-100 Vehicles) | Expand `cars_database.json` to 65 verified entries with 13 required fields, contiguous IDs "1" to "65", and complete mechanical specifications | M6 | ORIGINAL_REQUEST 2026-10-10 §R3 |
| F36 | Verified Unsplash Vehicle Images | Verified high-resolution vehicle photo URLs starting with `https://images.unsplash.com/` accurately matching make, model, and generation | M6 | ORIGINAL_REQUEST 2026-10-10 §R3 |
| F37 | Phase 2 Regression & E2E Validation | Verify 100% pass on all unit tests (including concurrency test) and 300 E2E tests, plus new unit/integration tests for Phase 2 | M7 | ORIGINAL_REQUEST 2026-10-10 Quality & Architecture |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Solution Foundation & Data Expansion | F01, F02, F03, F04: Solution `.slnx`, MAUI project targeting `net10.0-windows10.0.19041.0`, folder layout matching `TestVoiture.git`, dependencies, and `cars_database.json` | none | DONE |
| M2 | Models, ServiceAPI & Unit Tests | F05, F06, F07, F08, F09, F10, F11, F12, F13, F14, F15, F16, F17: Domain DTOs in `Modeles/`, single `Services/ServiceAPI.cs`, and `AutoMasters.Tests` project | M1 | DONE |
| M3 | MVVM Presentation Layer & Figma UI | F18, F19, F20, F21, F22, F23, F24, F25, F26, F27, F28: Views, ViewModels, Figma styling, navigation, booster flow, pure code-behind | M2 | DONE |
| M4 | Final Milestone: E2E Validation & Hardening | F29, F30: Phase 1 (100% pass of E2E Tiers 1-4) + Phase 2 (Adversarial coverage hardening & Forensic Audit) | M3, E2E Testing Track | DONE |
| M5 | Premium UI/UX, Dynamic Cards & Core Bug Fixes | F31, F32, F33: Dynamic rarity cards in `CataloguePage.xaml` and `GaragePage.xaml`, booster opening fix in `BoosterVueModele.cs` and `GaragePage.xaml`, dedicated `OrdresPage.xaml` and `OrdresVueModele.cs`, AppShell update | M4 | PLANNED |
| M6 | Data Expansion & Realistic Luxury Base Prices | F34, F35, F36: Update `cars_database.json` to 65 verified luxury/hypercars with contiguous IDs ("1" to "65"), realistic prices (hundreds of thousands to millions), and verified Unsplash image URLs | M4 | PLANNED |
| M7 | Phase 2 E2E Validation, Hardening & Academic Compliance | F37: Full regression verification (300 E2E + 94+ Unit tests passing 100%), Phase 2 unit/integration tests, forensic integrity audit, and BTS SIO standard compliance | M5, M6 | PLANNED |

## Interface Contracts

### ServiceAPI ↔ ViewModels
- `Task<ReponseConnexion> SeConnecterAsync(string email, string password)`
- `Task<List<Carte>> RecupererCatalogueAsync(string search = null, string rarete = null)`
- `Task<Carte> RecupererFicheCarteAsync(string id)`
- `Task<Portefeuille> RecupererPortefeuilleAsync()`
- `Task<List<Offre>> RecupererOffresAsync(string cardId = null)`
- `Task<bool> AcheterOffreAsync(string offreId)`
- `Task<bool> ReserverOffreAsync(string offreId)`
- `Task<bool> CreerOffreAsync(string copyId, decimal prix)`
- `Task<List<CartePossedee>> OuvrirBoosterAsync()`

### DTOs & Models (`Modeles/`)
- `Carte.cs`:
  - Propriétés de données : `Id`, `Marque`, `Modele`, `Variante`, `Annee`, `Puissance`, `Rarete`, `PrixOfficiel`, `ExemplairesEnCirculation`, `ImageUrl`, `Description`, `VitesseMax`, `ZeroACent`
  - Propriétés de présentation calculées (Phase 2) :
    - `CouleurPrimaireRarete` : couleur hexadécimale associée à la rareté (`#F59E0B`, `#EC4899`, `#0284C7`, `#10B981`, `#64748B`)
    - `CouleurSecondaireRarete` : nuance sombre associée pour les dégradés
    - `BadgeRareteCourt` : libellé court ("UR", "SR", "R", "UC", "C")
    - `ScoreAttaque` : score calculé à partir de la puissance
    - `ScoreDefense` : score calculé à partir de la vitesse max / accélération
    - `NomComplet`, `PuissanceFormatee`, `VitesseMaxFormatee`, `ZeroACentFormate`, `PrixOfficielFormate`
- `CartePossedee.cs`: `Id`, `Carte`, `Quantite`, `PrixAchat`, `ValeurActuelle`, `GainPerte`
- `Portefeuille.cs`: `ValeurTotale`, `TotalGainPerte`, `CartesPossedees`
- `Offre.cs`: `Id`, `Carte`, `Vendeur`, `Prix`, `EstReservee`, `DateExpiration`, `Statut`
- `BoosterVueModele.cs` (Phase 2 Contract) :
  - `EstFaceCachee` (`bool`, inversé de `EstRetournee`)
  - `CouleurSegment1`, `CouleurSegment2`, etc.
  - Concurrence thread-safe via `Interlocked.Exchange` sur `finDejaDeclenchee`
- `OrdresVueModele.cs` (Phase 2 Contract) :
  - `ObservableCollection<Offre> MesVentes`
  - `ObservableCollection<Offre> MesReservations`
  - `int OngletActifIndex`
  - `IAsyncRelayCommand ChargerDonneesCommand`
  - `IRelayCommand<string> ChangerOngletCommand`

## Code Layout
```
c:\Users\stent\source\repos\AutoMasters\
├── AutoMasters.slnx
├── AutoMasters/
│   ├── AutoMasters.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── AppShell.xaml / AppShell.xaml.cs
│   ├── MauiProgram.cs
│   ├── Modeles/
│   │   ├── Carte.cs
│   │   ├── CartePossedee.cs
│   │   ├── Portefeuille.cs
│   │   ├── Offre.cs
│   │   ├── Booster.cs
│   │   ├── DemandeConnexion.cs
│   │   ├── ReponseConnexion.cs
│   │   └── ErreurApi.cs
│   ├── Services/
│   │   └── ServiceAPI.cs   <-- ONLY file in Services folder
│   ├── VuesModeles/
│   │   ├── BaseVueModele.cs
│   │   ├── ConnexionVueModele.cs
│   │   ├── CatalogueVueModele.cs
│   │   ├── MarcheVueModele.cs
│   │   ├── GarageVueModele.cs
│   │   ├── BoosterVueModele.cs
│   │   └── OrdresVueModele.cs   <-- NEW: Distinct Orders ViewModel
│   ├── Vues/
│   │   ├── ConnexionPage.xaml / .xaml.cs
│   │   ├── CataloguePage.xaml / .xaml.cs
│   │   ├── MarchePage.xaml / .xaml.cs
│   │   ├── GaragePage.xaml / .xaml.cs
│   │   ├── FicheCartePage.xaml / .xaml.cs
│   │   └── OrdresPage.xaml / .xaml.cs   <-- NEW: Distinct Orders View
│   ├── Platforms/
│   │   └── Windows/
│   │       ├── App.xaml / .xaml.cs
│   │       ├── app.manifest
│   │       └── Package.appxmanifest
│   └── Resources/
│       ├── AppIcon/
│       ├── Fonts/
│       ├── Images/
│       └── Styles/
│           ├── Colors.xaml
│           └── Styles.xaml
├── AutoMasters.Tests/
│   ├── AutoMasters.Tests.csproj
│   ├── ServiceApiTests.cs
│   ├── ChallengerM3VerificationTests.cs
│   ├── ChallengerM4Phase2Tests.cs
│   └── MockHttpMessageHandler.cs
├── AutoMasters.E2ETests/
│   ├── AutoMasters.E2ETests.csproj
│   └── ... (300 E2E tests Tiers 1-4)
└── cars_database.json   <-- Expanded to 65 vehicles with realistic luxury prices
```
