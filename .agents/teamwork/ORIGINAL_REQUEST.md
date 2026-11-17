# Original User Request

## 2026-10-07T16:19:56Z

# Teamwork Project Prompt — Draft

> Status: Launched
> Goal: Craft prompt → get user approval → delegate to teamwork_preview
> Requested team: [none — teamwork routes from the description]

*Reference Material:*
- Cahier des charges: `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\cahier_des_charges.md`
- Figma mockups (images): 
  - `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791389223586_1f733aab.png`
  - `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791389263369_a72b0e54.png`
  - `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791389277036_c3cce1e8.png`
  - `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791389292458_a07f6e9a.png`

Build a .NET MAUI mobile application (MVVM architecture, using Newtonsoft.Json) for collecting and trading digital car cards, consuming a provided REST API (Group 2 API) and matching a specific GitHub repository structure. The project should be at the level of a 2nd-year BTS SIO student.

Working directory: c:\Users\stent\source\repos\AutoMasters
Integrity mode: benchmark

## Requirements

### R1. Base Application & Architecture
Create a .NET MAUI solution (with `.slnx` for VS 2026). Organize folders matching the reference GitHub repository (https://github.com/Thierrysio/TestVoiture.git), using a single `ServiceAPI` file in the services folder. Use MVVM pattern (ObservableObject, etc.) and Newtonsoft.Json for serialization.

### R2. Core MVP Features (Student Level)
Implement the core features from the specifications (Authentication, Catalog, Market/Trading, Garage/Portfolio). Focus on the core functional flow matching the Figma mockups (e.g., login only). Connect to the Group 2 API (https://api-cartes-g2.fldsio.com) using the provided credentials. Exclude complex features like SignalR real-time updates and automated orders.

### R3. Data Expansion
Create a separate JSON document (`cars_database.json`) containing new car data (matching the API format: id, brand, model, etc.) and including URLs to real car images found on the web.

## Acceptance Criteria

### Compilation & Architecture
- [ ] The solution (`.slnx`) compiles successfully with `dotnet build`.
- [ ] The project structure precisely matches the provided GitHub repository (`TestVoiture.git`).
- [ ] The `services` folder contains only one file named `ServiceAPI`.
- [ ] The code strictly follows the MVVM pattern (no business logic in code-behind).

### API & Data
- [ ] The `ServiceAPI` authenticates correctly with the Group 2 API (https://api-cartes-g2.fldsio.com).
- [ ] Unit tests for `ServiceAPI` pass and verify basic data retrieval (using `Newtonsoft.Json`).

### Data Expansion
- [ ] `cars_database.json` exists and contains correctly formatted JSON entries for cars, including real image URLs.

## 2026-10-09T14:25:15Z

The quota issue has been resolved and the server has restarted. Please resume the project orchestration exactly where you left off (Iteration 19 - applying the 3 test fixes and completing Milestone 2).

## 2026-10-10T16:46:04Z

The quota issue has been resolved and the server has restarted again. Please resume the project orchestration exactly where you left off (Iteration 9 of the post-restart, addressing the UX edge cases for Milestone 4).


## 2026-10-10T18:46:21Z

# Teamwork Project Prompt — Draft

> Status: Launched
> Goal: Craft prompt → get user approval → delegate to teamwork_preview
> Requested team: [none — teamwork routes from the description]

*Reference Material:*
- Cahier des charges: `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\cahier_des_charges.md`
- New Reference Card UI (image): `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791657264584_e9c97e4d.png`

Refine and upgrade the existing AutoMasters .NET MAUI mobile application. The goal is to significantly improve visual quality, fix bugs (boosters, duplicated sections), and drastically expand the car database with accurate images.

Working directory: c:\Users\stent\source\repos\AutoMasters

Integrity mode: development

## Requirements

### R1. Premium UI/UX & Dynamic Cards
Redesign the application's UI to be premium and highly polished using standard .NET MAUI controls (shadows, gradients, standard animations) to keep the code understandable for a student. Cards must have distinct visuals, backgrounds, and borders based on their rarity, inspired by but distinct from the provided reference image.

### R2. Bug Fixes & Refactoring
- Fix the booster opening mechanic so it works reliably.
- Investigate and fix the "Ordre" (Orders) section, which appears to be a duplicate of another section. 
- Ensure all base prices defined in the JSON database are realistic for luxury/hyper cars (e.g., hundreds of thousands to millions).

### R3. Massive Data Expansion
Use the `browser` subagent to find realistic, accurate images for a massive expansion of the `cars_database.json` (aiming for 50-100 high-quality entries). The images must strictly match the specific brand and model of the car.

## Acceptance Criteria

### UI & Architecture
- [ ] Cards display dynamic visual styles (colors/gradients) corresponding to their rarity.
- [ ] UI looks polished and matches/exceeds the Figma mockups using standard MAUI components.
- [ ] The code remains clean, understandable, and preserves the MVVM architecture and existing tests.

### Bug Fixes
- [ ] Opening a booster works without errors or freezing.
- [ ] The "Ordres" page has its own distinct functionality and UI, not duplicated from elsewhere.

### Data Expansion
- [ ] `cars_database.json` contains at least 50 verified car entries.
- [ ] Car base prices reflect real-world luxury car values.
- [ ] Image URLs point to correct images of the exact car model.
