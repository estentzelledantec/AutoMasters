# AutoMasters — E2E Testing Infrastructure (`TEST_INFRA.md`)

**Target Solution:** AutoMasters (.NET MAUI, Windows Unpackaged Desktop, net10.0-windows10.0.19041.0)  
**Test Project:** `AutoMasters.E2ETests`  
**Test Runner Framework:** MSTest (`Microsoft.NET.Test.Sdk 17.13.0`, `MSTest.TestFramework 3.8.2`, `Newtonsoft.Json 13.0.3`)  
**Specification Baseline:** `ORIGINAL_REQUEST.md`, `cahier_des_charges.md` (v1.0), Figma Screens 1–4, and Survey Reports  
**Methodology:** Opaque-Box, Requirement-Driven 4-Tier Testing Model  

---

## 1. Overview & Objectives

The `AutoMasters.E2ETests` test suite is an independent, opaque-box testing framework built to systematically validate all functional, architectural, security, and presentation requirements of the **AutoMasters** application across features **F01–F30**.

### Core Guiding Principles:
1. **Opaque-Box Requirement Derivation:** Tests are derived strictly from specifications, business rules (RM001–RM006), and user stories—never implementation internals.
2. **Zero Facade Testing:** Every test exercises real logical verification, contract assertion, schema evaluation, or state simulation.
3. **Progressive Testability:** Designed to allow milestone-driven verification. Structural and physical artifact checks gracefully report inconclusive states during early milestones (M1–M3) and transition to 100% full green assertions as files are delivered, culminating in M4 clearance.
4. **Offline Isolation:** Features an in-memory mock REST server (`MockGroup2ApiServer`) simulating the Group 2 API (`https://api-cartes-g2.fldsio.com/`), requiring no internet connectivity or fragile sandbox dependencies.

---

## 2. Test Architecture & Runner Setup

### 2.1 Project Layout
```
AutoMasters.E2ETests/
├── AutoMasters.E2ETests.csproj             # net10.0-windows10.0.19041.0 test runner
├── Infrastructure/
│   ├── TestWorkspace.cs                    # Workspace path resolution & static code analysis
│   ├── MockGroup2ApiServer.cs              # Full in-memory Group 2 REST API simulation
│   ├── SpecContracts.cs                    # Opaque-box DTO contracts & JSON models
│   └── E2ETestBase.cs                      # Base test fixture & HttpClient setup
├── Tier1_FeatureCoverage/
│   ├── F01_To_F04_FoundationTests.cs       # 20 tests (F01–F04)
│   ├── F05_To_F08_ModelAndCatalogTests.cs  # 20 tests (F05–F08)
│   ├── F09_To_F12_CardAndMarketTests.cs    # 20 tests (F09–F12)
│   ├── F13_To_F17_ServiceIntegrationTests.cs # 25 tests (F13–F17)
│   ├── F18_To_F22_ViewsAndViewModelsTests.cs # 25 tests (F18–F22)
│   └── F23_To_F28_UiAndArchitectureTests.cs  # 30 tests (F23–F28)
├── Tier2_BoundaryAndCornerCases/
│   ├── F01_To_F07_BoundaryTests.cs         # 35 boundary tests (F01–F07)
│   ├── F08_To_F14_BoundaryTests.cs         # 35 boundary tests (F08–F14)
│   ├── F15_To_F21_BoundaryTests.cs         # 35 boundary tests (F15–F21)
│   └── F22_To_F28_BoundaryTests.cs         # 35 boundary tests (F22–F28)
├── Tier3_CrossFeatureCombinations/
│   └── PairwiseIntegrationTests.cs         # 15 cross-feature interaction tests
└── Tier4_RealWorldScenarios/
    └── UserJourneyTests.cs                 # 5 end-to-end user journeys (Figma workflows)
```

### 2.2 Project Configuration (`AutoMasters.E2ETests.csproj`)
- **Target Framework:** `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`
- **Compiler Options:** `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`
- **Packages:**
  - `Microsoft.NET.Test.Sdk` (17.13.0)
  - `MSTest.TestAdapter` (3.8.2)
  - `MSTest.TestFramework` (3.8.2)
  - `Newtonsoft.Json` (13.0.3)

---

## 3. 4-Tier Test Suite Specification

### Summary Metrics
| Tier | Description | Target | Implemented Tests | Status |
|------|-------------|--------|-------------------|--------|
| **Tier 1** | Feature Coverage (F01–F28) | >=5 per feature (>=140) | **140 tests** | Passed / Progressive |
| **Tier 2** | Boundary & Corner Cases | >=5 per feature (>=140) | **140 tests** | Passed / Progressive |
| **Tier 3** | Cross-Feature Combinations | >=15 pairwise | **15 tests** | 100% Passed |
| **Tier 4** | Real-World User Journeys | >=5 Figma workflows | **5 tests** | 100% Passed |
| **Total** | Full E2E Test Suite | **300 tests** | **300 tests** | **0 Failures, 100% Build Clean** |

---

### Tier 1: Feature Coverage (140 Tests)
- **F01 (Project Solution & .slnx):** 5 tests verifying `.slnx` XML validity, project references, modern format.
- **F02 (Reference Folder Layout):** 5 tests verifying French naming (`Modeles`, `Services`, `Vues`, `VuesModeles`), Platforms, and Resources.
- **F03 (Project Dependencies & Build):** 5 tests verifying `net10.0-windows10.0.19041.0`, `CommunityToolkit.Mvvm`, `Newtonsoft.Json`, MAUI unpackaged target.
- **F04 (Seed Database):** 5 tests verifying `cars_database.json` existence, count (>=12), schema, rarity tiers, HTTP URLs.
- **F05 (API Domain Models):** 5 tests verifying DTO serialization/deserialization via `Newtonsoft.Json`.
- **F06 (Single ServiceAPI Constraint):** 5 tests verifying strictly one file in `Services/`, region discipline, singleton, base address, timeout.
- **F07 (Authentication Service):** 5 tests verifying `POST /auth/login`, 200 OK + JWT token, 401 Unauthorized, 400 Bad Request, token refresh.
- **F08 (Catalog Service):** 5 tests verifying `GET /cards`, pagination, search query filtering, rarity filtering, empty state.
- **F09 (Card Detail Service):** 5 tests verifying `GET /cards/{id}`, technical specs, 404 handling, RM001 authoritative price, RM002 rarity.
- **F10 (Portfolio Service):** 5 tests verifying `GET /portfolio`, total collection valuation, net P&L calculation, multi-copy aggregation, empty garage.
- **F11 (Market Offers Service):** 5 tests verifying `GET /cards/{id}/offers`, seller nicknames, coin prices, sold filter, empty market.
- **F12 (Immediate Purchase Service):** 5 tests verifying `POST /orders/buy`, balance deduction, RM006 `Idempotency-Key` enforcement, insufficient funds, 409 already sold.
- **F13 (Offer Reservation Service):** 5 tests verifying `POST /offers/{id}/reservations`, RM004 future expiration locking, RM003 concurrent conflict prevention.
- **F14 (Offer Creation Service):** 5 tests verifying `POST /offers`, owned card copy check, price validation (>0), RM003 listing guard.
- **F15 (Booster Opening Service):** 5 tests verifying 5-card unpack, quota deduction (3/10), quota exhausted rejection, atomic portfolio grant.
- **F16 (Injectable HTTP Handler):** 5 tests verifying offline testing, 500 error simulation, 502/503 HTML handling, network timeout simulation.
- **F17 (ServiceAPI Unit Tests):** 5 tests verifying `AutoMasters.Tests` project, test adapter, Newtonsoft.Json, auth/catalog coverage.
- **F18 (Login Page View):** 5 tests verifying `ConnexionPage.xaml`, email/password inputs, "SE CONNECTER" button, AUTO MASTERS title, helper links.
- **F19 (Login ViewModel):** 5 tests verifying `ConnexionVueModele`, `ObservableObject`, credentials bindings, IsBusy state, validation.
- **F20 (Navigation Structure):** 5 tests verifying AppShell 5-tab navigation (Catalogue, Marché, Ordres, Garage, Activité), dark theme, pure code-behind.
- **F21 (Catalog Page & ViewModel):** 5 tests verifying `CataloguePage.xaml`, search bar, CollectionView, observable collection, filtering.
- **F22 (Market Page & ViewModel):** 5 tests verifying `MarchePage.xaml`, "🟢 EN DIRECT" badge, Buy/Reserve buttons, FAB `+` sell button.
- **F23 (Immediate Buy & Reserve UX):** 5 tests verifying confirmation recap dialog, coin balance prompt, cancel action, IsBusy debounce, reservation timer.
- **F24 (Sell Card Modal UX):** 5 tests verifying owned card selection, asking price entry, price validation > 0, listing creation, dismiss.
- **F25 (Garage Page & ViewModel):** 5 tests verifying `GaragePage.xaml`, valuation ("482 500 coins"), P&L banner ("+32 500 €" in green), `x2` multiplier badge.
- **F26 (Booster Pack Dialog Flow):** 5 tests verifying quota ("3/10 disponibles"), countdown ("09:55"), "⚡ OUVRIR", 5-card reveal ("Carte 1 sur 5", "RETOURNER ▶").
- **F27 (Design Tokens & Styling):** 5 tests verifying `Colors.xaml`, dark background (`#0D0D0E`), primary red (`#E63946`), green (`#10B981`), `Styles.xaml`.
- **F28 (Code-Behind MVVM Purity):** 5 tests performing static analysis on all `.xaml.cs` files to verify zero business logic, zero HTTP calls, pure constructors.

---

### Tier 2: Boundary & Corner Cases (140 Tests)
- **F01–F07 Boundaries (35 tests):** Malformed XML in `.slnx`, rogue English folders, platform manifests, empty JSON arrays, negative prices/power, extra unexpected JSON fields, unicode accents, base URI trailing slashes, thread-safety, password special characters, huge inputs.
- **F08–F14 Boundaries (35 tests):** Regex metacharacters in search queries, whitespace trimming, zero circulation counts, precision preservation (no float rounding errors), empty portfolio break-even (P&L = 0), exact coin balance purchase, duplicate double-tap idempotency, expired reservations, non-numeric prices.
- **F15–F21 Boundaries (35 tests):** Booster quota 0 vs 1 boundary, refill countdown transition, atomic grant on crash, 502 Bad Gateway HTML proxy error, compact mobile viewport (320px), rapid tab switching, search debounce simulation (300ms).
- **F22–F28 Boundaries (35 tests):** Empty market feed, recap dialog dismissal, price comma/space parsing ("250 000" / "250,50"), single copy hiding multiplier badge, countdown formatting ("09:55"), hex color tokens validation, detection of unauthorized `HttpClient` or `Task.Run` in code-behind.

---

### Tier 3: Cross-Feature Combinations (15 Pairwise Tests)
1. `Pairwise_01`: Auth + Catalog (Unauthenticated request vs Authenticated session).
2. `Pairwise_02`: Auth + Market (Session tokens accessing market offers).
3. `Pairwise_03`: Auth + Portfolio (Bearer token headers populating personal garage).
4. `Pairwise_04`: Catalog + Card Detail (Catalog list selection matches detail specs).
5. `Pairwise_05`: Catalog Filter + Market Offers (Filtering brand matches market offers).
6. `Pairwise_06`: Market Purchase + Garage Valuation (Purchase increases total garage valuation).
7. `Pairwise_07`: Market Purchase + Balance Deduction (Coins deducted from balance).
8. `Pairwise_08`: Market Reservation + Purchase Lock (Reserved card locks out other buyers).
9. `Pairwise_09`: Market Reservation Expiry + Public Availability (Lock released after expiry).
10. `Pairwise_10`: Garage Card + Market Sell Offer (Listing owned card creates active listing).
11. `Pairwise_11`: Booster Pack + Garage Valuation (Unpacking 5 cards increments garage count & value).
12. `Pairwise_12`: Booster Pack + Quota Countdown (Opening decrements daily quota).
13. `Pairwise_13`: Market Idempotency + Double Tap (Identical `Idempotency-Key` prevents double charge).
14. `Pairwise_14`: Seed Database + Catalog Deserialization (`cars_database.json` models match DTOs).
15. `Pairwise_15`: ViewModel Command + Code-Behind Purity (ViewModel commands decoupled from code-behind).

---

### Tier 4: Real-World Scenarios (5 Figma User Journeys)
1. `Journey_01`: New Student Launch & Authentication (Launch -> Credentials submission -> JWT token -> SecureStorage -> Shell initialization).
2. `Journey_02`: Collector Catalog Search, Filter & Inspection (Browse -> Search "Porsche" -> Filter "ÉPIQUE" -> Inspect 911 GT3 RS 525 ch specs).
3. `Journey_03`: Strategic Marketplace Acquisition (Browse Market -> Find Ferrari -> Reserve offer -> Immediate Buy with UUIDv4 Idempotency Key -> Verification of coins deducted and car transferred to garage).
4. `Journey_04`: Portfolio Valuation Audit & Asset Sale (Open Garage -> Verify 449,000 coins and +39,000 € P&L -> Spot Toyota GR Yaris x2 -> List 1 copy via FAB `+` for 49,000 coins -> Confirm active listing).
5. `Journey_05`: Daily Booster Ceremony & Card Reveal Sequence (Open Booster modal -> Verify 3/10 quota & countdown -> Click "⚡ OUVRIR" -> Step through Card 1 to 5 with "RETOURNER ▶" -> Reveal all 5 vehicles -> Add all to Garage collection).

---

## 4. Execution Commands

### Run Full E2E Test Suite
```powershell
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj
```

### Run by Specific Tier
```powershell
# Tier 1: Core Feature Coverage
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=Tier1"

# Tier 2: Boundary & Corner Cases
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=Tier2"

# Tier 3: Cross-Feature Combinations
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=Tier3"

# Tier 4: Real-World User Journeys
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=Tier4"
```

### Run by Implementation Milestone
```powershell
# Milestone 1: Foundation & Data Expansion (F01–F04)
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=M1"

# Milestone 2: Models & ServiceAPI (F05–F17)
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=M2"

# Milestone 3: MVVM Views & Styling (F18–F28)
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj --filter "TestCategory=M3"
```
