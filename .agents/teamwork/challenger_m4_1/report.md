# Rapport d'Évaluation Empirique et Contradictoire (Challenger) — Milestone 4 Phase 2

**Auteur** : challenger_m4_1 (Rôles : critic, specialist)  
**Date** : 2026-10-10  
**Périmètre** : Validation E2E & Durcissement Contradictoire de la Solution Complète (Fonctionnalités F29, F30)  
**Verdict Final** : **APPROVE**  

---

## 1. Résumé Exécutif & Verdict

Une campagne complète de vérification empirique et contradictoire a été exécutée directement sur la solution AutoMasters (.NET MAUI 10 / Windows unpackaged).

### Résumé des Mesures Empiriques

| Suite de Tests | Commande Exécutée | Tests Attendus | Réussis | Échecs | Durée | Statut |
|---|---|---|---|---|---|---|
| **AutoMasters.Tests** | `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` | 86 | **86** | 0 | 389 ms | **CONFORME** |
| **AutoMasters.E2ETests** | `dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj` | 300 | **300** | 0 | 837 ms | **CONFORME** |
| **Total Combiné** | — | 386 | **386** | **0** | **1 226 ms** | **100% SUCCÈS** |

### **Verdict : APPROVE**
L'ensemble des critères d'acceptation du cahier des charges, de l'architecture BTS SIO SLAM 2ème année et des exigences de résilience hors-ligne et de robustesse face aux données manquantes/nulles sont **validés sans réserve**.

---

## 2. Validation Empirique de la Suite E2E (`AutoMasters.E2ETests`)

La commande de vérification :
```powershell
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj
```
Résultat verbatim :
```
AutoMasters.E2ETests -> C:\Users\stent\source\repos\AutoMasters\AutoMasters.E2ETests\bin\Debug\net10.0-windows10.0.19041.0\AutoMasters.E2ETests.dll
Série de tests pour C:\Users\stent\source\repos\AutoMasters\AutoMasters.E2ETests\bin\Debug\net10.0-windows10.0.19041.0\AutoMasters.E2ETests.dll (.NETCoreApp,Version=v10.0)
Au total, 1 fichiers de test ont correspondu au modèle spécifié.

Réussi!  - échec :     0, réussite :   300, ignorée(s) :     0, total :   300, durée : 837 ms - AutoMasters.E2ETests.dll (net10.0)
```

### Répartition par Niveaux (Tiers 1 à 4)
- **Tier 1 — Couverture Fonctionnelle Nominale (Features F01 à F28)** : **140 tests**
  - F01 à F04 (Fondations, layout, dépendances, `cars_database.json`) : 20 tests
  - F05 à F08 (Modèles DTOs, ServiceAPI, Authentification, Catalogue) : 20 tests
  - F09 à F12 (Fiche détaillée, Portefeuille, Offres du marché, Achat immédiat) : 20 tests
  - F13 à F17 (Réservation, Création d'offre, Booster 5 cartes, Mockability, Tests unitaires) : 25 tests
  - F18 à F22 (Connexion, AppShell, Catalogue, Marché) : 25 tests
  - F23 à F28 (Achat immédiat UX, Vente UX, Garage/P&L, Booster UX, Tokens, Pureté code-behind) : 30 tests
- **Tier 2 — Cas Limites & Frontières Adversariales (Boundary Cases)** : **140 tests**
  - Entrées vides, chaînes géantes, prix négatifs, quotas épuisés, doubles clics, perte de session, tokens corrompus, réponses HTML 502, timeouts réseau, etc.
- **Tier 3 — Combinaisons Inter-Fonctionnalités (Pairwise Integration)** : **15 tests**
  - Authentification + Catalogue, Auth + Marché, Auth + Portefeuille, Catalogue + Fiche détaillée, Booster + Portefeuille, Idempotence + Double tap, Pureté XAML + MVVM.
- **Tier 4 — Scénarios Réels & Parcours Utilisateur (User Journeys)** : **5 tests**
  - `Journey_01_NewStudent_Launch_And_Authentication` (Lancement et connexion BTS SIO)
  - `Journey_02_Collector_Catalog_Search_Filter_And_Inspection` (Recherche, filtrage rareté et fiche technique)
  - `Journey_03_Strategic_Marketplace_Acquisition` (Réservation 15 min, achat avec clé idempotente GUID)
  - `Journey_04_Portfolio_Valuation_Audit_And_Market_Listing` (Audit de valorisation, détection de doublons x2, mise en vente FAB)
  - `Journey_05_Daily_Booster_Ceremony_And_Reveal_Sequence` (Ouverture booster, décompte quota, révélation 5 cartes)

---

## 3. Validation Empirique de la Suite Unitaire (`AutoMasters.Tests`)

La commande de vérification :
```powershell
dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj
```
Résultat verbatim :
```
AutoMasters -> C:\Users\stent\source\repos\AutoMasters\AutoMasters\bin\Debug\net10.0-windows10.0.19041.0\win-x64\AutoMasters.dll
AutoMasters.Tests -> C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\bin\Debug\net10.0-windows10.0.19041.0\AutoMasters.Tests.dll
Série de tests pour C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\bin\Debug\net10.0-windows10.0.19041.0\AutoMasters.Tests.dll (.NETCoreApp,Version=v10.0)
Au total, 1 fichiers de test ont correspondu au modèle spécifié.

Réussi!  - échec :     0, réussite :    86, ignorée(s) :     0, total :    86, durée : 389 ms - AutoMasters.Tests.dll (net10.0)
```

### Répartition des 86 Tests Unitaires
1. **`ServiceApiTests.cs` (33 tests)** :
   - Tests exhaustifs de l'unique service HTTP `Services/ServiceAPI.cs`.
   - Couverture complète de `POST /auth/login`, `GET /cards`, `GET /cards/{id}`, `GET /portfolio`, `GET /offers`, `POST /orders/buy`, `POST /offers/{id}/reservations`, `POST /offers`, `POST /boosters/open`.
   - Validation de la gestion du token Bearer, de l'idempotence, de la désérialisation JSON via `Newtonsoft.Json`, et du déballage des enveloppes paginées.
2. **`ChallengerM3VerificationTests.cs` (27 tests)** :
   - Vérification de la pureté absolue du code-behind (`*.xaml.cs` exempts de `HttpClient`, `ServiceAPI` ou logique métier).
   - Validation de la navigation AppShell, de la gestion des erreurs d'authentification et de la préservation de l'email lors d'une saisie de mot de passe erronée.
   - Idempotence concurrente sur la finalisation de booster (clics multiples thread-safe).
3. **`ViewModelEmpiricalChallengeTests.cs` (26 tests)** :
   - Vérification contradictoire de la logique de présentation (`MarcheVueModele`, `GarageVueModele`, `BoosterVueModele`, `CreationOffreVueModele`).
   - Injection d'en-tête `Idempotency-Key` au format GUID, détection de solde insuffisant sans requête réseau inutile.
   - Validation des calculs de rentabilité (+32 500 € en vert, pertes en rouge), validation stricte du prix de vente (> 0).

---

## 4. Épreuve de Résilience des ViewModels (Données Nulles / Manquantes)

Chaque ViewModel a été analysé sous l'angle contradictoire des données nulles, manquantes ou corrompues :

1. **`ConnexionVueModele`** :
   - Propriétés `Email` et `MotDePasse` encapsulées avec opérateur coalescent `value ?? ""` pour interdire les références nulles.
   - Validation préalable des entrées vides ou d'un format email invalide avant tout appel API.
   - Protection `try/catch` intégrale garantissant l'affichage d'un message d'erreur clair sans crash applicatif.
2. **`CatalogueVueModele`** :
   - Tolérance absolue à un `serviceApi` ou un retour `resultat` nul (`resultat ?? new List<Carte>()`).
   - Filtrage LINQ défensif avec vérification explicite de non-nullité (`c.Marque != null && c.Marque.Contains(...)`).
   - Aucun risque de `NullReferenceException` même si une carte du catalogue possède des propriétés textuelles nulles.
3. **`MarcheVueModele`** :
   - Méthodes `AcheterOffreAsync(null)` et `ReserverOffreAsync(null)` protégées par court-circuit immédiat (`if (EstOccupe || offre == null) return;`).
   - Filtrage sécurisé avec navigation conditionnelle `o.Carte?.Marque != null`.
   - Appels aux boîtes de dialogue MAUI protégés par `Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null`.
4. **`GarageVueModele`** :
   - Initialisation défensive de `Portefeuille` et `CartesPossedees`.
   - Tolérance aux collections vides ou positions manquantes sans division par zéro.
   - Exceptions de synchronisation capturées et converties en message utilisateur informatif.
5. **`BoosterVueModele`** :
   - Sécurisation contre les listes de cartes vides (`cartesTirees.Count == 0 || EstTermine`).
   - Évaluation de la rareté et de la couleur par opérateur de navigation sécurisée `CarteActuelle?.Rarete`.
   - Déclenchement d'événement thread-safe `SurBoosterTermine?.Invoke()`.
   - Gestion des appuis rapides répétés (rapid double-tap) avec décrémentation unique du quota.
6. **`FicheCarteVueModele`** :
   - Propriétés calculées (`Marque`, `Modele`, `PuissanceFormatee`, `PrixOfficielFormate`, etc.) implémentant un fallback systématique (`CarteSelectionnee?.Marque ?? ""`, `"Tirage non spécifié"`).
   - Chargement asynchrone sécurisé avec vérification `if (string.IsNullOrWhiteSpace(cibleId)) return;`.
7. **`CreationOffreVueModele`** :
   - Interception immédiate si aucun véhicule n'est sélectionné (`CarteSelectionnee == null`).
   - Nettoyage des chaînes de prix (virgules, espaces) et analyse sécurisée via `decimal.TryParse` avec `CultureInfo.InvariantCulture`.
   - Rejet automatique des prix négatifs ou nuls (<= 0) sans appel API.

---

## 5. Herméticité & Exécution Hors-Ligne (`MockHttpMessageHandler`)

L'exigence d'indépendance vis-à-vis du réseau et des serveurs externes a été vérifiée de manière formelle :
- **Absence de dépendance externe** : Aucun appel HTTP réel n'est émis sur Internet pendant l'exécution des tests.
- **`MockHttpMessageHandler`** : Injecté dans `ServiceAPI`, il intercepte l'ensemble des requêtes HTTP (`GET`, `POST`, `PATCH`, `DELETE`), enregistre les en-têtes et le corps pour vérification, et permet d'injecter des scénarios d'anomalie réseau :
  - Simulation de panne réseau (`HttpRequestException`)
  - Simulation de timeout / délai dépassé (`TaskCanceledException`)
  - Simulation d'erreurs serveur HTTP 500, 502 (Bad Gateway HTML), 400, 401, 404, 409
- **`MockGroup2ApiServer`** : Serveur en mémoire hermétique et déterministe simulant avec fidélité la totalité des endpoints du Groupe 2, la balance de jetons, le verrouillage de réservation (15 min), et la déduplication par clé d'idempotence.

---

## 6. Synthèse d'Évaluation Contradictoire

| Dimension Contradictoire | Risque Évalué | Résultat des Tests | Statut |
|---|---|---|---|
| **Exécution E2E globale** | Échec ou instabilité de tests | 300 / 300 réussis (837 ms) | **PASSE** |
| **Exécution unitaire** | Régression sur ServiceAPI ou ViewModels | 86 / 86 réussis (389 ms) | **PASSE** |
| **Résilience aux valeurs nulles** | Crash `NullReferenceException` | 100% des ViewModels sécurisés | **PASSE** |
| **Indépendance réseau** | Fuite réseau / dépendance sandbox | 100% hermétique hors-ligne | **PASSE** |
| **Idempotence des transactions** | Double débit de jetons / double offre | Idempotency-Key GUID validée | **PASSE** |
| **Pureté MVVM & Code-behind** | Code métier dans le code-behind XAML | 0 violation (vérifié statiquement) | **PASSE** |

---

## 7. Décision Finale

Le système **AutoMasters** satisfait l'ensemble des exigences fonctionnelles, architecturales et de robustesse du Milestone 4 Phase 2.

**Verdict définitif : APPROVE**
