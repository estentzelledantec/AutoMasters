# AutoMasters — Specification Survey & Feature Mining Report

**Document Version:** 1.0.0  
**Author:** spec_miner_survey_specs  
**Target Level:** 2nd-year BTS SIO (Services Informatiques aux Organisations — option SLAM)  
**Framework / Stack:** .NET MAUI (C# / XAML), Visual Studio 2026 (`.slnx`), MVVM Pattern, Newtonsoft.Json  
**Target Platform:** Mobile (iOS / Android / Desktop-responsive)  
**Date:** October 7, 2026  

---

## 1. Executive Summary & Authoritative Specification Sources

This document provides the definitive specification survey, feature mining analysis, design system deconstruction, user stories, and architecture requirements for the **AutoMasters** digital automotive trading card application.

The findings synthesize four authoritative specification sources:
1. **Cahier des charges (v1.0):** `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\cahier_des_charges.md`
2. **Figma UI/UX Mockups (4 Screen Images):**
   - Screen 1: Login / Authentication (`media_1791389223586_1f733aab.png`)
   - Screen 2: Garage & Booster Pack Dialog (`media_1791389263369_a72b0e54.png`)
   - Screen 3: Booster Card Reveal & Unpacking Flow (`media_1791389277036_c3cce1e8.png`)
   - Screen 4: Marketplace / Trading Feed (`media_1791389292458_a07f6e9a.png`)
3. **Original Project Request & Constraints:** `.agents/teamwork/ORIGINAL_REQUEST.md` (and `prompt_draft.md`)
4. **Pedagogical Standards:** 2nd-year BTS SIO SLAM national curriculum standards (Clean MVVM separation, secure token storage, DI container, HTTP client abstraction, JSON serialization, unit testing).

---

## 2. BTS SIO 2nd-Year Requirements & User Stories

### 2.1 Academic Level Calibration (BTS SIO SLAM - 2nd Year)
The application must reflect the expected proficiency of a graduating BTS SIO SLAM student:
- **Clean MVVM Separation:** Strict decoupling between Views (XAML only), ViewModels (`ObservableObject`, commands, observable collections), Models/DTOs, and Services. Zero business logic or HTTP calls in `.xaml.cs` code-behind.
- **Service Layer Abstraction:** A centralized HTTP client encapsulated within a single `ServiceAPI.cs` class in the `Services/` folder.
- **Serialization Standard:** Explicit use of `Newtonsoft.Json` (`JsonConvert.DeserializeObject`, `JsonConvert.SerializeObject`, `[JsonProperty]`).
- **Platform Security:** Safe storage of authentication JWT tokens using .NET MAUI `SecureStorage`.
- **Fault-Tolerant UX:** Graceful error handling (friendly user alerts, fallback images, empty states, offline notifications) avoiding unhandled crashes.

### 2.2 User Stories & Acceptance Criteria

#### Epic 1: Authentication & Security (AUTH)
- **US-AUTH-01:** *As a user, I want to authenticate with my email and password so that I can access my garage and trade cards.*
  - **AC 1.1:** The login screen provides dedicated email and password input fields matching Figma mockup 1.
  - **AC 1.2:** Submitting valid credentials issues a `POST /auth/login` request and receives an access token (and refresh token).
  - **AC 1.3:** The JWT token is securely saved using MAUI `SecureStorage`.
  - **AC 1.4:** Invalid credentials trigger a friendly error message without clearing the email field.
- **US-AUTH-02:** *As a returning user, I want my session to remain authenticated across app launches.*
  - **AC 2.1:** If a valid token exists in `SecureStorage`, the app navigates directly to the main AppShell.
  - **AC 2.2:** If an API call returns `401 Unauthorized`, `ServiceAPI` attempts a `POST /auth/refresh` before redirecting to login.

#### Epic 2: Vehicle Catalog & Discovery (CAT)
- **US-CAT-01:** *As a collector, I want to browse all available vehicles in the catalog so that I can discover rare cars.*
  - **AC 1.1:** Cards are displayed in a clean responsive grid or card list featuring image thumbnail, brand, model, rarity badge, and current official price (`RM001`).
  - **AC 1.2:** Pull-to-refresh or automatic loading loads items via `GET /cards`.
- **US-CAT-02:** *As a collector, I want to search and filter the catalog by brand, model, and rarity.*
  - **AC 2.1:** A search bar filters cards dynamically by text (`EF011`).
  - **AC 2.2:** Filter chips or picker allow filtering by rarity (COMMUNE, RARE, ÉPIQUE, LÉGENDAIRE) and brand (`EF012`).
- **US-CAT-03:** *As a collector, I want to view detailed technical specifications and circulation count of a selected car.*
  - **AC 3.1:** Tapping a card opens a detailed view (`GET /cards/{id}`) showing brand, model, variant, year, power, rarity, official price, and copies in circulation (`EF020`, `RM002`).

#### Epic 3: Marketplace & Trading (MKT)
- **US-MKT-01:** *As a buyer, I want to browse active market listings submitted by other collectors.*
  - **AC 1.1:** The "MARCHÉ" tab displays active sell offers (`GET /cards/{id}/offers` or market feed) matching Figma mockup 4.
  - **AC 1.2:** Each listing displays vehicle thumbnail, brand, model, year, seller username (e.g. `par TurboAce`), price in coins, and rarity badge.
  - **AC 1.3:** A live status badge ("🟢 EN DIRECT") is visible at the top.
- **US-MKT-02:** *As a buyer, I want to purchase a car immediately with one tap.*
  - **AC 2.1:** Tapping "ACHETER IMMÉDIATEMENT" opens a confirmation recap dialog displaying card name, price, and current user coin balance (`EF030`).
  - **AC 2.2:** Confirming sends `POST /orders/buy` with an idempotency key (`RM006`).
  - **AC 2.3:** Upon successful purchase (`RM005`), coins are deducted, the car is transferred to the user's garage, and the offer is removed from the market.
- **US-MKT-03:** *As a buyer, I want to reserve a card temporarily so that another buyer cannot take it while I finalize my decision.*
  - **AC 3.1:** Tapping "RÉSERVER" locks the offer and assigns an expiration timestamp (`RM004`).
  - **AC 3.2:** A card reserved cannot be bought by others or double-reserved (`RM003`).
- **US-MKT-04:** *As a seller, I want to put an eligible car from my garage up for sale.*
  - **AC 4.1:** Tapping the red Floating Action Button (`+`) on the Marché screen opens a "Vendre une carte" modal.
  - **AC 4.2:** The user selects an unreserved, owned card, sets an asking price, and confirms.
  - **AC 4.3:** Sends `POST /offers` (`EF031`) and updates the user garage status.

#### Epic 4: Garage & Portfolio Management (GAR)
- **US-GAR-01:** *As a collector, I want to view my entire personal garage collection.*
  - **AC 1.1:** The "GARAGE" tab displays all owned cars (`GET /portfolio`, `EF050`).
  - **AC 1.2:** When multiple copies of the same card are owned, a quantity badge (e.g., `x2`) is displayed over the thumbnail (Mockup 2 & 3).
  - **AC 1.3:** Each car row displays thumbnail, brand, model, rarity badge, current valuation, purchase price, and individual P&L (+ / -).
- **US-GAR-02:** *As an investor, I want to track my total collection valuation and net profit/loss (P&L).*
  - **AC 2.1:** The garage header prominently displays total collection valuation ("482 500 coins", `EF051`).
  - **AC 2.2:** Net total P&L is shown with color-coded profit ("+32 500 € P&L total" in vibrant green).

#### Epic 5: Booster Pack Opening Flow (BOOST)
- **US-BOOST-01:** *As a player, I want to check my booster pack availability and cooldown timer.*
  - **AC 1.1:** A booster modal or section indicates remaining boosters ("3/10 disponibles") and a refill timer ("Prochain dans 09:55", Mockup 2).
- **US-BOOST-02:** *As a player, I want to open a booster pack and discover 5 new cars.*
  - **AC 2.1:** Tapping "⚡ OUVRIR" launches the unpacking ceremony (Mockup 2).
  - **AC 2.2:** An interactive 5-segment progress bar indicates progress ("Carte 1 sur 5", Mockup 3).
  - **AC 2.3:** Tapping "RETOURNER ▶" reveals each card one by one, adding them to the user's garage.

---

## 3. Core UI Screens & Workflows (Figma Deconstruction)

### Screen 1: Login / Authentication (`media_1791389223586_1f733aab.png`)
```
+-------------------------------------------------------+
|                       [ Notch ]                       |
|                                                       |
|                     +-----------+                     |
|                     | [CAR LOGO]|  (Squircle #221215) |
|                     +-----------+                     |
|                                                       |
|                  AUTO MASTERS                         |
|          Collecte · Échange · Compétition             |
|                                                       |
|   ADRESSE EMAIL                                       |
|   +-----------------------------------------------+   |
|   |  etudiant@sio.lan                             |   |
|   +-----------------------------------------------+   |
|                                                       |
|   MOT DE PASSE                                        |
|   +-----------------------------------------------+   |
|   |  ••••••••••••                                 |   |
|   +-----------------------------------------------+   |
|                               Mot de passe oublié ?   |
|                                                       |
|   +-----------------------------------------------+   |
|   |                 SE CONNECTER                  |   |  (#E63946 Solid Red)
|   +-----------------------------------------------+   |
|                                                       |
|           Pas encore de compte ? Créer un compte      |
+-------------------------------------------------------+
```
- **Key Elements:**
  - Car silhouette badge inside dark-red glowing container.
  - Title: Dual-color `AUTO` (white) + `MASTERS` (red).
  - Uppercase field labels with high contrast.
  - Large red CTA pill button.
  - Secondary helper links in subtle red / muted gray.

### Screen 2: Garage & Portfolio (`media_1791389263369_a72b0e54.png`)
```
+-------------------------------------------------------+
|  GARAGE                                               |
|                                                       |
|  VALEUR TOTALE DE LA COLLECTION                       |
|  482 500 coins                                        |
|  +32 500 €  P&L total                                 |
|                                                       |
|  MA COLLECTION · 5 CARTES                             |
|  +-------------------------------------------------+  |
|  | [Img] Porsche       ÉPIQUE        222 000 €     |  |
|  |       911 GT3 RS                  +24 000 € [⋮] |  |
|  +-------------------------------------------------+  |
|  | [Img] Renault       RARE           44 000 €     |  |
|  |       Mégane R.S.                  +2 000 € [⋮] |  |
|  +-------------------------------------------------+  |
|  | [x2]  Toyota        RARE           47 000 €     |  |
|  | [Img] GR Yaris                    +15 000 € [⋮] |  |
|  +-------------------------------------------------+  |
|  | [Img] Lotus         ÉPIQUE         89 000 €     |  |
|  |       Emira V6 First Edition       -2 000 € [⋮] |  |
|  +-------------------------------------------------+  |
+-------------------------------------------------------+
| [🔍 Cat.]  [🏪 Marché]  [📋 Ordres]  [🏎️ Garage] [⏱️ Act.]|
+-------------------------------------------------------+
```
- **Key Elements:**
  - Header portfolio card: Total balance in coins, positive profit badge in green (`+32 500 €`).
  - Collection items: Thumbnail on left, car name and model, rarity badge top right, current value and net profit/loss, multi-copy badge (`x2`) on thumbnail.
  - Action menu (`⋮`) for selling or viewing car specs.

### Screen 3: Booster Pack Reveal Flow (`media_1791389263369` & `media_1791389277036`)
```
Step 1: Pack Selection Modal             Step 2: Card Reveal Ceremony
+------------------------------------+   +------------------------------------+
|  [✕]                               |   |  [✕]                               |
|       BOOSTER AUTOMASTERS          |   |       BOOSTER AUTOMASTERS          |
|         ● ● ● ○ ○ ○ ○ ○ ○ ○        |   |       [■] [ ] [ ] [ ] [ ] (5 bars) |
|           3/10 disponibles         |   |           Carte 1 sur 5            |
|         Prochain dans 09:55        |   |                                    |
|                                    |   |                                    |
|          +--------------+          |   |          +--------------+          |
|          |     🏎️       |          |   |          |     🏎️       |          |
|          | AUTOMASTERS  |          |   |          | AUTOMASTERS  |          |
|          +--------------+          |   |          +--------------+          |
|                                    |   |                                    |
|        [ ⚡ OUVRIR ]               |   |        [ RETOURNER ▶ ]             |
+------------------------------------+   +------------------------------------+
```
- **Workflow:**
  1. Triggered from Garage screen or Booster button.
  2. Displays quota (e.g. 3/10 available) and cooldown countdown timer.
  3. Clicking "⚡ OUVRIR" consumes 1 booster and enters the 5-card reveal sequence.
  4. Step indicator shows progress ("Carte 1 sur 5" with segmented progress bar).
  5. Clicking "RETOURNER ▶" flips the card face-up with animation, displaying vehicle photo, rarity, and specs.
  6. Button advances to next card until all 5 cards are added to the garage.

### Screen 4: Marketplace / Trading Feed (`media_1791389292458_a07f6e9a.png`)
```
+-------------------------------------------------------+
|  MARCHÉ                                   🟢 EN DIRECT |
|  8 offres actives                                     |
|                                                       |
|  +-------------------------------------------------+  |
|  | [Img] Ferrari                  ★ LÉGENDAIRE     |  |
|  |       LaFerrari Aperta · 2017                   |  |
|  |       480 000 coins · par TurboAce              |  |
|  |                                                 |  |
|  |  [ ACHETER IMMÉDIATEMENT ]   [ RÉSERVER ]       |  |
|  +-------------------------------------------------+  |
|                                                       |
|  +-------------------------------------------------+  |
|  | [Img] Koenigsegg               ★ LÉGENDAIRE     |  |
|  |       Jesko Absolut · 2022                      |  |
|  |       2 650 000 coins · par ApexHunter          |  |
|  |                                                 |  |
|  |  [ ACHETER IMMÉDIATEMENT ]   [ RÉSERVER ]       |  |
|  +-------------------------------------------------+  |
|                                                       |
|  +-------------------------------------------------+  |
|  | [Img] Lamborghini              ÉPIQUE           |  |
|  |       Huracán STO · 2022                        |  |
|  |       318 000 coins · par NightRider            |  |
|  |                                                 |  |
|  |  [ ACHETER IMMÉDIATEMENT ]   [ RÉSERVER ]       |  |
|  +-------------------------------------------------+  |
|                                                       |
|                                                 (+)   |  (FAB Red Button)
+-------------------------------------------------------+
| [🔍 Cat.]  [🏪 Marché]  [📋 Ordres]  [🏎️ Garage] [⏱️ Act.]|
+-------------------------------------------------------+
```
- **Key Elements:**
  - Header with live badge "🟢 EN DIRECT" and count of active offers.
  - Offer cards with thumbnail, brand, model & year, price in coins with seller nickname.
  - Dual action buttons: "ACHETER IMMÉDIATEMENT" (green) and "RÉSERVER" (dark red).
  - Floating Action Button (FAB `+`) at bottom right for creating a new sell listing.

---

## 4. Design System Specification

### 4.1 Color Tokens
| Token Name | Hex Code | Visual Application |
|---|---|---|
| `ColorBackground` | `#0D0D0E` | Main page background (deep dark gray / near black) |
| `ColorSurfaceCard` | `#1A1A1E` | Card containers, list items, dialog containers |
| `ColorSurfaceInput` | `#242426` | Text entry background, secondary control backings |
| `ColorSurfaceModal` | `#141416` | Bottom navigation bar, modal backgrounds |
| `ColorPrimaryRed` | `#E63946` | Brand accent: "MASTERS", "SE CONNECTER", "OUVRIR", "RETOURNER", FAB `+` |
| `ColorSecondaryGreen` | `#10B981` | Success accent: "ACHETER IMMÉDIATEMENT", positive P&L (`+32 500 €`), "🟢 EN DIRECT" |
| `ColorReserveDarkRed` | `#2D1518` | "RÉSERVER" button background |
| `ColorReserveBorderRed` | `#EF4444` | "RÉSERVER" border and text color |
| `ColorRarityLegendary` | `#F59E0B` | Gold/Amber badge text, border, and star icon (`★ LÉGENDAIRE`) |
| `ColorRarityEpic` | `#A855F7` | Purple/Violet badge text and border (`ÉPIQUE`) |
| `ColorRarityRare` | `#38BDF8` | Cyan/Blue badge text and border (`RARE`) |
| `ColorRarityCommon` | `#9CA3AF` | Silver/Gray badge text and border (`COMMUNE`) |
| `ColorTextPrimary` | `#FFFFFF` | Main titles, brand names, button text |
| `ColorTextSecondary` | `#9CA3AF` | Subtitles, models, year, secondary labels |
| `ColorTextMuted` | `#6B7280` | Inactive dots, helper links, metadata |
| `ColorTimerAmber` | `#FBBF24` | Countdown timer text ("Prochain dans 09:55") |

### 4.2 Typography Hierarchy
| Role | Font Family / Weight | Size | Casing / Tracking |
|---|---|---|---|
| App Main Logo | Sans-serif Bold / Black | 24–28pt | UPPERCASE, LetterSpacing = 2 |
| Page Titles (`MARCHÉ`, `GARAGE`) | Sans-serif Bold | 22–24pt | UPPERCASE |
| Big Stat Numbers (`482 500`) | Sans-serif Bold | 32–36pt | Normal |
| Card Title (Brand) | Sans-serif Bold | 16–18pt | Title Case |
| Card Subtitle (Model · Year) | Sans-serif Regular | 13–14pt | Normal |
| Form Labels (`ADRESSE EMAIL`) | Sans-serif SemiBold | 11–12pt | UPPERCASE, LetterSpacing = 1 |
| Button Text | Sans-serif Bold | 14–15pt | UPPERCASE |
| Badges & Status | Sans-serif SemiBold | 10–11pt | UPPERCASE |

### 4.3 Component Geometry & Spacing
- **Corners (`CornerRadius`):**
  - Cards & Containers: `14` to `16`
  - Action Buttons (Pill / Rounded): `10` to `12`
  - Entry Fields: `8` to `10`
  - Badges (Rarity / Multiplier): `4` to `6`
  - Floating Action Button (FAB): `28` (Circle, 56x56)
- **Margins & Paddings:**
  - Page Horizontal Padding: `16px`
  - Card Internal Padding: `14px`
  - Vertical List Spacing: `12px` between cards

---

## 5. Scope Boundaries (MVP vs Excluded)

### 5.1 Strictly INCLUDED in MVP Scope (Student Level)
1. **Authentication (EF001, EF002):**
   - Login page matching Figma mockup 1 (Email, Password, "SE CONNECTER").
   - Storage of JWT token in MAUI `SecureStorage`.
   - Token refresh mechanism (`/auth/refresh`).
2. **App Shell & 5-Tab Navigation:**
   - AppShell with 5 tabs: `Catalogue`, `Marché`, `Ordres`, `Garage`, `Activité`.
   - Custom tab bar styling with active red indicator.
3. **Vehicle Catalog (EF010, EF011, EF012, EF020):**
   - Browsing cards from API (`GET /cards`).
   - Dynamic search by brand and model.
   - Filtering by rarity and brand.
   - Detail view with vehicle specifications, rarity, official price, and circulation count.
4. **Marketplace & Trading (EF030, EF031, RM001–RM006):**
   - Active listings feed matching mockup 4.
   - "ACHETER IMMÉDIATEMENT" with confirmation dialog and idempotency key (`RM006`).
   - "RÉSERVER" action with expiration countdown (`RM004`).
   - "Vendre une carte" via Floating Action Button (`+`) (`EF031`).
5. **Garage & Portfolio (EF050, EF051):**
   - Owned cars list (`GET /portfolio`) with quantity badge (`x2`).
   - Total portfolio valuation and P&L color-coded calculation.
6. **Booster Pack Opening (Mockup 2 & 3):**
   - Booster modal in Garage with quota indicator (e.g. 3/10) and cooldown timer.
   - Interactive 5-card reveal flow with "RETOURNER ▶" action.
7. **Data Expansion (`cars_database.json`):**
   - Separate rich JSON database file formatted to match API card schema with real web image URLs.
8. **Unit Testing & Architecture:**
   - Single `ServiceAPI.cs` in `Services/` using `Newtonsoft.Json`.
   - Unit test suite verifying authentication and catalog data fetching.

### 5.2 Strictly EXCLUDED from MVP Scope
1. **SignalR / WebSocket Real-Time Push:** The UI displays the static visual indicator "🟢 EN DIRECT", but does NOT implement a real-time SignalR hub connection. Data updates occur via REST polling or on-demand pull-to-refresh.
2. **Automated Trading & Conditional Algorithmic Orders:** Complex limit orders, stop-loss triggers, or automated trading bots are omitted.
3. **User Self-Registration & Password Reset Backend Flow:** The UI includes the visual links ("Créer un compte", "Mot de passe oublié ?") for fidelity with Figma mockup 1, but they display informational dialogs since accounts are pre-provisioned for BTS students.
4. **Real Currency Payment Gateways:** All financial operations use in-app virtual "coins".

---

## 6. Business Rules & Architecture Constraints

### 6.1 Business Rules Matrix (Cahier des charges §4.2)
- **RM001 (Official Valuation Authority):** The server is the sole source of truth for card values. The mobile client must display the latest received price along with its timestamp.
- **RM002 (Server-Calculated Rarity):** Card rarity (COMMUNE, RARE, ÉPIQUE, LÉGENDAIRE) is determined by the server based on circulation volume.
- **RM003 (Operation Exclusivity):** A card currently listed for sale, reserved, or being gifted cannot simultaneously enter another conflicting transaction.
- **RM004 (Reservation Expiration):** Every reservation carries an expiration datetime after which the lock is automatically released.
- **RM005 (Transaction Finality):** A transaction is considered successful only when the server returns a 200/201 response containing a final status and transaction ID.
- **RM006 (Idempotence Safeguard):** To prevent duplicate orders during network lag or repeated user clicks, the client generates and sends a unique `Idempotency-Key` (UUIDv4) header in purchase requests.

### 6.2 Architecture Rules (Cahier des charges §13 & ORIGINAL_REQUEST)
1. **Single Service File Constraint:** The `Services/` directory must contain only one file named `ServiceAPI.cs` (or `IServiceAPI.cs` interface co-located or single class) containing all REST client logic.
2. **MVVM Pattern:** ViewModels inherit from `ObservableObject` (or `INotifyPropertyChanged`). Views bind to ViewModel properties and `ICommand`s.
3. **Newtonsoft.Json Exclusivity:** All JSON serialization and deserialization must use `Newtonsoft.Json` (`JsonConvert`).
4. **Visual Studio 2026 Solution:** Solution file must use the modern `.slnx` format.

---

## 7. Data Expansion Specification (`cars_database.json`)

To satisfy requirement **R3**, a standalone `cars_database.json` file must be generated with realistic car data matching the API schema:

```json
[
  {
    "id": 101,
    "brand": "Ferrari",
    "model": "LaFerrari Aperta",
    "variant": "Aperta",
    "year": 2017,
    "powerHp": 963,
    "rarity": "LÉGENDAIRE",
    "copiesInCirculation": 210,
    "officialPrice": 480000,
    "imageUrl": "https://images.unsplash.com/photo-1583121274602-3e2820c69888?auto=format&fit=crop&w=1200&q=80",
    "description": "Hypercar hybride V12 à toit ouvert combinant un moteur atmosphérique de 6,3 litres et le système HY-KERS.",
    "topSpeedKmH": 350,
    "zeroToHundred": 2.9
  },
  {
    "id": 102,
    "brand": "Koenigsegg",
    "model": "Jesko Absolut",
    "variant": "Absolut",
    "year": 2022,
    "powerHp": 1600,
    "rarity": "LÉGENDAIRE",
    "copiesInCirculation": 125,
    "officialPrice": 2650000,
    "imageUrl": "https://images.unsplash.com/photo-1617814076367-b759c7d7e738?auto=format&fit=crop&w=1200&q=80",
    "description": "La voiture la plus rapide jamais conçue par Koenigsegg avec un coefficient de traînée réduit à 0,278.",
    "topSpeedKmH": 531,
    "zeroToHundred": 2.5
  },
  {
    "id": 103,
    "brand": "Lamborghini",
    "model": "Huracán STO",
    "variant": "Super Trofeo Omologata",
    "year": 2022,
    "powerHp": 640,
    "rarity": "ÉPIQUE",
    "copiesInCirculation": 700,
    "officialPrice": 318000,
    "imageUrl": "https://images.unsplash.com/photo-1544829099-b9a0c07fad1a?auto=format&fit=crop&w=1200&q=80",
    "description": "Supercar homologuée pour la route inspirée de la compétition Squadra Corse avec aérodynamisme poussé.",
    "topSpeedKmH": 310,
    "zeroToHundred": 3.0
  },
  {
    "id": 104,
    "brand": "Porsche",
    "model": "911 GT3 RS",
    "variant": "992 GT3 RS",
    "year": 2023,
    "powerHp": 525,
    "rarity": "ÉPIQUE",
    "copiesInCirculation": 1200,
    "officialPrice": 223000,
    "imageUrl": "https://images.unsplash.com/photo-1614162692292-7ac56d7f7f1e?auto=format&fit=crop&w=1200&q=80",
    "description": "Athlète haute performance conçue pour les circuits avec aérodynamique active DRS et châssis affûté.",
    "topSpeedKmH": 296,
    "zeroToHundred": 3.2
  },
  {
    "id": 105,
    "brand": "Nissan",
    "model": "GT-R NISMO",
    "variant": "NISMO Special Edition",
    "year": 2024,
    "powerHp": 600,
    "rarity": "ÉPIQUE",
    "copiesInCirculation": 1500,
    "officialPrice": 141000,
    "imageUrl": "https://images.unsplash.com/photo-1617654112368-307921291f42?auto=format&fit=crop&w=1200&q=80",
    "description": "L'ultime évolution de Godzilla équipée de turbocompresseurs de spécification GT3 et d'un capot carbone.",
    "topSpeedKmH": 330,
    "zeroToHundred": 2.7
  },
  {
    "id": 106,
    "brand": "Renault",
    "model": "Mégane R.S. Ultime",
    "variant": "Ultime",
    "year": 2023,
    "powerHp": 300,
    "rarity": "RARE",
    "copiesInCirculation": 1976,
    "officialPrice": 44000,
    "imageUrl": "https://images.unsplash.com/photo-1629897048514-3dd7414fe72a?auto=format&fit=crop&w=1200&q=80",
    "description": "Série limitée marquant la fin de la lignée Renault Sport avec châssis Cup et 4CONTROL.",
    "topSpeedKmH": 260,
    "zeroToHundred": 5.7
  },
  {
    "id": 107,
    "brand": "Toyota",
    "model": "GR Yaris",
    "variant": "Rally Edition / Track Pack",
    "year": 2023,
    "powerHp": 261,
    "rarity": "RARE",
    "copiesInCirculation": 3500,
    "officialPrice": 47000,
    "imageUrl": "https://images.unsplash.com/photo-1621135802920-133df287f89c?auto=format&fit=crop&w=1200&q=80",
    "description": "Homologation WRC dotée d'une transmission intégrale GR-FOUR permanente et d'un 3-cylindres turbo rageur.",
    "topSpeedKmH": 230,
    "zeroToHundred": 5.2
  },
  {
    "id": 108,
    "brand": "Lotus",
    "model": "Emira",
    "variant": "V6 First Edition",
    "year": 2022,
    "powerHp": 405,
    "rarity": "ÉPIQUE",
    "copiesInCirculation": 2500,
    "officialPrice": 89000,
    "imageUrl": "https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=1200&q=80",
    "description": "Dernière sportive à moteur thermique de Lotus, dotée d'un V6 3,5L compressé et d'une boîte manuelle.",
    "topSpeedKmH": 290,
    "zeroToHundred": 4.3
  },
  {
    "id": 109,
    "brand": "Mazda",
    "model": "MX-5",
    "variant": "RF PRHT",
    "year": 2021,
    "powerHp": 184,
    "rarity": "RARE",
    "copiesInCirculation": 8000,
    "officialPrice": 32000,
    "imageUrl": "https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80",
    "description": "Roadster légendaire avec toit rigide rétractable Fastback, propulsion agile et équilibre 50/50.",
    "topSpeedKmH": 220,
    "zeroToHundred": 6.8
  },
  {
    "id": 110,
    "brand": "Bugatti",
    "model": "Chiron Super Sport 300+",
    "variant": "300+",
    "year": 2021,
    "powerHp": 1600,
    "rarity": "LÉGENDAIRE",
    "copiesInCirculation": 30,
    "officialPrice": 3800000,
    "imageUrl": "https://images.unsplash.com/photo-1542282088-72c9c27ed0cd?auto=format&fit=crop&w=1200&q=80",
    "description": "L'hypercar W16 quadri-turbo qui a franchi la barre mythique des 300 mph (490,48 km/h).",
    "topSpeedKmH": 490,
    "zeroToHundred": 2.4
  }
]
```

---

## Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | AUTH | User Authentication | Logs in student user via email & password | Email string, password string | JWT Access token, Refresh token, User profile | Displays error alert: "Identifiants invalides" | Mockup 1 & Cahier §6 EF001 |
| 2 | AUTH | Secure Token Storage | Stores JWT session token securely in platform keystore | JWT token string | Encrypted storage entry | Logs failure, falls back to in-memory session | Cahier §6 EF002 |
| 3 | AUTH | Token Refresh | Silently renews expired access token using refresh token | Refresh token string | New access token & expiry | Prompts user to log in again on failure | Cahier Annexe A `POST /auth/refresh` |
| 4 | AUTH | Forgot Password Link | User requests password recovery | Tap on "Mot de passe oublié ?" | Navigation or informational dialog | Shows alert "Fonctionnalité désactivée en mode examen" | Mockup 1 |
| 5 | AUTH | Create Account Link | Navigates to student registration | Tap on "Créer un compte" | Navigation or dialog | Shows alert "Comptes fournis par l'enseignant" | Mockup 1 |
| 6 | NAV | 5-Tab AppShell Navigation | Persistent bottom navigation bar with 5 primary views | Tab tap (Catalogue, Marché, Ordres, Garage, Activité) | Displays corresponding page, updates active indicator | Remains on current tab if navigation fails | Mockups 2, 3, 4 |
| 7 | NAV | Active Tab Highlighting | Visual feedback for selected tab | Active route binding | Red underline bar and highlighted tab icon/text | N/A | Mockups 2 & 4 |
| 8 | CAT | Catalog Browsing | Displays paginated list/grid of available automotive cards | `GET /cards?page=1&limit=20` | Observable collection of card DTOs | Displays "Impossible de charger le catalogue" + retry | Cahier §6 EF010 |
| 9 | CAT | Full-Text Card Search | Filters catalog cards in real-time by brand or model name | Text query string (e.g. "Porsche") | Filtered card collection | Displays "Aucun véhicule correspondant trouvé" | Cahier §6 EF011 |
| 10 | CAT | Multi-Criteria Filtering | Filters catalog by rarity and brand | Selected rarity enum, brand string | Refined card subset | Resets filter on clear | Cahier §6 EF012 |
| 11 | CAT | Vehicle Detailed Sheet | Displays complete card specs, history, and copies in circulation | Card ID (`GET /cards/{id}`) | Full car view (power, year, price, rarity, copies) | Shows "Fiche indisponible" error modal | Cahier §6 EF020 |
| 12 | CAT | Official Price & Timestamp | Displays server-authoritative valuation and timestamp | Server card object | Formatted price string with update time | Displays last cached price with warning indicator | Cahier §4.2 RM001 |
| 13 | CAT | Card Rarity Badging | Color-coded pill badge (COMMUNE, RARE, ÉPIQUE, LÉGENDAIRE) | Server rarity string | Styled border, background tint, and star icon | Defaults to silver "COMMUNE" style | Cahier §4.2 RM002 & Mockup 4 |
| 14 | MKT | Active Market Listings | Displays live sell offers with car thumbnail, seller, and price | `GET /offers` or `GET /cards/{id}/offers` | List of active offer cards | Displays empty state "Aucune offre active" | Mockup 4 & Cahier §6 EF030 |
| 15 | MKT | Live Indicator Badge | Displays visual status badge "🟢 EN DIRECT" | System connection state | Green dot + text "EN DIRECT" | Switches to amber/offline badge if disconnected | Mockup 4 |
| 16 | MKT | Immediate Card Purchase | Buys a listed card instantly for asking coin price | Tap "ACHETER IMMÉDIATEMENT" + confirm | `POST /orders/buy`, updates user balance & garage | Displays error: "Fonds insuffisants" or "Offre expirée" | Mockup 4 & Cahier §6 EF030 |
| 17 | MKT | Purchase Recap Confirmation | Modal dialog summarizing car name, price, and balance before buying | Selected offer object | Visual confirmation prompt with Buy / Cancel buttons | Cancellation safely dismisses modal | Cahier §6 EF030 |
| 18 | MKT | Card Reservation | Locks an offer for exclusive purchase within a time window | Tap "RÉSERVER" | Server reservation ticket + expiration timer | Displays error if card already reserved by another user | Mockup 4 & Cahier §4.2 RM004 |
| 19 | MKT | Reservation Expiration Timer | Releases locked reservation when deadline expires | Expiration timestamp | Card returned to public market, button re-enabled | N/A | Cahier §4.2 RM004 |
| 20 | MKT | Sell Offer Creation (FAB) | Floating action button (`+`) to put an owned car up for sale | Tap red FAB `+`, select car, input asking price | `POST /offers`, updates market list | Displays error: "Carte déjà engagée" if reserved/listed | Mockup 4 & Cahier §6 EF031 |
| 21 | MKT | Purchase Idempotency Key | Generates UUIDv4 header to prevent double purchases on lag | Internal UUID header per transaction | Single execution guarantee on server | Rejects redundant request without double deduction | Cahier §4.2 RM006 |
| 22 | GAR | Total Portfolio Valuation | Header metric calculating sum of owned cards' current value | `GET /portfolio` | Large numeric display "482 500 coins" | Shows "0 coins" if empty portfolio | Mockup 2 & Cahier §6 EF051 |
| 23 | GAR | Portfolio Net P&L Display | Calculates and displays net profit or loss since purchase | Current value minus purchase cost | Green "+32 500 € P&L total" or red "-X €" | Correct neutral styling if P&L == 0 | Mockup 2 |
| 24 | GAR | Owned Cars Collection List | Displays all owned vehicle cards with specs and valuation | Portfolio card items | Vertical list of owned car cards | Shows "Votre garage est vide. Ouvrez un booster !" | Mockup 2 & Cahier §6 EF050 |
| 25 | GAR | Multi-Copy Multiplier Badge | Displays badge over thumbnail when multiple copies are owned | Card count integer (e.g. 2) | Small red badge "x2" anchored to thumbnail | Hidden if quantity == 1 | Mockups 2 & 3 |
| 26 | GAR | Card Context Menu | Three-dot menu (`⋮`) on garage card item for actions | Tap on `⋮` | Action sheet: "Vendre", "Voir détails", "Offrir" | Closes menu on background tap | Mockup 2 |
| 27 | GAR | Card Gifting / Transfer | Transfers an eligible car to another member | Recipient student ID, card ID | Transfers car, updates garage | Rejects transfer if card is locked in sale | Cahier §3 (Donateur role) |
| 28 | BOOST| Booster Quota Counter | Displays available booster packs out of daily maximum | Server booster quota | Visual progress dots + text "3/10 disponibles" | Disables open button when 0 boosters available | Mockup 2 |
| 29 | BOOST| Booster Refill Countdown | Shows time remaining until next free booster replenishment | Next replenishment timestamp | Dynamic countdown timer "Prochain dans 09:55" | Resets countdown and increments quota at 00:00 | Mockup 2 |
| 30 | BOOST| Booster Opening Ceremony | Initiates the opening of a 5-card booster pack | Tap "⚡ OUVRIR" | Modal overlay with 5-card progression | Shows error if daily quota exhausted | Mockups 2 & 3 |
| 31 | BOOST| 5-Card Progress Tracking | Visual indicator tracking card reveal progress | Active card index (1 to 5) | Segmented progress bar + "Carte 1 sur 5" | Reaches completion after card 5 | Mockup 3 |
| 32 | BOOST| Card Flip Action | Flips the active card face-up to reveal vehicle and rarity | Tap "RETOURNER ▶" | Flip animation, shows vehicle photo & stats | Button disabled during flip animation | Mockup 3 |
| 33 | DATA | External Vehicle Database | Standalone JSON catalog with real automotive specs and images | `cars_database.json` file | Seed data / test database matching API schema | Validates JSON format on load | ORIGINAL_REQUEST R3 |
| 34 | ARCH | Centralized ServiceAPI | Single service encapsulation for all HTTP API interactions | `ServiceAPI.cs` methods | Strongly-typed DTOs via Newtonsoft.Json | Throws structured `ApiException` with HTTP status | ORIGINAL_REQUEST R1 & R2 |
| 35 | ARCH | MVVM Command Binding | Decoupled UI interactions via `IAsyncRelayCommand` | UI taps / inputs | Executes ViewModel logic without code-behind | Disables button while command `IsRunning` | ORIGINAL_REQUEST R1 |

---

## Edge Cases

| # | Feature | Input | Observed Behavior |
|---|---------|-------|-------------------|
| 1 | Authentication | Valid email, incorrect password | API returns 401 Unauthorized. UI displays "Identifiants incorrects" alert; password field is cleared while email is preserved. |
| 2 | Authentication | Network unreachable / server offline | Request times out. UI catches `HttpRequestException` and displays "Impossible de joindre le serveur. Vérifiez votre connexion." |
| 3 | Token Lifecycle | Access token expires during app use | On first 401, `ServiceAPI` interceptor invokes `POST /auth/refresh`. If refresh succeeds, original request is retried seamlessly. If refresh fails, user is redirected to Login. |
| 4 | Catalog Search | Search string with no matches (e.g. "Zzzzz") | Collection is empty. UI displays centered empty state illustration and text: "Aucun véhicule ne correspond à votre recherche." |
| 5 | Catalog Search | Rapid typing in search bar | Debounce delay (300ms) prevents firing API requests on every single keystroke. |
| 6 | Market Purchase | User has fewer coins than the asking price | Buy button triggers pre-check: modal alert informs "Fonds insuffisants : vous possédez 50 000 coins mais ce véhicule coûte 480 000 coins." Request is not sent. |
| 7 | Market Purchase | Rapid double-tap on "ACHETER IMMÉDIATEMENT" | Button disables immediately via `IsBusy` / `CanExecute` flag. Both attempts carry the same `Idempotency-Key` header (`RM006`), ensuring at most one debit. |
| 8 | Market Purchase | Another user purchased the car moments earlier | Server returns 409 Conflict / 404 Not Found. UI alerts "Cette offre n'est plus disponible" and automatically removes the card from the active market feed. |
| 9 | Market Reservation | Reservation time expires (00:00) | Client-side timer triggers UI refresh; the "RÉSERVER" button switches back to active or the reservation badge expires per `RM004`. |
| 10 | Market Selling | Putting a car already listed or reserved on sale | Validation check (`RM003`) prevents listing. Error message: "Ce véhicule est déjà engagé dans une transaction en cours." |
| 11 | Portfolio / Garage | User owns zero cars (brand new account) | Collection list is empty. UI displays friendly placeholder card: "Votre garage est vide ! Ouvrez un booster pour obtenir vos premières cartes." |
| 12 | Portfolio / Garage | User owns multiple identical cars | Item appears as a single row in garage with quantity multiplier badge (e.g. `x2`, `x3`) instead of cluttering the list with duplicate entries. |
| 13 | Portfolio / Garage | Vehicle current market price is lower than purchase cost | Negative P&L is formatted with minus sign and styled in warning red (e.g., `-2 000 €`). |
| 14 | Booster Opening | Quota reaches 0/10 available | "⚡ OUVRIR" button is disabled (grayed out). Subtitle shows countdown timer until next replenishment. |
| 15 | Booster Opening | User kills/closes the app mid-booster reveal (e.g. at Card 2/5) | Server atomically grants all 5 cards at the initial "OUVRIR" request (`POST /boosters/open`). Upon relaunch, all 5 cards are safely present in the user's garage. |
| 16 | Vehicle Images | Real vehicle image URL fails to load (404 / timeout) | MAUI `Image` component triggers `SourceFallback`, displaying a high-resolution dark placeholder with the AutoMasters stylized car logo. |
| 17 | Extreme Numeric Values | Total portfolio value exceeds 1,000,000 coins | Value is formatted with culture-aware thousand separators (e.g., `2 650 000 coins`) and auto-scales font size to prevent layout clipping. |

---

## 8. Verification & Delivery Confirmation

This mining report has been synthesized from authoritative specifications and visual mockups. It serves as the baseline for:
- Repository structure matching `TestVoiture.git` (tracked by `explorer_survey_repo`).
- API endpoint integration and DTO schemas (tracked by `spec_miner_survey_api`).
- Implementation plan and test execution (orchestrated by `orchestrator_1`).
