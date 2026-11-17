# AutoMasters Phase 2 — Survey & Investigation Handoff Report

**Target Folder:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_survey_specs_tests`  
**Author:** Explorer Subagent (`explorer_survey_specs_tests`)  
**Timestamp:** 2026-10-10T20:57:30Z  
**Type:** Hard Handoff (Investigation & Survey Complete)  

---

## 1. Observation

1. **AppShell Routing Duplication:**
   - File: `c:\Users\stent\source\repos\AutoMasters\AutoMasters\AppShell.xaml`
   - Lines 51–56:
     ```xml
     <!-- Onglet 3: Ordres -->
     <Tab Title="Ordres" Route="Ordres">
         <ShellContent
             Title="Ordres"
             ContentTemplate="{DataTemplate vues:MarchePage}"
             Route="OrdresPage" />
     </Tab>
     ```
   - Lines 43–48 define Tab 2 ("Marché") with the exact same `ContentTemplate="{DataTemplate vues:MarchePage}"`. Tab 3 is a verbatim duplicate.
   - Lines 67–72 define Tab 5 ("Activité") with `ContentTemplate="{DataTemplate vues:GaragePage}"`, which is also a duplicate of Tab 4 ("Garage").

2. **Reference Card Mockup Image Analysis:**
   - Path: `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791657264584_e9c97e4d.png`
   - 5 cards displayed: Bugatti Chiron (UR), GMK (UR), Ferrari SF90 Stradale (SR), Rimac Nevera (SR), Toyota Yaris Cross (SR).
   - Features: Outer halo glowing borders, marble/wavy gradient backgrounds, pill badges ("UR", "SR") at top-left, bookmark stars at top-right, clean car hero photos, dark bold typography, and bottom stat row with crossed swords (⚔️) and blue shield (🛡️).

3. **Booster Pack Card Flip & Freeze Defect:**
   - File: `c:\Users\stent\source\repos\AutoMasters\AutoMasters\Vues\GaragePage.xaml`
   - Line 279:
     ```xml
     <VerticalStackLayout
         VerticalOptions="Center"
         Spacing="12"
         IsVisible="{Binding BoosterVueModele.EstRetournee, Converter={x:Null}, FallbackValue=True}">
     ```
   - Line 293:
     ```xml
     <VerticalStackLayout
         Spacing="8"
         IsVisible="{Binding BoosterVueModele.EstRetournee}">
     ```
   - File: `c:\Users\stent\source\repos\AutoMasters\AutoMasters\VuesModeles\BoosterVueModele.cs`
     - Line 38: `private bool estRetournee = false;`
     - Lines 154–157: `catch (Exception ex) { Message = ex.Message; }`
     - Line 170: `if (cartesTirees.Count == 0 || EstTermine) return;`
   - When unopened, `EstRetournee == false`, causing both face cachée and face révélée to evaluate `IsVisible = false` (blank card). When flipped, both become `true` and collide. When the network call fails or throws, `Message` is set silently with no UI display in `GaragePage.xaml`, and subsequent clicks hit `cartesTirees.Count == 0`, doing nothing and freezing the modal.

4. **Test Suite Baseline & Invariants:**
   - Executed: `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj`
     - Result: `Réussi! - échec : 0, réussite : 93, ignorée(s) : 1, total : 94, durée : 692 ms`
   - Executed: `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj`
     - Result: `Réussi! - échec : 0, réussite : 298, ignorée(s) : 2, total : 300, durée : 897 ms`
   - Invariant A (`ChallengerM4Phase2Tests.cs` lines 214–224): Enforces that car IDs in `cars_database.json` are strictly contiguous string integers from `"1"` to `array.Count`.
   - Invariant B (`ChallengerM4Phase2Tests.cs` lines 280–311): Enforces that car image URLs must start with `https://images.unsplash.com/`.

5. **`cars_database.json` State:**
   - Path: `c:\Users\stent\source\repos\AutoMasters\cars_database.json`
   - Currently contains 14 vehicle records (IDs 1 to 14).
   - Contains economy cars with prices below 100k (e.g. Clio 2 at 3,500 coins, 208 GTI at 24,000 coins, MX-5 at 38,000 coins).

---

## 2. Logic Chain

1. From **Observation 1**, Tab 3 in `AppShell.xaml` is proven to render `MarchePage` rather than a dedicated orders view. This accounts for the user's issue ("Ordre section appears to be a duplicate of another section"). Decoupling Tab 3 requires creating `OrdresPage.xaml` and `OrdresVueModele.cs`.
2. From **Observation 2**, the reference image demonstrates how digital trading card games evoke rarity through outer border glow, pill badges, and a distinct stats footer. In AutoMasters, mapping these concepts to automotive specifications (Puissance, Vitesse, 0-100, Cours) preserves the automotive domain identity while delivering premium aesthetic polish using standard MAUI `Border`, `Shadow`, and `LinearGradientBrush`.
3. From **Observation 3**, the booster sequence freezing and visual glitches are caused by the lack of an inverted boolean property/converter on the card back in XAML, the absence of an `ActivityIndicator` during API latency, and silent error swallowing that traps the user in an unresponsive modal. Introducing an explicit `EstFaceCachee` property, a loading indicator, and an inline error display directly solves the issue.
4. From **Observation 4**, expanding `cars_database.json` without maintaining contiguous string IDs `"1"` through `"50"` or using image URLs from sources other than `https://images.unsplash.com/` will cause regressions in `ChallengerM4Phase2Tests`. Maintaining these two rules guarantees 100% test pass rate.
5. From **Observation 5**, expanding `cars_database.json` from 14 to $\ge 50$ entries and setting realistic luxury valuations ($>100\text{k}$ coins) fulfills Requirement R3 and Acceptance Criteria without impacting existing functional tests.

---

## 3. Caveats

1. **Live Unsplash CDN Access:** In offline or sandboxed build environments, live HTTP network requests to `images.unsplash.com` will not resolve; however, `ChallengerM4Phase2Tests` explicitly catches network timeouts and only requires syntactic URL verification (`https://images.unsplash.com/`).
2. **SignalR / Real-Time Updates:** The specification excludes real-time WebSocket feeds; all order views should poll or refresh via manual `RefreshView` / `RelayCommand`.
3. **Tab 5 ("Activité"):** While Tab 3 ("Ordres") is the explicit focus of Phase 2, Tab 5 is also currently duplicating `GaragePage`. It should either remain untouched or be converted into a transaction history log if requested.

---

## 4. Conclusion

Phase 2 requirements are fully mapped and actionable:
1. **Ordres Feature:** Create `OrdresPage` and `OrdresVueModele` to display buy orders (`POST /orders/buy`), user listings (`POST /offers`), and reservations (`POST /offers/{id}/reservations`), rerouting Tab 3 in `AppShell.xaml`.
2. **Dynamic Cards:** Apply `Border` with `LinearGradientBrush`, `Shadow`, and rarity background tints, with an automotive micro-grid (⚡, 🏁, ⏱️, 🪙).
3. **Booster Pack Reliability:** Fix the card back visibility property, add an `ActivityIndicator`, and add inline error feedback.
4. **Data Expansion:** Expand `cars_database.json` to 50+ luxury entries with strictly contiguous IDs (`"1"` to `"50"`), prices $>100\text{k}$, and `https://images.unsplash.com/` URLs.

---

## 5. Verification Method

To verify these findings and safeguard against regressions:

1. **Run Unit & Challenger Tests:**
   ```powershell
   dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj
   ```
   *Expected:* 93 passed, 1 skipped, 0 failed.

2. **Run Full 4-Tier E2E Tests:**
   ```powershell
   dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj
   ```
   *Expected:* 298 passed, 2 skipped, 0 failed.

3. **Inspect Key Artifacts:**
   - Survey Report: `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_survey_specs_tests\report.md`
   - AppShell definition: `c:\Users\stent\source\repos\AutoMasters\AutoMasters\AppShell.xaml`
   - Card flip markup: `c:\Users\stent\source\repos\AutoMasters\AutoMasters\Vues\GaragePage.xaml`
   - Database invariants: `c:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ChallengerM4Phase2Tests.cs`

4. **Invalidation Conditions:**
   - Any failure among the 300 E2E or 94 unit tests.
   - Non-contiguous IDs in `cars_database.json`.
   - Non-Unsplash URLs in `cars_database.json`.
   - Direct `HttpClient` or `ServiceAPI` logic introduced into `.xaml.cs` code-behind files.
