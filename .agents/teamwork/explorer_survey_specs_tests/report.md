# AutoMasters Phase 2 — Survey & Investigation Report: Specifications, Reference Card UI, and Test Architecture

**Author:** Explorer Subagent (`explorer_survey_specs_tests`)  
**Target Solution:** AutoMasters (.NET MAUI, Windows Desktop Unpackaged, net10.0-windows10.0.19041.0)  
**Date:** 2026-10-10  
**References:**
- `ORIGINAL_REQUEST.md` (Specifically update 2026-10-10T18:46:21Z)
- `cahier_des_charges.md` (v1.0)
- Reference Card UI Mockup: `media_1791657264584_e9c97e4d.png`
- Test Infrastructure: `TEST_INFRA.md`, `TEST_READY.md`
- Solution Projects: `AutoMasters/`, `AutoMasters.Tests/`, `AutoMasters.E2ETests/`

---

## 1. Executive Summary

This investigation surveys the business specifications, user interface references, and testing infrastructure for **Phase 2 of AutoMasters**. 

### Key Findings:
1. **Ordres Section Duplication Identified:** In `AutoMasters/AppShell.xaml` (lines 51–56), Tab 3 ("Ordres") was configured with `ContentTemplate="{DataTemplate vues:MarchePage}"`, creating an exact duplicate of Tab 2 ("Marché"). Furthermore, Tab 5 ("Activité") also duplicates `vues:GaragePage`. A distinct `OrdresPage` and `OrdresVueModele` are required to manage user buy order history (`POST /orders/buy`), active sell listings (`POST /offers`), and active reservations (`POST /offers/{id}/reservations`).
2. **Reference Card UI Decoded:** Analysis of `media_1791657264584_e9c97e4d.png` reveals a premium collectible trading card aesthetic featuring glowing border gradients, rarity pill badges ("UR", "SR"), bookmark stars, and an iconic lower stat row. For AutoMasters, this can be achieved cleanly with standard .NET MAUI controls (`Border`, `LinearGradientBrush`, `Shadow`, `DataTriggers`), adapting the RPG crossed-swords/shield stats into automotive technical specifications (Puissance ⚡, Vitesse Max 🏁, 0-100 km/h ⏱️, Cours 🪙).
3. **Booster Pack Freeze & Failure Root Causes:** The booster opening sequence freezes or fails due to three compounding defects: (a) a XAML visibility inversion bug where both the face cachée and face révélée bind to `EstRetournee` without an inverter, causing a blank card initially and overlapping elements when flipped; (b) absence of an `ActivityIndicator` during the network call to `POST /boosters/open`; and (c) silent exception handling where errors are caught into `Message`, but no UI label displays the error and no fallback/retry button exists, trapping the user in an unresponsive modal.
4. **Test Suite Integrity & Critical Regression Invariants:** The test suite currently comprises 300 E2E tests (MSTest) and 94 unit tests (xUnit), achieving a 100% pass rate. Crucially, static tests enforce strict invariants: `ChallengerM4Phase2Tests` enforces that car IDs in `cars_database.json` **must be strictly contiguous ("1" to "N")** and that image URLs **must start with `https://images.unsplash.com/`**. Any deviation from these invariants will break existing regression tests.

---

## 2. Cahier des Charges & Specifications Analysis

### 2.1 Specification Baseline (`cahier_des_charges.md`)
The specification defines four user roles and six core business rules (RM001–RM006):
- **RM001:** The server is the authoritative source for official card values with timestamps.
- **RM002:** Card rarity is server-provided and tied to circulation volume.
- **RM003:** A card listed, reserved, or gifted cannot be involved in conflicting concurrent operations.
- **RM004:** Reservations carry an expiration date.
- **RM005:** Transactions succeed only when the server returns a final positive status and a transaction ID.
- **RM006:** Idempotency keys (`Idempotency-Key` UUIDv4) must prevent duplicate transactions on network retries.

### 2.2 Mentions of Core Domain Concepts
| Concept | Mentions in `cahier_des_charges.md` | Business Role & Constraints |
|---|---|---|
| **Ordres / Commandes** | Section 3 (Acheteur: "créer des ordres conditionnels"); Annexe A (`POST /orders/buy`) | Execution of immediate purchase orders, transaction finality (`RM005`), idempotency protection (`RM006`). |
| **Booster** | Implemented as Feature F26; server endpoint `POST /boosters/open` | 5-card daily pack opening ceremony, daily quota decrement (e.g. 3/10), countdown timer to refill, atomic portfolio grant. |
| **Marché** | Section 3 (Vendeur, Acheteur); Section 6 (EF030, EF031); Annexe A (`POST /offers`, `GET /cards/{id}/offers`) | Peer-to-peer active marketplace feed, live listing status ("🟢 EN DIRECT"), immediate buy, card reservation. |
| **Cartes** | Section 6 (EF010, EF011, EF012, EF020, EF050); Annexe A (`GET /cards`, `GET /cards/{id}`) | Complete catalog of collectible luxury/sports vehicles with technical specs (Puissance, Vitesse, 0-100, Rareté, Exemplaires). |

### 2.3 The "Ordres" Duplication Bug in `AppShell.xaml`
In `AutoMasters/AppShell.xaml`:
```xml
<!-- Onglet 2: Marché / Trading -->
<Tab Title="Marché" Route="Marche">
    <ShellContent
        Title="Marché"
        ContentTemplate="{DataTemplate vues:MarchePage}"
        Route="MarchePage" />
</Tab>

<!-- Onglet 3: Ordres -->
<Tab Title="Ordres" Route="Ordres">
    <ShellContent
        Title="Ordres"
        ContentTemplate="{DataTemplate vues:MarchePage}"
        Route="OrdresPage" />
</Tab>
```
**Direct Observation:** Tab 3 literally instantiates `vues:MarchePage`. To the user, clicking "Ordres" opens the exact same market feed as Tab 2. Furthermore, Tab 5 ("Activité") points to `vues:GaragePage`.

### 2.4 Business Requirements for the Independent "Ordres" Page
To provide dedicated and meaningful functionality matching the Cahier des Charges:
1. **User Transaction & Order History:**
   - Display all immediate buy orders executed by the user via `POST /orders/buy` (showing Transaction ID, Vehicle, Amount paid, Date/Time, and Status "Complété").
   - Display all active and past sell listings created by the user via `POST /offers` (showing Asking Price, Card, Date, Status "En vente" / "Vendu").
   - Display active reservations placed by the user via `POST /offers/{id}/reservations` (showing Expiration countdown, Target card, Status "Réservé").
2. **Filtering & Segmented Navigation:**
   - Filter chips: `TOUS`, `ACHATS` (Buy Orders), `VENTES` (Sell Listings), `RÉSERVATIONS` (Active Locks).
3. **Card Summary & Status Indicators:**
   - Distinct badge pills: `COMPLÉTÉ` (Green `#10B981`), `EN COURS / EN VENTE` (Amber `#F59E0B`), `RÉSERVÉ` (Blue `#38BDF8`), `EXPIRÉ / ANNULÉ` (Gray `#6B7280`).
4. **Empty State:**
   - Graceful illustration or icon (`📋`) with a friendly explanation when no orders exist.

---

## 3. Reference Card UI Analysis (`media_1791657264584_e9c97e4d.png`)

### 3.1 Visual Anatomy of the Reference Cards
Inspection of the five cards in `media_1791657264584_e9c97e4d.png`:
- **Card 1 (UR Bugatti Chiron):** Rich warm amber/gold marble wavy gradient background, outer golden glowing border, top-left "UR" rounded pill badge (black text on gold), top-right outline star, high-quality full car photo, bold dark typography, subtle divider, bottom stats row with red crossed swords (⚔️ 8 432) and blue shield (🛡️ 8 000).
- **Card 2 (UR GMK vidéaste):** Identical UR theme with monogram emblem, ⚔️ 7 067, 🛡️ 3 935.
- **Card 3 (SR Ferrari SF90 Stradale):** Neon magenta/rose gradient, "SR" pink pill badge, ⚔️ 6 360, 🛡️ 4 874.
- **Card 4 (SR Rimac Nevera):** SR magenta theme with filled gold star (★ favorited), ⚔️ 6 378, 🛡️ 5 600.
- **Card 5 (SR Toyota Yaris Cross):** SR magenta theme with outline star, ⚔️ 6 330, 🛡️ 5 696.

### 3.2 Detailed Visual Components Breakdown

| Visual Component | Reference Image Styling | AutoMasters Target Adaptation (.NET MAUI) |
|---|---|---|
| **Border & Outer Glow** | Subtle neon halo/glow matching card rarity tier. | `Border` with `StrokeShape="RoundRectangle 16"`, `StrokeThickness="1.5"`, and `Shadow` configured with `Radius="10"`, `Opacity="0.35"`, and color matching rarity token. |
| **Border Stroke** | Solid or subtle gradient line. | `LinearGradientBrush` for `Stroke` (e.g. for Légendaire: `#F59E0B` to `#D97706`; for Épique: `#A855F7` to `#7C3AED`). |
| **Card Background** | Wavy marbled pastel/vibrant gradient. | Luxury dark cockpit theme with rarity gradient tint: dark graphite surface (`#181014`) subtly blended towards the rarity color at 15–20% opacity. |
| **Rarity Badge** | Rounded pill badge at top-left ("UR", "SR"). | Rarity pill badge: `★ LÉGENDAIRE` (`#F59E0B`), `ÉPIQUE` (`#A855F7`), `RARE` (`#38BDF8`), `PEU COMMUNE` (`#10B981`), `COMMUNE` (`#9CA3AF`). |
| **Top-Right Accessory** | Bookmark / favorite star icon. | Circulation badge (`210 ex.`) or bookmark icon in top-right corner. |
| **Vehicle Image Frame** | Clean rectangular frame with upper corner rounding, aspect fill. | `Image` with `Aspect="AspectFill"`, `HeightRequest="130"` (grid) or `240` (detail), with upper rounded corners matching card radius. |
| **Model & Brand Typography** | Bold black font on lighter gradient. | High-contrast dark luxury styling: Brand in bold white (`#FFFFFF`, 14-16pt), Model in vibrant crimson accent (`#E63946`) or muted silver (`#D1D5DB`). |
| **Stats Grid / Stat Row** | RPG gaming icons (⚔️ Attack, 🛡️ Defense). | **Automotive Technical Specifications Micro-Grid:**<br>⚡ Puissance (`PuissanceFormatee`, e.g. `525 ch`)<br>🏁 Vitesse (`VitesseMaxFormatee`, e.g. `296 km/h`)<br>⏱️ 0–100 km/h (`ZeroACentFormate`, e.g. `3.2 s`)<br>🪙 Cours Officiel (`PrixOfficielFormate`, e.g. `223 000 coins`). |
| **Visual Depth & Layers** | Smooth multi-level depth with subtle card shadows. | MAUI standard `Shadow` properties, subtle dark gradient overlays on image bottom edges to ensure readability. |

### 3.3 What to Adopt vs. What to Keep Distinct
- **Adopt:**
  - Distinct border color and glowing shadow per rarity.
  - Prominent rarity badge pill in top corner.
  - Iconic bottom specification badges.
  - Clean card rounded corners (`RoundRectangle 14` to `16`).
- **Keep Distinct (Automotive & BTS SIO Identity):**
  - **Do NOT use RPG swords and shields (⚔️ / 🛡️):** AutoMasters is a luxury automotive trading app; specifications must be horsepower, top speed, acceleration, and coin valuation.
  - **Maintain Dark Theme Consistency:** The reference image uses bright light backgrounds; AutoMasters must preserve its signature `#10080B` / `#1E1014` dark cockpit palette so text readability and contrast remain pristine.
  - **Student-Level Maintainability:** Use standard XAML controls (`Border`, `Grid`, `DataTriggers`, `LinearGradientBrush`) rather than complex SkiaSharp canvas shaders or custom renderers.

---

## 4. Booster Opening Mechanic: Failure & Freeze Root Causes

### 4.1 Execution Flow Analysis
The booster opening workflow involves:
1. `GaragePage.xaml` button bound to `GarageVueModele.OuvrirBoosterCommand`.
2. `GarageVueModele.OuvrirBoosterAsync()` instantiates `BoosterVueModele`, hooks `SurBoosterTermine`, sets `EstBoosterModalOuvert = true`, and invokes `DemarrerOuvertureCommand.ExecuteAsync()`.
3. `BoosterVueModele.DemarrerOuvertureAsync()` calls `ServiceAPI.OuvrirBoosterAsync()`, which posts to `/boosters/open`.
4. User taps `ActionCarteSuivanteCommand` to flip the card (`EstRetournee = true`) and advance to the next card.

### 4.2 Detailed Root Cause Analysis of Freezes and Failures

#### Bug A: XAML Card Flip Binding Logic Bug
In `GaragePage.xaml` (lines 276–295):
```xml
<!-- Face cachée (avant retournement) -->
<VerticalStackLayout
    VerticalOptions="Center"
    Spacing="12"
    IsVisible="{Binding BoosterVueModele.EstRetournee, Converter={x:Null}, FallbackValue=True}">
    <Label Text="🏎️" FontSize="56" HorizontalOptions="Center" />
    <Label Text="AUTOMASTERS" ... />
</VerticalStackLayout>

<!-- Face révélée (après retournement) -->
<VerticalStackLayout
    Spacing="8"
    IsVisible="{Binding BoosterVueModele.EstRetournee}">
    <Image Source="{Binding BoosterVueModele.CarteActuelle.ImageUrl}" ... />
    ...
</VerticalStackLayout>
```
- **The Issue:** `BoosterVueModele` starts with `EstRetournee = false`. Because `Converter={x:Null}` is specified on the face cachée, MAUI directly evaluates `IsVisible = false`. Consequently, **both the card back and the card front are invisible**! The user sees a blank dark rectangle.
- When the user taps "RETOURNER ▶", `EstRetournee` flips to `true`. Now **both the card back and the card front become visible simultaneously**, overlapping and corrupting the layout.

#### Bug B: Missing Loading Feedback (`ActivityIndicator`)
- When `OuvrirBoosterAsync` is triggered, the network call to `POST /boosters/open` may take 500–2000ms.
- During this window, `CarteActuelle` is `null`, and `cartesTirees` is empty. The modal opens immediately, but displays empty labels and no progress. Users perceive this latency as an application freeze.

#### Bug C: Silent Exception Handling Trapping the User
In `BoosterVueModele.cs`:
```csharp
catch (Exception ex)
{
    Message = ex.Message;
}
```
- If the API returns 400 (Quota Exhausted), 401, or 500, `Message` is set, but `GaragePage.xaml` has **no Label bound to `BoosterVueModele.Message`**.
- Because `cartesTirees` remains empty, subsequent taps on `ActionCarteSuivanteCommand` hit:
  ```csharp
  if (cartesTirees.Count == 0 || EstTermine) return;
  ```
- The button does nothing. The modal remains stuck on screen with no indication of error, effectively freezing the UI until the application is restarted.

### 4.3 Proposed Architectural Fix
1. **Property Addition in `BoosterVueModele`:**
   Add `[ObservableProperty] private bool estFaceCachee = true;` or `public bool EstFaceCachee => !EstRetournee;` with `[NotifyPropertyChangedFor(nameof(EstFaceCachee))]` on `estRetournee`.
2. **ActivityIndicator in Modal:**
   Display a centered `ActivityIndicator` bound to `BoosterVueModele.EstOccupe`.
3. **Inline Error Banner in Modal:**
   Add an error container displaying `BoosterVueModele.Message` with a "Fermer" button when an error occurs, so the modal never locks the user.
4. **Idempotency Guard:**
   Prevent concurrent taps during `DemarrerOuvertureAsync` and `ActionCarteSuivante`.

---

## 5. Test Suite Analysis & Regression Safeguards

### 5.1 Overview of Current Test Infrastructure

| Suite | Framework | Total Tests | Pass Rate | Ignored / Skipped | Scope |
|---|---|---|---|---|---|
| `AutoMasters.E2ETests` | MSTest (net10.0) | 300 | 298 Passed | 2 Skipped (`F18_05`, `F18_B03` intentionally skipped for email/links) | Opaque-box 4-Tier test suite covering features F01–F30. |
| `AutoMasters.Tests` | xUnit (net10.0) | 94 | 93 Passed | 1 Skipped (`Challenger_Login_InvalidEmailFormat` skipped for identifier auth) | Unit, ViewModel, and Adversarial Challenger tests. |

### 5.2 Test Architecture Breakdown

#### A. `AutoMasters.E2ETests` (300 Tests across 4 Tiers):
1. **Tier 1 — Feature Coverage (140 tests, F01–F28):**
   - 5 tests per feature validating contracts, DTO serialization, service interfaces, XAML templates, and MVVM purity.
2. **Tier 2 — Boundary & Corner Cases (140 tests):**
   - Stress-tests edge cases: negative values, empty lists, SQL/script patterns, unicode accents, token expiration, idempotency headers (`RM006`), and route isolation.
3. **Tier 3 — Cross-Feature Pairwise Integrations (15 tests):**
   - Tests combined workflows (e.g. `Pairwise_11`: Booster pack unpack increments garage valuation and count; `Pairwise_13`: Double-tap idempotency).
4. **Tier 4 — Real-World Figma User Journeys (5 tests):**
   - Full end-to-end user workflows matching Figma screens: Authentication, Catalog search/filter, Marketplace buy, Portfolio valuation and sell, Booster unpack ceremony.

#### B. `AutoMasters.Tests` (94 Tests):
- `ServiceApiTests.cs` (45 tests): Offline mock tests for all `ServiceAPI` endpoints via `MockHttpMessageHandler`.
- `ViewModelEmpiricalChallengeTests.cs` (19 tests): Behavioral assertions on `CatalogueVueModele`, `GarageVueModele`, `MarcheVueModele`, `BoosterVueModele`.
- `ChallengerM3VerificationTests.cs` (16 tests): Code-behind purity (zero `HttpClient` / `ServiceAPI` in `.xaml.cs`), Shell routing invariants.
- `ChallengerM4Phase2Tests.cs` (14 tests): Data expansion schema, required fields, and adversarial validation.

### 5.3 Critical Regression Constraints Discovered

During investigation, three strict test invariants were uncovered that **must NOT be violated** during Phase 2 implementation:

1. **Contiguous IDs in `cars_database.json`:**
   In `ChallengerM4Phase2Tests.cs` (lines 214–224):
   ```csharp
   [Fact]
   public void F30_Adversarial_AllIdsAreContiguousOrUniqueValidStrings()
   {
       for (int i = 1; i <= array.Count; i++)
       {
           var found = array.FirstOrDefault(item => item["id"]?.ToString() == i.ToString());
           Assert.NotNull(found);
       }
   }
   ```
   **Constraint:** Car IDs in `cars_database.json` MUST be sequential string integers: `"1"`, `"2"`, `"3"`, ..., `"50"`. Non-contiguous IDs (e.g., `"card_101"` or gaps) will cause immediate test failures.

2. **Unsplash Image URL Host Enforcement:**
   In `ChallengerM4Phase2Tests.cs` (lines 280–311):
   ```csharp
   Assert.StartsWith("https://images.unsplash.com/", carte.ImageUrl);
   Assert.All(cartes, c => Assert.Contains("images.unsplash.com", c.ImageUrl));
   ```
   **Constraint:** All car image URLs in `cars_database.json` MUST be valid HTTPS URLs hosted on `https://images.unsplash.com/`. Random external domains (e.g. Wikimedia, Flickr, Imgur) will fail this test.

3. **Code-Behind MVVM Purity Engine:**
   In `ChallengerM3VerificationTests.cs` and `F28_01` to `F28_05`:
   - All `.xaml.cs` files are inspected via static AST.
   - Code-behind files must contain **zero** references to `HttpClient`, `ServiceAPI`, or asynchronous business tasks.
   - Any new page (e.g. `OrdresPage.xaml.cs`) must contain only standard constructor initialization (`InitializeComponent()`).

---

## 6. Recommended Test Suites for Phase 2

To safeguard against regressions while validating new features, the following test cases should be added:

### 6.1 Dynamic Card Styles & Rarity Validation Tests
- **Test Case 1 (XAML Triggers / Resources):** Verify that `CataloguePage.xaml` and `FicheCartePage.xaml` define dynamic style bindings or triggers for each of the five rarity tiers (`MYTHIQUE`, `LÉGENDAIRE`, `ÉPIQUE`, `RARE`, `COMMUNE`).
- **Test Case 2 (Rarity Color Consistency):** Verify that border stroke and shadow colors match the specification design tokens defined in `Colors.xaml`.
- **Test Case 3 (Automotive Specs Micro-Grid):** Assert that card templates bind to `PuissanceFormatee`, `VitesseMaxFormatee`, and `ZeroACentFormate`.

### 6.2 Booster Opening Reliability Tests
- **Test Case 1 (Face Cachée Visibility Inversion):** Assert that when `EstRetournee == false`, the card back is visible and the card front is invisible. When `EstRetournee == true`, the card back is hidden and the card front is visible.
- **Test Case 2 (Error State Handling & Dismissal):** Simulate a 400 `QUOTA_EXHAUSTED` or 500 error from `POST /boosters/open`; verify `EstOccupe == false`, error message is captured, and the modal remains dismissible without hanging.
- **Test Case 3 (Loading State):** Verify `EstOccupe == true` while awaiting network response, preventing duplicate taps.

### 6.3 Ordres Page Independent Functionality Tests
- **Test Case 1 (AppShell Route Disassociation):** Assert that Tab 3 Route "Ordres" is mapped to `vues:OrdresPage` and NOT `vues:MarchePage`.
- **Test Case 2 (OrdresVueModele Data Fetching):** Assert that `OrdresVueModele` exposes distinct collections for executed buy orders, active sell offers, and reservations.
- **Test Case 3 (Status Badge Evaluation):** Verify correct formatting and color-coding for status strings ("COMPLÉTÉ", "EN VENTE", "RÉSERVÉ").
- **Test Case 4 (Code-Behind Purity):** Confirm `OrdresPage.xaml.cs` contains zero HTTP or service logic.

### 6.4 Expanded `cars_database.json` Validation Tests
- **Test Case 1 (Count Threshold):** Assert `cars_database.json` contains $\ge 50$ entries (target: 50–100).
- **Test Case 2 (Realistic Luxury Pricing):** Assert that all luxury/hyper cars have `officialPrice >= 100000.0`.
- **Test Case 3 (Unsplash URL Format & HTTPS):** Assert all URLs begin with `https://images.unsplash.com/`.
- **Test Case 4 (Contiguous IDs):** Assert IDs strictly match `"1"` through `N`.
- **Test Case 5 (Model Specificity):** Validate that brand, model, and year fields correspond to genuine automotive models.

---

## 7. Zero-Regression Execution Protocol

To guarantee that Phase 2 updates achieve 100% test pass rates without regressions:

1. **Baseline Verification:**
   Execute current tests to confirm clean baseline:
   ```powershell
   dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj
   dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj
   ```
2. **Incremental Validation Steps:**
   - **Step 1 (Data Expansion):** When updating `cars_database.json`, run `ChallengerM4Phase2Tests` immediately to ensure contiguous IDs, Unsplash URLs, and schema compliance.
   - **Step 2 (Ordres Page):** When introducing `OrdresPage` and `OrdresVueModele`, run `ChallengerM3VerificationTests` to ensure AppShell route hierarchy and code-behind purity remain intact.
   - **Step 3 (Booster Fix):** After updating `BoosterVueModele` and `GaragePage.xaml`, execute `ViewModelEmpiricalChallengeTests` and `UserJourneyTests.Journey_05_Daily_Booster_Ceremony_And_Reveal_Sequence`.
   - **Step 4 (Final Regression Pass):** Run both test suites with zero filters to verify 0 failures across all 394+ tests.

---

## 8. Conclusion & Recommendations

1. **Clear Division of Labor:**
   - The UI designer/implementer should focus on introducing `OrdresPage.xaml` / `OrdresVueModele.cs`, correcting the `EstFaceCachee` visibility binding in `GaragePage.xaml`, and applying rarity gradients/shadows across card components.
   - The data engineer should expand `cars_database.json` to 50+ vehicles using verified Unsplash car photography, contiguous ID numbering (`1`–`50`), and realistic luxury pricing ($>100\text{k}$ coins).
2. **Architecture Compliance:**
   - Continue strictly respecting the single file constraint in `Services/` (`ServiceAPI.cs`).
   - Retain MVVM purity in all new views.
   - Adhere to the `net10.0-windows10.0.19041.0` desktop unpackaged target.
