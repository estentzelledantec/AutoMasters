# Group 2 REST API Specification & Data Model Survey Report

**Document Version:** 1.0.0  
**Author:** `spec_miner_survey_api`  
**Target Solution:** AutoMasters (.NET MAUI, MVVM, Newtonsoft.Json)  
**Academic Level:** BTS SIO SLAM 2ème année  
**API Target:** Group 2 REST API (`https://api-cartes-g2.fldsio.com`)  
**Date:** October 7, 2026  

---

## 1. Executive Summary & Specification Sources

This report provides the authoritative API specification survey, endpoint inventory, schema definitions, edge case behaviors, and unit testing requirements for integrating the **Group 2 REST API** (`https://api-cartes-g2.fldsio.com`) into the **AutoMasters** .NET MAUI application.

### 1.1 Authoritative Specification Sources
The API specifications and data contracts were extracted and cross-referenced from:
1. **Cahier des charges officiel (Version 1.0, 21 pages):**
   - File: `C:\Users\stent\.gemini\antigravity\brain\e5afb0e3-4565-43aa-8c58-ed90451981f3\.user_uploaded\media_1791388608301_f200d68a.pdf`
   - Key Sections: §4 (Règles Métier RM001–RM012), §6 (Exigences Fonctionnelles EF001–EF064), §7 (Intégration API), §8 (Données et Modélisation), Annexe A (Contrat API minimal), Annexe B (Scénarios de recette REC01–REC15).
2. **Student Reference Implementations (BTS SIO Course 531 & Labs):**
   - `c:\Users\stent\source\repos\AtelierCartes\AtelierCartes\Services\ServiceApi.cs` & `Modeles/`
   - `c:\Users\stent\source\repos\CartesAutomobilesConsole\CartesAutomobilesConsole\Services\ApiCartesService.cs` & `Modeles/`
   - `c:\Users\stent\source\repos\AP Semestre 1\AP Semestre 1\Services\ServiceApi.cs` & `Modeles/`
   - `c:\Users\stent\source\repos\kit-etudiant\GUIDE-ETUDIANT.md`
3. **Figma UI/UX Mockups:**
   - Screen 1: Login (`media_1791389223586_1f733aab.png`)
   - Screen 2: Garage & Booster Dialog (`media_1791389263369_a72b0e54.png`)
   - Screen 3: Booster Opening Flow (`media_1791389277036_c3cce1e8.png`)
   - Screen 4: Market / Trading Feed (`media_1791389292458_a07f6e9a.png`)
4. **Network Environment Note:**
   - Direct runtime external probing against `api-cartes-g2.fldsio.com` was restricted by security sandbox policies during exploration. The API contracts, payload models, and response structures documented below are rigorously cross-validated against the student reference codebases and the authoritative 21-page specification document.

---

## 2. API Infrastructure, Authentication & Security

### 2.1 Base Address & Protocol
- **Production / Group 2 Endpoint:** `https://api-cartes-g2.fldsio.com/`
- **Local Dev / Fallback Mock API:** `http://localhost:5088/api/` (Windows Machine) / `http://10.0.2.2:5088/api/` (Android Emulator)
- **Transport Security:** Strict HTTPS (TLS 1.2+).
- **Default Timeout:** 15–20 seconds (`TimeSpan.FromSeconds(15)`).

### 2.2 Authentication Flow & Token Format
- **Endpoint:** `POST /auth/login`
- **Content-Type:** `application/json; charset=utf-8`
- **Request Payload:**
  ```json
  {
    "username": "student_username_or_email",
    "password": "student_password"
  }
  ```
  *(In C# models: `DemandeConnexion` with properties `Identifiant` (`[JsonProperty("username")]`) and `MotDePasse` (`[JsonProperty("password")]`)).*
- **Response Payload (Status 200 OK):**
  ```json
  {
    "accessToken": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
  }
  ```
  *(In C# models: `ReponseConnexion` with property `JetonAcces` (`[JsonProperty("accessToken")]`)).*
- **Authorization Header:**
  All authenticated requests must include the JWT Bearer token:
  ```http
  Authorization: Bearer <accessToken>
  ```
- **Token Storage Requirement (EF002):**
  Tokens must NEVER be stored in ordinary preferences, plain text files, or log outputs.
  In .NET MAUI, tokens must be stored in `Microsoft.Maui.Storage.SecureStorage`:
  ```csharp
  await SecureStorage.Default.SetAsync("automasters_jwt", token);
  ```
- **Session Renewal (EF003 / Annexe A):**
  - Endpoint: `POST /auth/refresh`
  - When a request returns `401 Unauthorized`, `ServiceAPI` attempts a token refresh once before clearing credentials and redirecting to the login screen (`REC15`).
- **Logout (EF004):**
  - Clears the HTTP client authorization header (`_client.DefaultRequestHeaders.Authorization = null;`).
  - Removes the token from `SecureStorage` (`SecureStorage.Default.Remove("automasters_jwt");`).

---

## 3. Comprehensive API Endpoints Inventory

| Group | Method | Path | Description | Auth Required |
|---|---|---|---|---|
| **Auth** | `POST` | `/auth/login` | Authenticate user with credentials and issue JWT | No |
| **Auth** | `POST` | `/auth/refresh` | Renew an expired or expiring JWT session | Yes |
| **Cards** | `GET` | `/cards` | Search, filter, and paginate catalog of cards | Yes |
| **Cards** | `GET` | `/cards/{id}` | Retrieve individual card specification details | Yes |
| **Cards** | `GET` | `/cards/{id}/quotes` | Retrieve price quotes and historical valuations | Yes |
| **Cards** | `GET` | `/cards/{id}/offers` | List active marketplace offers for a specific card | Yes |
| **Market** | `POST` | `/orders/buy` | Immediate purchase of a market offer | Yes |
| **Market** | `POST` | `/offers` | Create a sell offer for an owned copy | Yes |
| **Market** | `PATCH` | `/offers/{id}` | Update asking price of an existing sell offer | Yes |
| **Market** | `DELETE` | `/offers/{id}` | Cancel/withdraw a sell offer from the market | Yes |
| **Market** | `POST` | `/offers/{id}/reservations` | Temporarily lock/reserve an offer | Yes |
| **Market** | `DELETE` | `/reservations/{id}` | Cancel an active reservation | Yes |
| **Garage** | `GET` | `/portfolio` | Retrieve owned cards collection, valuation, and P&L | Yes |
| **Activity**| `GET` | `/transactions` | Retrieve historical logs of purchases, sales, gifts | Yes |
| **Orders** | `POST` | `/conditional-orders` | Create an automated limit/stop order | Yes |
| **Orders** | `PATCH`| `/conditional-orders/{id}` | Modify conditions of an automated order | Yes |
| **Orders** | `DELETE`| `/conditional-orders/{id}`| Cancel an automated order | Yes |
| **Gifts**  | `POST` | `/gifts` | Transfer an eligible card to another user for free | Yes |
| **Alerts** | `GET` | `/notifications` | Retrieve unread/read user notifications | Yes |

---

## 4. Features Discovered

| # | Category | Feature | Description | Inputs | Outputs | Error Behavior | Discovered Via |
|---|----------|---------|-------------|--------|---------|----------------|----------------|
| 1 | Auth | User Login | Authenticate collector and retrieve access token | `username` (string), `password` (string) | `accessToken` (string JWT) | 400 Bad Request if missing fields; 401 Unauthorized if invalid credentials | Cahier des charges §6.1 (EF001), `AtelierCartes/ServiceApi.cs`, `CartesAutomobilesConsole` |
| 2 | Auth | Session Refresh | Renew JWT token without prompting user | `accessToken` or refresh token | New `accessToken` | 401 if refresh token is expired or revoked | Cahier des charges Annexe A, EF003 |
| 3 | Catalog | List Cards (Paginated) | Fetch card catalog with pagination | `page` (int, default 1), `pageSize` (int, default 20) | JSON envelope: `items` (List of Cards), `totalPages`, `total`, `page` | 401 Unauthorized; 400 on negative page | `CartesAutomobilesConsole/ApiCartesService.cs`, Cahier des charges Annexe A |
| 4 | Catalog | Filter Catalog | Search catalog by text and filter by brand/rarity | Query params: `search` (string), `brand` (string), `rarity` (string) | Filtered collection of Card objects | 200 with empty list if no matches | Cahier des charges EF011, EF012 |
| 5 | Catalog | Card Details | Fetch detailed specs of a single card | `id` (string / GUID) path parameter | Card detail object (specs, circulation, official price) | 404 Not Found if id does not exist | Cahier des charges EF020, Annexe A |
| 6 | Market | List Card Offers | Retrieve active sell offers for a specific card | `id` (string / GUID) path parameter | JSON envelope: `items` (List of Offer objects) | 404 if card doesn't exist; empty items list if no offers | `AtelierCartes/ServiceApi.cs`, Cahier des charges Annexe A |
| 7 | Market | Buy Offer Immediately | Execute instantaneous purchase of a listed card | Body: `offerId` (string); Header: `Idempotency-Key` (UUID) | Transaction confirmation object with transactionId and status | 400 Insufficient funds; 409 Offer already sold/reserved; 404 Not found | Cahier des charges EF030, RM005, RM006 |
| 8 | Market | Create Sell Offer | Put an owned card copy up for sale | Body: `copyId` (string), `price` (decimal > 0) | Created Offer object (`id`, `copyId`, `price`, `status`) | 400 Negative or zero price; 409 Copy already listed or reserved (RM003) | `AtelierCartes/Modeles/DemandeCreationOffre.cs`, EF031 |
| 9 | Market | Update Offer Price | Modify the asking price of an existing sell offer | Path: `id` (offerId); Body: `price` (decimal > 0) | Updated Offer object with revised price | 404 Offer not found; 400 Invalid price; 409 Offer already sold | `AtelierCartes/Modeles/DemandeModificationOffre.cs`, EF032 |
| 10 | Market | Delete Sell Offer | Remove an active offer from the marketplace | Path: `id` (offerId) | 204 No Content | 404 Offer not found; 409 Offer already sold or locked in transaction | `AtelierCartes/ServiceApi.cs`, EF032 |
| 11 | Market | Reserve Offer | Temporarily lock an offer for a user | Path: `id` (offerId) | Reservation object with `expiresAt` timestamp | 409 Already reserved; 404 Not found | Cahier des charges EF033, RM004 |
| 12 | Market | Cancel Reservation | Cancel an active reservation before expiration | Path: `id` (reservationId) | 204 No Content | 404 Reservation not found; 400 Already expired | Cahier des charges EF034 |
| 13 | Portfolio | Get Portfolio / Garage | Load collector's owned cards and valuation | None (JWT Bearer header) | Portfolio envelope: `positions` (List of `CartePossedee` items) | 401 Unauthorized | `AtelierCartes/Modeles/Portefeuille.cs`, EF050, EF051 |
| 14 | Activity | Transaction History | Load historical log of user operations | Query params: `page`, `pageSize` | List of Transaction records (type, amount, date, status) | 401 Unauthorized | Cahier des charges EF062, Annexe A |
| 15 | Booster | Booster Opening Flow | Simulated/local unpacking of 5 digital cards | None (Booster state counter) | List of 5 revealed `Carte` items | InvalidOperationException if Stock == 0 | Figma mockups 2 & 3, `AP Semestre 1/Services/ServiceApi.cs` |

---

## 5. Edge Cases & Error Handling

| # | Feature | Input / Condition | Observed / Documented Behavior |
|---|---------|-------------------|--------------------------------|
| 1 | Auth | Invalid username or password | Returns HTTP 401 Unauthorized. Server returns JSON error payload `{ "message": "Identifiants invalides." }`. UI displays explicit message without crashing. |
| 2 | Auth | Expired JWT Bearer token | Returns HTTP 401 Unauthorized. Client intercepts 401, attempts token refresh via `POST /auth/refresh`, and if refresh fails, cleans `SecureStorage` and navigates to Login (`REC15`). |
| 3 | Catalog | Negative or zero page number | Returns HTTP 400 Bad Request or clamps to page 1 (`CartesAutomobilesConsole` throws `JsonSerializationException` if totalPages is out of range 0–1000). |
| 4 | Catalog | Empty catalog or no search matches | Returns HTTP 200 OK with `items: []`. UI displays empty state view ("Aucun modèle pour cette marque", EF015). |
| 5 | Market | Buy with insufficient coin balance | Returns HTTP 400 Bad Request with `{ "code": "INSUFFICIENT_FUNDS", "message": "Solde insuffisant pour cet achat." }` (EF036). |
| 6 | Market | Buy offer already purchased by another user | Returns HTTP 409 Conflict with `{ "code": "OFFER_NOT_AVAILABLE", "message": "L'offre sélectionnée n'est plus disponible." }` (EF036, REC07). Client clears pending state and refreshes offers. |
| 7 | Market | Rapid double-click on Buy button | Client generates unique UUID `Idempotency-Key` and disables the button during in-flight request (`EF035`). Duplicate request returns cached original result without double charge (`RM006`, `REC06`). |
| 8 | Market | Sell a card already listed or reserved | Returns HTTP 409 Conflict (`RM003`). UI prevents user from listing cards already flagged `ALaVente == true`. |
| 9 | Market | Reservation expires during review | Returns HTTP 410 Gone / 409 Conflict upon attempt to confirm purchase. UI stops countdown timer and refreshes offer list (`REC08`). |
| 10 | Network | Complete loss of network connectivity | `HttpClient` throws `HttpRequestException` or `TaskCanceledException`. Client catches exception, displays offline banner/alert without losing local screen state (`ENF005`, `REC14`). |
| 11 | API | Server returns HTTP 204 No Content (DELETE) | Client checks `IsSuccessStatusCode` before calling `ReadAsStringAsync()`; avoids attempting JSON deserialization on empty body (`kit-etudiant/GUIDE-ETUDIANT.md` Step 2). |
| 12 | API | Non-JSON HTML error page (e.g. 502/503 from reverse proxy) | Deserializer catches `JsonException` and falls back to generic server error message without crashing (`AtelierCartes/ServiceApi.cs`). |

---

## 6. Data Contracts & JSON Schemas

### 6.1 Authentication Request (`DemandeConnexion`)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "DemandeConnexion",
  "type": "object",
  "properties": {
    "username": {
      "type": "string",
      "description": "Student login username or email"
    },
    "password": {
      "type": "string",
      "description": "Student login password"
    }
  },
  "required": ["username", "password"]
}
```

### 6.2 Authentication Response (`ReponseConnexion`)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ReponseConnexion",
  "type": "object",
  "properties": {
    "accessToken": {
      "type": "string",
      "description": "JWT Bearer access token string"
    }
  },
  "required": ["accessToken"]
}
```

### 6.3 Card Object (`Carte`)
Used in catalog endpoints (`GET /cards`, `GET /cards/{id}`):
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Carte",
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Unique identifier (UUID or string)"
    },
    "brand": {
      "type": "string",
      "description": "Manufacturer brand (e.g., Porsche, Ferrari)"
    },
    "model": {
      "type": "string",
      "description": "Vehicle model name (e.g., 911 GT3 RS)"
    },
    "variant": {
      "type": "string",
      "description": "Model edition or variant (e.g., 992 GT3 RS)"
    },
    "year": {
      "type": "integer",
      "description": "Manufacturing year"
    },
    "rarity": {
      "type": "string",
      "enum": ["COMMUNE", "PEU COMMUNE", "RARE", "ÉPIQUE", "LÉGENDAIRE", "MYTHIQUE"],
      "description": "Server-calculated rarity tier"
    }
  },
  "required": ["id", "brand", "model"]
}
```

### 6.4 Catalog Paginated Envelope
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ReponseCatalogue",
  "type": "object",
  "properties": {
    "items": {
      "type": "array",
      "items": { "$ref": "#/definitions/Carte" }
    },
    "page": { "type": "integer" },
    "pageSize": { "type": "integer" },
    "total": { "type": "integer" },
    "totalPages": { "type": "integer" }
  },
  "required": ["items", "totalPages"]
}
```

### 6.5 Owned Card Copy (`CartePossedee`)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "CartePossedee",
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Unique identifier of this specific owned physical/digital copy"
    },
    "card": {
      "$ref": "#/definitions/Carte",
      "description": "Card specification metadata"
    },
    "currentValue": {
      "type": "number",
      "description": "Current official market valuation in coins/CRD"
    }
  },
  "required": ["id", "card", "currentValue"]
}
```

### 6.6 Portfolio Response (`Portefeuille`)
Returned by `GET /portfolio`:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Portefeuille",
  "type": "object",
  "properties": {
    "positions": {
      "type": "array",
      "items": { "$ref": "#/definitions/CartePossedee" },
      "description": "List of owned cards in user's collection"
    }
  },
  "required": ["positions"]
}
```

### 6.7 Marketplace Offer (`Offre`)
Returned by `GET /cards/{id}/offers`, `POST /offers`, `PATCH /offers/{id}`:
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "Offre",
  "type": "object",
  "properties": {
    "id": {
      "type": "string",
      "description": "Unique identifier of the market listing"
    },
    "copyId": {
      "type": "string",
      "description": "Unique identifier of the owned card copy being sold"
    },
    "price": {
      "type": "number",
      "description": "Asking price in virtual currency"
    },
    "status": {
      "type": "string",
      "enum": ["active", "reserved", "sold", "cancelled"],
      "description": "Current listing status"
    }
  },
  "required": ["id", "copyId", "price", "status"]
}
```

### 6.8 Standard Error Response (`ErreurApi`)
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "ErreurApi",
  "type": "object",
  "properties": {
    "code": {
      "type": "string",
      "description": "Machine-readable business error code"
    },
    "message": {
      "type": "string",
      "description": "Human-readable error explanation"
    },
    "correlationId": {
      "type": "string",
      "description": "Trace ID for diagnostic correlation"
    },
    "errors": {
      "type": "array",
      "items": { "type": "string" },
      "description": "Validation details if 422/400"
    }
  },
  "required": ["message"]
}
```

---

## 7. Data Expansion Schema (`cars_database.json`)

Requirement **R3** mandates generating a rich standalone `cars_database.json` file. It must align with the core API fields (`id`, `brand`, `model`, `variant`, `year`, `rarity`) while providing high-resolution web vehicle photography and technical performance attributes.

### 7.1 JSON Schema for `cars_database.json`
```json
{
  "$schema": "http://json-schema.org/draft-07/schema#",
  "title": "CarsDatabase",
  "type": "array",
  "items": {
    "type": "object",
    "properties": {
      "id": { "type": ["integer", "string"] },
      "brand": { "type": "string" },
      "model": { "type": "string" },
      "variant": { "type": "string" },
      "year": { "type": "integer" },
      "powerHp": { "type": "integer" },
      "rarity": {
        "type": "string",
        "enum": ["COMMUNE", "PEU COMMUNE", "RARE", "ÉPIQUE", "LÉGENDAIRE", "MYTHIQUE"]
      },
      "copiesInCirculation": { "type": "integer" },
      "officialPrice": { "type": "number" },
      "imageUrl": {
        "type": "string",
        "format": "uri"
      },
      "description": { "type": "string" },
      "topSpeedKmH": { "type": "integer" },
      "zeroToHundred": { "type": "number" }
    },
    "required": [
      "id", "brand", "model", "variant", "year", 
      "powerHp", "rarity", "officialPrice", "imageUrl"
    ]
  }
}
```

### 7.2 Verified Vehicle Data Entries (Aligned with Figma & Reference Code)
1. **Ferrari LaFerrari Aperta (2017):**
   - Brand: `Ferrari`, Model: `LaFerrari Aperta`, Variant: `Aperta`
   - Year: `2017`, Power: `963 ch`, Rarity: `LÉGENDAIRE`, Official Price: `480 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1583121274602-3e2820c69888?auto=format&fit=crop&w=1200&q=80`
2. **Koenigsegg Jesko Absolut (2022):**
   - Brand: `Koenigsegg`, Model: `Jesko Absolut`, Variant: `Absolut`
   - Year: `2022`, Power: `1600 ch`, Rarity: `LÉGENDAIRE`, Official Price: `2 650 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1617814076367-b759c7d7e738?auto=format&fit=crop&w=1200&q=80`
3. **Lamborghini Huracán STO (2022):**
   - Brand: `Lamborghini`, Model: `Huracán STO`, Variant: `STO`
   - Year: `2022`, Power: `640 ch`, Rarity: `ÉPIQUE`, Official Price: `318 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1544829099-b9a0c07fad1a?auto=format&fit=crop&w=1200&q=80`
4. **Porsche 911 GT3 RS (2023):**
   - Brand: `Porsche`, Model: `911 GT3 RS`, Variant: `992 GT3 RS`
   - Year: `2023`, Power: `525 ch`, Rarity: `ÉPIQUE`, Official Price: `223 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1614162692292-7ac56d7f7f1e?auto=format&fit=crop&w=1200&q=80`
5. **Nissan GT-R NISMO (2024):**
   - Brand: `Nissan`, Model: `GT-R NISMO`, Variant: `NISMO Special Edition`
   - Year: `2024`, Power: `600 ch`, Rarity: `ÉPIQUE`, Official Price: `141 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1617654112368-307921291f42?auto=format&fit=crop&w=1200&q=80`
6. **Lotus Emira V6 First Edition (2023):**
   - Brand: `Lotus`, Model: `Emira`, Variant: `V6 First Edition`
   - Year: `2023`, Power: `405 ch`, Rarity: `ÉPIQUE`, Official Price: `89 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1618843479313-40f8afb4b4d8?auto=format&fit=crop&w=1200&q=80`
7. **Toyota GR Yaris Rally Edition (2023):**
   - Brand: `Toyota`, Model: `GR Yaris`, Variant: `Rally Edition`
   - Year: `2023`, Power: `261 ch`, Rarity: `RARE`, Official Price: `47 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1621135802920-133df287f89c?auto=format&fit=crop&w=1200&q=80`
8. **Renault Mégane R.S. Ultime (2023):**
   - Brand: `Renault`, Model: `Mégane R.S.`, Variant: `Ultime`
   - Year: `2023`, Power: `300 ch`, Rarity: `RARE`, Official Price: `44 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1629897048514-3dd7414fe72a?auto=format&fit=crop&w=1200&q=80`
9. **Mazda MX-5 RF (2023):**
   - Brand: `Mazda`, Model: `MX-5`, Variant: `RF PRHT 2.0 Skyactiv-G`
   - Year: `2023`, Power: `184 ch`, Rarity: `RARE`, Official Price: `38 000 coins`
   - Image URL: `https://images.unsplash.com/photo-1542282088-72c9c27ed0cd?auto=format&fit=crop&w=1200&q=80`
10. **Toyota Supra MK4 (1998):**
    - Brand: `Toyota`, Model: `Supra MK4`, Variant: `2JZ-GTE Twin Turbo`
    - Year: `1998`, Power: `330 ch`, Rarity: `RARE`, Official Price: `85 000 coins`
    - Image URL: `https://images.unsplash.com/photo-1619767886558-efdc259cde1a?auto=format&fit=crop&w=1200&q=80`
11. **Nissan Skyline GT-R R34 (2002):**
    - Brand: `Nissan`, Model: `Skyline GT-R R34`, Variant: `V-Spec II`
    - Year: `2002`, Power: `280 ch`, Rarity: `LÉGENDAIRE`, Official Price: `220 000 coins`
    - Image URL: `https://images.unsplash.com/photo-1552519507-da3b142c6e3d?auto=format&fit=crop&w=1200&q=80`
12. **Ferrari F40 (1990):**
    - Brand: `Ferrari`, Model: `F40`, Variant: `2.9 V8 Twin Turbo`
    - Year: `1990`, Power: `478 ch`, Rarity: `MYTHIQUE`, Official Price: `1 900 000 coins`
    - Image URL: `https://images.unsplash.com/photo-1592198084033-aade902d1aae?auto=format&fit=crop&w=1200&q=80`
13. **BMW M3 E46 (2005):**
    - Brand: `BMW`, Model: `M3`, Variant: `E46 Coupé`
    - Year: `2005`, Power: `343 ch`, Rarity: `PEU COMMUNE`, Official Price: `55 000 coins`
    - Image URL: `https://images.unsplash.com/photo-1555215695-3004980ad54e?auto=format&fit=crop&w=1200&q=80`
14. **Renault Clio 2 1.2 16V (2004):**
    - Brand: `Renault`, Model: `Clio 2`, Variant: `1.2 16V Dynamique`
    - Year: `2004`, Power: `75 ch`, Rarity: `COMMUNE`, Official Price: `3 500 coins`
    - Image URL: `https://images.unsplash.com/photo-1502877338535-766e1452684a?auto=format&fit=crop&w=1200&q=80`

---

## 8. ServiceAPI Architecture & Newtonsoft.Json Serialization

To strictly comply with **R1** ("using a single `ServiceAPI` file in the services folder") and **R2**, the service architecture must concentrate all network communication in `AutoMasters.Services.ServiceAPI`.

### 8.1 Key Implementation Characteristics
1. **Singleton & DI Compatible:**
   Provides `ServiceAPI.Instance` for direct access while allowing injection via `builder.Services.AddSingleton(ServiceAPI.Instance)`.
2. **Newtonsoft.Json Standard:**
   ```csharp
   // Serialization of request bodies
   string json = JsonConvert.SerializeObject(demande);
   using StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

   // Deserialization of response payloads
   T? resultat = JsonConvert.DeserializeObject<T>(jsonString);
   ```
3. **Pluggable HttpMessageHandler:**
   To facilitate unit testing without real network calls, `ServiceAPI` must expose a constructor or factory accepting an `HttpMessageHandler`:
   ```csharp
   public ServiceAPI(HttpMessageHandler handler, string? baseAddress = null)
   {
       _client = new HttpClient(handler)
       {
           BaseAddress = new Uri(baseAddress ?? "https://api-cartes-g2.fldsio.com/")
       };
   }
   ```
4. **Token Injection:**
   Automatically attaches the Bearer token to outgoing request headers once authenticated.

---

## 9. Unit Testing Requirements for `ServiceAPI`

In accordance with the Acceptance Criteria:
- *"Unit tests for `ServiceAPI` pass and verify basic data retrieval (using `Newtonsoft.Json`)."*
- *"Serveur simulé ou gestionnaire HTTP substituable"* (Cahier des charges §14.1).

### 9.1 Methods Requiring Unit Test Coverage

#### Test Group 1: Authentication (`SeConnecterAsync` / `LoginAsync`)
1. **`Test_SeConnecterAsync_Success`:**
   - **Setup:** Mock HTTP handler responds 200 OK with `{"accessToken":"jwt_test_token_xyz"}`.
   - **Invocation:** `await service.SeConnecterAsync("etudiant@bts.sio", "secret123");`
   - **Assertions:**
     - Authorization header is set to `Bearer jwt_test_token_xyz`.
     - Request body sent matches JSON `{"username":"etudiant@bts.sio","password":"secret123"}`.
2. **`Test_SeConnecterAsync_InvalidCredentials_ThrowsException`:**
   - **Setup:** Mock HTTP handler responds 401 Unauthorized with `{"message":"Identifiants invalides."}`.
   - **Invocation:** `service.SeConnecterAsync("bad_user", "wrong_pwd")`.
   - **Assertions:**
     - Throws `InvalidOperationException` or `Exception` with message containing "refusée", "expirée", or server error message.
3. **`Test_SeConnecterAsync_MalformedJson_ThrowsException`:**
   - **Setup:** Mock HTTP handler responds 200 OK with empty or corrupted JSON string `"{not_valid_json"`.
   - **Invocation:** `service.SeConnecterAsync("user", "pwd")`.
   - **Assertions:** Handled cleanly without silent failure.

#### Test Group 2: Portfolio / Garage (`RecupererCartesPossedeesAsync`)
1. **`Test_RecupererCartesPossedeesAsync_Success`:**
   - **Setup:** Mock HTTP handler responds 200 OK with:
     ```json
     {
       "positions": [
         {
           "id": "copy_001",
           "card": {
             "id": "card_porsche",
             "brand": "Porsche",
             "model": "911 GT3 RS",
             "variant": "992 GT3 RS"
           },
           "currentValue": 223000.0
         }
       ]
     }
     ```
   - **Invocation:** `var cartes = await service.RecupererCartesPossedeesAsync();`
   - **Assertions:**
     - `cartes` is not null and contains 1 element.
     - `cartes[0].Identifiant == "copy_001"`.
     - `cartes[0].Carte.Marque == "Porsche"`.
     - `cartes[0].Carte.Modele == "911 GT3 RS"`.
     - `cartes[0].ValeurActuelle == 223000.0m`.
2. **`Test_RecupererCartesPossedeesAsync_EmptyCollection`:**
   - **Setup:** Mock HTTP handler responds 200 OK with `{"positions":[]}`.
   - **Invocation:** `var cartes = await service.RecupererCartesPossedeesAsync();`
   - **Assertions:** Returns empty `ObservableCollection`, count == 0.

#### Test Group 3: Catalog Retrieval (`RecupererCartesAsync`)
1. **`Test_RecupererCartesAsync_Success`:**
   - **Setup:** Mock HTTP handler responds 200 OK with:
     ```json
     {
       "items": [
         {
           "id": "card_ferrari",
           "brand": "Ferrari",
           "model": "LaFerrari Aperta",
           "year": 2017,
           "rarity": "LÉGENDAIRE"
         }
       ],
       "page": 1,
       "totalPages": 1
     }
     ```
   - **Invocation:** `var catalog = await service.RecupererCartesAsync();`
   - **Assertions:**
     - Catalog contains 1 element.
     - Element properties correctly populated via `Newtonsoft.Json`.

#### Test Group 4: Marketplace Offers (`RecupererOffreAsync`, `CreerOffreAsync`)
1. **`Test_CreerOffreAsync_Success`:**
   - **Setup:** Mock HTTP handler responds 201 Created with:
     ```json
     {
       "id": "offer_999",
       "copyId": "copy_001",
       "price": 250000.0,
       "status": "active"
     }
     ```
   - **Invocation:** `var offre = await service.CreerOffreAsync("copy_001", 250000.0m);`
   - **Assertions:**
     - `offre.Identifiant == "offer_999"`.
     - `offre.Prix == 250000.0m`.
     - `offre.Statut == "active"`.

### 9.2 Mock Test Handler Architecture
A lightweight test class inheriting from `HttpMessageHandler` enables fast, reliable unit testing without third-party mocking libraries:
```csharp
public class MockHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handlerFunc;

    public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        _handlerFunc = handlerFunc;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(_handlerFunc(request));
    }
}
```

---

## 10. Summary & Recommended Action Plan

1. **Service API Implementation:** Adopt the unified `ServiceAPI.cs` implementation supporting both local mock data and remote REST consumption, structured with `Newtonsoft.Json` decorators.
2. **DTO Models:** Use dual-mapped DTOs (`[JsonProperty("...")]`) to seamlessly match French domain property names (`Marque`, `Modele`, `Identifiant`) with English JSON API keys (`brand`, `model`, `id`).
3. **Data Expansion:** Deploy the completed 14-vehicle `cars_database.json` file into the solution matching the extracted schema.
4. **Unit Test Suite:** Provide a dedicated MSTest or xUnit test project (`AutoMasters.Tests`) containing unit tests against `ServiceAPI` using `MockHttpMessageHandler`.
