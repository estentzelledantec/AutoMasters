# Handoff Report — spec_miner_survey_specs

**Agent:** spec_miner_survey_specs  
**Working Directory:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_specs`  
**Milestone:** M0_Specification_Mining  
**Date:** 2026-10-07T16:31:00Z  

---

## 1. Observation

Direct observations from authoritative project sources:
1. **ORIGINAL_REQUEST.md (`c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\ORIGINAL_REQUEST.md`):**
   - Lines 19-20: *"Build a .NET MAUI mobile application (MVVM architecture, using Newtonsoft.Json) for collecting and trading digital car cards, consuming a provided REST API (Group 2 API) and matching a specific GitHub repository structure. The project should be at the level of a 2nd-year BTS SIO student."*
   - Lines 26-27: *"Create a .NET MAUI solution (with `.slnx` for VS 2026). Organize folders matching the reference GitHub repository (https://github.com/Thierrysio/TestVoiture.git), using a single `ServiceAPI` file in the services folder. Use MVVM pattern (ObservableObject, etc.) and Newtonsoft.Json for serialization."*
   - Lines 30-31: *"Implement the core features from the specifications (Authentication, Catalog, Market/Trading, Garage/Portfolio). Focus on the core functional flow matching the Figma mockups (e.g., login only). Connect to the Group 2 API (https://api-cartes-g2.fldsio.com) using the provided credentials. Exclude complex features like SignalR real-time updates and automated orders."*
   - Lines 32-33: *"Create a separate JSON document (`cars_database.json`) containing new car data (matching the API format: id, brand, model, etc.) and including URLs to real car images found on the web."*

2. **Cahier des charges (`C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\cahier_des_charges.md`):**
   - Section 3: Defines roles: *Collectionneur, Vendeur, Acheteur, Donateur*.
   - Section 4.2: Defines mandatory rules:
     - `RM001`: *"Le serveur est l'unique source du cours officiel. Le client affiche la dernière valeur reçue avec son horodatage."*
     - `RM002`: *"La rareté est fournie par le serveur et tient compte du nombre d'exemplaires en circulation."*
     - `RM003`: *"Une carte vendue, réservée ou offerte ne peut pas être engagée simultanément dans une autre opération incompatible."*
     - `RM004`: *"Une réservation possède une date d'expiration."*
     - `RM005`: *"Une transaction n'est réussie que lorsque le serveur renvoie un statut final positif et un identifiant de transaction."*
     - `RM006`: *"Une demande répétée à la suite d'un délai réseau ne doit pas créer une double transaction. Le client utilise la clé d'idempotence prévue par l'API."*
   - Section 6: Functional requirements:
     - `EF001`: *"Permettre la connexion avec les identifiants attendus par l'API."*
     - `EF002`: *"Conserver les jetons uniquement dans le stockage sécurisé de la plateforme."*
     - `EF010`: *"Afficher les cartes disponibles sous forme de liste ou grille avec image, nom, rareté et cours."*
     - `EF011`: *"Rechercher par texte sur la marque, le modèle ou les champs prévus par l'API."*
     - `EF012`: *"Filtrer au minimum par marque, modèle et rareté."*
     - `EF020`: *"Présenter la marque, le modèle, la variante, l'année, la rareté, le nombre en circulation et l'image."*
     - `EF030`: *"Acheter une offre disponible après affichage d'un récapitulatif."*
     - `EF031`: *"Créer une offre de vente pour un exemplaire possédé et éligible."*
     - `EF050`: *"Afficher toutes les cartes possédées et leur quantité ou leurs exemplaires."*
     - `EF051`: *"Afficher la valeur actuelle totale du portefeuille."*
   - Annexe A: Minimal API Contract: `POST /auth/login`, `POST /auth/refresh`, `GET /cards`, `GET /cards/{id}`, `GET /cards/{id}/offers`, `POST /orders/buy`, `POST /offers`, `GET /portfolio`.

3. **Figma Mockup Screenshots:**
   - Image 1 (`media_1791389223586_1f733aab.png`): Login page with logo, "AUTO" (white) + "MASTERS" (red), subtitle "Collecte · Échange · Compétition", inputs "ADRESSE EMAIL", "MOT DE PASSE", helper "Mot de passe oublié ?", red button "SE CONNECTER", footer "Pas encore de compte ? Créer un compte".
   - Image 2 (`media_1791389263369_a72b0e54.png`): Garage screen header with "VALEUR TOTALE DE LA COLLECTION: 482 500 coins", "+32 500 € P&L total", collection items with multiplier "x2", plus Booster dialog overlay showing "BOOSTER AUTOMASTERS", "3/10 disponibles", "Prochain dans 09:55", red button "⚡ OUVRIR". Bottom TabBar: CATALOGUE, MARCHÉ, ORDRES, GARAGE, ACTIVITÉ.
   - Image 3 (`media_1791389277036_c3cce1e8.png`): Booster card reveal ceremony with 5-step progress bar, "Carte 1 sur 5", card back with car logo, red button "RETOURNER ▶".
   - Image 4 (`media_1791389292458_a07f6e9a.png`): Marché screen with "🟢 EN DIRECT", "8 offres actives", offer cards with brand, model, year, price in coins, seller name, rarity badges (★ LÉGENDAIRE in gold, ÉPIQUE in purple), dual action buttons ("ACHETER IMMÉDIATEMENT" in green, "RÉSERVER" in dark red), red Floating Action Button (`+`).

---

## 2. Logic Chain

1. **Academic Alignment:** The prompt requires calibrating for a 2nd-year BTS SIO SLAM student. Based on national syllabus guidelines and R1/R2 in ORIGINAL_REQUEST, this requires strict MVVM separation (Views in XAML, ViewModels handling commands and data binding, Models as DTOs, and a centralized `ServiceAPI.cs` HTTP client using `Newtonsoft.Json`).
2. **UI Fidelity to Figma Mockups:** The UI must adhere to the design system extracted from the 4 mockup images:
   - Deep charcoal/black background (`#0D0D0E`), card background (`#1A1A1E`), primary red CTA (`#E63946`), green purchase CTA (`#10B981`), dark red reserve button (`#2D1518` / `#EF4444`).
   - 5-tab AppShell navigation structure (`Catalogue`, `Marché`, `Ordres`, `Garage`, `Activité`).
   - The Booster Pack reveal mechanism is fully defined in Mockups 2 and 3, connecting garage assets with card reveals.
3. **Scope Clarification:**
   - Included: Login with SecureStorage token saving, Catalog (search, filter, card details), Marketplace (active listings, instant buy, reservation, sell listing via FAB), Garage (owned cards, total coins valuation, P&L, booster modal), Data expansion (`cars_database.json`).
   - Excluded: SignalR push (the "🟢 EN DIRECT" badge is visual/polling only), automated algorithmic trading, and backend registration/password reset.
4. **Data Contract Compliance:** Data in `cars_database.json` matches the fields required by `EF020` and the API schema: `id`, `brand`, `model`, `variant`, `year`, `powerHp`, `rarity`, `copiesInCirculation`, `officialPrice`, `imageUrl`, and descriptions.

---

## 3. Caveats

1. **Pre-provisioned Student Accounts:** The Figma mockup displays "Créer un compte" and "Mot de passe oublié ?". Because BTS SIO testing uses pre-provisioned credentials, these UI actions should display friendly informational alerts rather than full registration workflows.
2. **Live Indicator Mechanism:** While Figma mockup 4 displays "🟢 EN DIRECT", the specifications explicitly exclude SignalR; therefore, live polling or standard REST refresh satisfies the requirement without WebSockets.
3. **Currency Display:** Mockups show both "coins" and "€" in certain metrics (e.g. "482 500 coins" with "+32 500 € P&L total"). The DTO and UI formatting support both unit formats cleanly.

---

## 4. Conclusion

The specification mining and survey is complete. All 35 core and discovered features have been enumerated, mapped to business rules (`RM001`–`RM006`) and functional requirements (`EF001`–`EF051`), and edge cases documented in `report.md`.
The requirements strictly bound the implementation to a clean, robust, and idiomatic .NET MAUI MVVM application matching BTS SIO academic expectations and visual parity with the Figma mockups.

---

## 5. Verification Method

To verify these specification findings:
1. Inspect `report.md`:
   `view_file c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\spec_miner_survey_specs\report.md`
2. Compare features against `cahier_des_charges.md` requirements:
   Verify coverage of EF001, EF002, EF010, EF011, EF012, EF020, EF030, EF031, EF050, EF051, RM001-RM006.
3. Visually cross-reference Figma mockups with Section 3 and Section 4 of `report.md` (colors, geometry, and component layout).
