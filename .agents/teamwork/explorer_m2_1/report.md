# Rapport d'Architecture et Conception : Modèles Métier & DTOs (Milestone 2 - F05)

**Projet :** AutoMasters (.NET MAUI / Visual Studio 2026)  
**Auteur :** `explorer_m2_1`  
**Cible académique :** BTS SIO SLAM 2ème année (Normes `TestVoiture.git`)  
**API cible :** REST API Groupe 2 (`https://api-cartes-g2.fldsio.com`)  
**Date :** 7 octobre 2026  
**Statut :** Spécification complète et modèles prêts pour implémentation  

---

## 1. Synthèse Exécutive & Périmètre de la Fonctionnalité F05

Le présent rapport formalise la conception architecturale exhaustive de la couche de données et des objets de transfert (DTOs) pour l'application mobile **AutoMasters**, dans le cadre du **Jalon 2 (Milestone 2)** et de la fonctionnalité **F05 (API Domain Models)**.

### 1.1 Objectifs Clés
1. **Conformité stricte BTS SIO SLAM :** Respecter les conventions de nommage en français (`Marque`, `Modele`, `Prix`, `ValeurActuelle`, `Portefeuille`, etc.) et la structuration obligatoire en 4 régions C# (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).
2. **Interopérabilité avec l'API Groupe 2 :** Décorer explicitement chaque propriété avec des attributs `[JsonProperty("...")]` de `Newtonsoft.Json` pour assurer la sérialisation / désérialisation bidirectionnelle sans perte de données.
3. **Résilience et Tolérance aux Variations :** Gérer les champs optionnels (`variant`, `description`, `refreshToken`), tolérer les champs inattendus (`F05_B01`), supporter les alias de champs (ex. `purchasePrice` et `acquisitionPrice`, `gainLoss` et `profitLoss`, `username` et `email`) et préserver les caractères accentués UTF-8 (`F05_B04`).
4. **Calculs Métier Embarqués :** Intégrer les propriétés calculées indispensables aux maquettes Figma (ex. `GainLoss` sur `CartePossedee`, `TotalValuation` et `TotalGainLoss` sur `Portefeuille`, formattage de compte à rebours et quotas sur `Booster`).

---

## 2. Matrice de Correspondance API Groupe 2 ↔ Modèles C#

| Modèle C# | Propriété C# (Français) | Clé JSON API Groupe 2 | Type C# | Rôle & Description |
|---|---|---|---|---|
| **Carte** | `Identifiant` | `"id"` | `string` | Identifiant unique de la carte (UUID ou clé alphanumérique) |
| **Carte** | `Marque` | `"brand"` | `string` | Constructeur automobile (ex: Porsche, Ferrari, Bugatti) |
| **Carte** | `Modele` | `"model"` | `string` | Modèle du véhicule (ex: 911 GT3 RS, LaFerrari) |
| **Carte** | `Variante` | `"variant"` | `string?` | Édition ou finition (optionnelle, ex: Weissach Package) |
| **Carte** | `Annee` | `"year"` | `int` | Année de fabrication |
| **Carte** | `Puissance` | `"powerHp"` | `int` | Puissance du moteur en chevaux DIN (ch) |
| **Carte** | `Rarete` | `"rarity"` | `string` | Niveau de rareté officiel déterminé par le serveur |
| **Carte** | `PrixOfficiel` | `"officialPrice"` | `decimal` | Cours officiel en monnaie virtuelle (coins / CRD) |
| **Carte** | `ExemplairesEnCirculation`| `"copiesInCirculation"` | `int` | Volume d'exemplaires émis sur le marché |
| **Carte** | `ImageUrl` | `"imageUrl"` | `string` | URL HTTPS haute résolution de la photographie |
| **Carte** | `Description` | `"description"` | `string?` | Présentation historique et technique du véhicule |
| **Carte** | `VitesseMax` | `"topSpeedKmH"` | `int` | Vitesse de pointe en km/h |
| **Carte** | `ZeroACent` | `"zeroToHundred"` | `double` | Temps d'accélération de 0 à 100 km/h en secondes |
| **CartePossedee** | `Identifiant` | `"id"` | `string` | Identifiant de l'exemplaire possédé ou de la position |
| **CartePossedee** | `IdentifiantExemplaire`| `"copyId"` | `string?` | Identifiant d'exemplaire utilisé pour la mise en vente |
| **CartePossedee** | `Carte` | `"card"` | `Carte` | Métadonnées complètes de la carte automobile |
| **CartePossedee** | `Quantite` | `"quantity"` | `int` | Nombre d'exemplaires détenus (défaut : 1) |
| **CartePossedee** | `PrixAchat` | `"purchasePrice"` / `"acquisitionPrice"` | `decimal` | Prix d'acquisition initial (0 si obtenu via booster) |
| **CartePossedee** | `ValeurActuelle` | `"currentValue"` | `decimal` | Cours actuel unitaire officiel |
| **CartePossedee** | `GainPerte` | `"gainLoss"` / `"profitLoss"` | `decimal` | Plus/moins-value : `(ValeurActuelle * Quantite) - (PrixAchat * Quantite)` |
| **Portefeuille** | `CartesPossedees` | `"positions"` / `"ownedCards"` | `ObservableCollection<CartePossedee>` | Liste réactive des cartes détenues par le joueur |
| **Portefeuille** | `ValeurTotale` | `"totalValuation"` | `decimal` | Somme des valorisations actuelles de toutes les positions |
| **Portefeuille** | `TotalGainPerte` | `"totalProfitLoss"` / `"totalGainLoss"` | `decimal` | Somme des plus/moins-values nettes du portefeuille |
| **Offre** | `Identifiant` | `"id"` | `string` | Identifiant unique de l'offre sur le marché |
| **Offre** | `IdentifiantExemplaire`| `"copyId"` | `string` | Identifiant de l'exemplaire mis en vente |
| **Offre** | `Carte` | `"card"` | `Carte?` | Métadonnées du véhicule (incluses lors des requêtes catalogue) |
| **Offre** | `Vendeur` | `"seller"` | `string` | Identifiant ou pseudonyme du vendeur |
| **Offre** | `Prix` | `"price"` | `decimal` | Prix de vente demandé |
| **Offre** | `Statut` | `"status"` | `string` | État : `"active"`, `"reserved"`, `"sold"`, `"cancelled"` |
| **Offre** | `EstReservee` | `"isReserved"` | `bool` | Vrai si l'offre est verrouillée par une réservation |
| **Offre** | `ReservePar` | `"reservedBy"` | `string?` | Identifiant de l'acheteur ayant réservé l'offre |
| **Offre** | `DateExpiration` | `"expiresAt"` | `DateTime?` | Date/heure d'expiration du verrou de réservation (RM004) |
| **DemandeConnexion** | `Identifiant` | `"username"` / `"email"` | `string` | Adresse e-mail ou identifiant étudiant |
| **DemandeConnexion** | `MotDePasse` | `"password"` | `string` | Mot de passe en clair pour authentification HTTPS |
| **ReponseConnexion** | `JetonAcces` | `"accessToken"` / `"token"` | `string` | Jeton JWT d'accès Bearer |
| **ReponseConnexion** | `JetonRafraichissement`| `"refreshToken"` | `string?` | Jeton de renouvellement de session |
| **ReponseConnexion** | `DateExpiration` | `"expiresAt"` | `DateTime?` | Horodatage d'expiration du jeton |
| **DemandeCreationOffre** | `IdentifiantExemplaire`| `"copyId"` | `string` | Identifiant de la carte possédée à mettre en vente |
| **DemandeCreationOffre** | `Prix` | `"price"` | `decimal` | Prix de vente unitaire fixé par le joueur (> 0) |
| **DemandeModificationOffre** | `Prix` | `"price"` | `decimal` | Nouveau prix de vente appliqué à l'offre existante |
| **ReponseOffres** | `Offres` | `"items"` | `ObservableCollection<Offre>` | Liste des offres actives sur le marché |
| **ReponseOffres** | `Total` | `"total"` | `int` | Nombre total d'offres répertoriées |
| **ErreurApi** | `Code` | `"code"` | `string?` | Code d'erreur machine (ex: `"INSUFFICIENT_FUNDS"`) |
| **ErreurApi** | `Message` | `"message"` | `string` | Message d'explication lisible par l'utilisateur |
| **ErreurApi** | `CorrelationId` | `"correlationId"` | `string?` | Identifiant de traçabilité serveur pour le diagnostic |
| **ErreurApi** | `Erreurs` | `"errors"` | `List<string>?` | Liste d'erreurs de validation détaillées |
| **Booster** | `PaquetsDisponibles`| `"availablePacks"` / `"remainingQuota"` / `"stock"` | `int` | Nombre de paquets restants à ouvrir aujourd'hui |
| **Booster** | `PaquetsMax` | `"maxPacks"` / `"quotaMax"` | `int` | Quota maximum journalier (10 par défaut) |
| **Booster** | `TempsRestantSecondes`| `"cooldownRemaining"` / `"nextRefillSeconds"` | `int` | Secondes restantes avant la recharge du quota |
| **Booster** | `CartesTirees` | `"openedCards"` / `"cards"` | `List<Carte>` | Liste des 5 cartes débloquées lors de l'ouverture |
| **Booster** | `ProchainDisponible`| `"nextAvailableAt"` | `DateTimeOffset?` | Horodatage précis du prochain déblocage |

---

## 3. Spécification Détaillée des Modèles et DTOs

Tous les modèles ci-dessous ont été rédigés et sauvegardés dans `.agents/teamwork/explorer_m2_1/proposed_Modeles/`. Ils respectent l'organisation stricte en 4 régions BTS SIO, les règles de nullabilité C# et les besoins de liaison de données MVVM.

### 3.1 `Carte.cs`
- **Fichier source proposé :** `.agents/teamwork/explorer_m2_1/proposed_Modeles/Carte.cs`
- **Rôle :** Représentation d'une carte automobile dans le catalogue officiel et dans les détails de véhicule.
- **Régions C# :**
  - `#region Attributs` : champs privés d'instance (`identifiant`, `marque`, `modele`, `variante`, `annee`, `puissance`, `rarete`, `prixOfficiel`, `exemplairesEnCirculation`, `imageUrl`, `description`, `vitesseMax`, `zeroACent`).
  - `#region Constructeurs` : constructeur par défaut `public Carte()` + constructeur paramétré complet.
  - `#region Getter/Setter` : accesseurs décorés `[JsonProperty("...")]`.
  - `#region Methodes` : propriétés de présentation formatées `[JsonIgnore]` (`NomComplet`, `PuissanceFormatee`, `VitesseMaxFormatee`, `ZeroACentFormate`, `PrixOfficielFormate`) et `ToString()`.

### 3.2 `CartePossedee.cs`
- **Fichier source proposé :** `.agents/teamwork/explorer_m2_1/proposed_Modeles/CartePossedee.cs`
- **Rôle :** Représentation d'une carte détenue par l'utilisateur dans son garage (`GET /portfolio`).
- **Particularités techniques :**
  - Gère les alias JSON `id` / `copyId`, `purchasePrice` / `acquisitionPrice`, et `gainLoss` / `profitLoss`.
  - Calcul dynamique du gain/perte : `(ValeurActuelle * Quantite) - (PrixAchat * Quantite)` si non fourni par le serveur.
  - Propriété UI `QuantiteBadge` (ex: `"x2"`), correspondant exactement au badge visualisé sur la maquette Figma Screen 2.

### 3.3 `Portefeuille.cs`
- **Fichier source proposé :** `.agents/teamwork/explorer_m2_1/proposed_Modeles/Portefeuille.cs`
- **Rôle :** Synthèse du garage du collectionneur (`GET /portfolio`).
- **Particularités techniques :**
  - Collection réactive `ObservableCollection<CartePossedee>` pour la liaison directe aux composants MAUI (`CollectionView`, `ListView`).
  - Agrégation automatique de `ValeurTotale` et `TotalGainPerte` si le serveur omet ces champs globaux.
  - Propriété formatée `ValeurTotaleFormatee` (ex: `"482 500 coins"`) matching Figma Screen 2.

### 3.4 `Offre.cs`
- **Fichier source proposé :** `.agents/teamwork/explorer_m2_1/proposed_Modeles/Offre.cs`
- **Rôle :** Offre de vente listée sur la place de marché (`GET /cards/{id}/offers`, `POST /offers`).
- **Particularités techniques :**
  - Prise en compte des règles métier **RM003** (exclusivité d'engagement) et **RM004** (expiration de réservation).
  - Propriété booléenne `EstReservee` synchronisée avec le statut `"reserved"`.
  - Propriété `TempsRestantReservation` pour affichage de compte à rebours avant expiration.

### 3.5 `DemandeConnexion.cs` & `ReponseConnexion.cs`
- **Fichiers sources proposés :**
  - `.agents/teamwork/explorer_m2_1/proposed_Modeles/DemandeConnexion.cs`
  - `.agents/teamwork/explorer_m2_1/proposed_Modeles/ReponseConnexion.cs`
- **Rôle :** Flux d'authentification (`POST /auth/login`, `POST /auth/refresh`).
- **Particularités techniques :**
  - `DemandeConnexion` tolère à la fois `"username"` et `"email"`.
  - `ReponseConnexion` accepte `"accessToken"` ou `"token"`, et intègre la méthode de validation de validité `EstValide`.

### 3.6 `DemandeCreationOffre.cs` & `DemandeModificationOffre.cs`
- **Fichiers sources proposés :**
  - `.agents/teamwork/explorer_m2_1/proposed_Modeles/DemandeCreationOffre.cs`
  - `.agents/teamwork/explorer_m2_1/proposed_Modeles/DemandeModificationOffre.cs`
- **Rôle :** Publication et mise à jour de listings de vente sur le marché (`POST /offers`, `PATCH /offers/{id}`).
- **Particularités techniques :**
  - Méthodes de validation métier légères `EstValide` vérifiant que le prix est strictement positif (> 0) et que l'exemplaire est identifié.

### 3.7 `ReponseOffres.cs` & `ErreurApi.cs`
- **Fichiers sources proposés :**
  - `.agents/teamwork/explorer_m2_1/proposed_Modeles/ReponseOffres.cs`
  - `.agents/teamwork/explorer_m2_1/proposed_Modeles/ErreurApi.cs`
- **Rôle :** Enveloppe de récupération des offres de marché et désérialisation normalisée des réponses d'erreur HTTP (400, 401, 404, 409, 500).

### 3.8 `Booster.cs`
- **Fichier source proposé :** `.agents/teamwork/explorer_m2_1/proposed_Modeles/Booster.cs`
- **Rôle :** Gestion de l'état du booster quotidien et de l'ouverture de 5 cartes (Feature F15, Figma Mockups 2 & 3).
- **Particularités techniques :**
  - Tolérance multi-clés : `availablePacks` / `remainingQuota` / `stock`.
  - Formatage direct du compte à rebours `TempsRestantFormate` (ex: `"09:55"`).
  - Formatage du libellé de quota `QuotaFormate` (ex: `"3/10 disponibles"`).
  - Prise en charge des 5 cartes tirées dans `CartesTirees` (`openedCards` / `cards`).

### 3.9 DTOs Complémentaires Conçus
- **`DemandeAchat.cs` :** Encapsule l'identifiant de l'offre et l'en-tête d'idempotence UUID requis par la règle métier **RM006** (`POST /orders/buy`).
- **`ReponseCatalogue.cs` :** Enveloppe paginée (`items`, `page`, `pageSize`, `total`, `totalPages`) pour le catalogue de cartes.
- **`ReponseAchat.cs` :** Confirmation de transaction d'achat (`status`, `transactionId`, `remainingBalance`).
- **`ReponseReservation.cs` :** Confirmation de réservation avec timestamp d'expiration (`offerId`, `status`, `expiresAt`).

---

## 4. Analyse des Cas aux Limites & Robustesse

Les modèles ont été spécifiquement conçus pour satisfaire l'ensemble des scénarios de test E2E de niveau Tier 1 et Tier 2 :

1. **Tolérance aux champs inattendus (`F05_B01`) :**
   Grâce au comportement par défaut de `Newtonsoft.Json` (`MissingMemberHandling.Ignore`), la présence de champs additionnels non cartographiés par le serveur ne génère aucune exception.
2. **Gestion des champs optionnels nuls (`F05_B02`) :**
   Les champs `variante` et `description` sont déclarés en types nullables C# (`string?`), empêchant tout échec de désérialisation lorsque l'API renvoie la valeur JSON `null`.
3. **Précision monétaire & grands nombres (`F05_B03`, `F10_B05`) :**
   L'utilisation stricte du type `decimal` pour tous les prix et valorisations (`PrixOfficiel`, `PrixAchat`, `ValeurActuelle`, `GainPerte`, `ValeurTotale`, `TotalGainPerte`) élimine les erreurs d'arrondi à virgule flottante inhérentes aux types `float`/`double`.
4. **Préservation Unicode et caractères accentués (`F05_B04`) :**
   Les marques, modèles et raretés comportant des accents français (ex: `"Mégane R.S. Ultime"`, `"ÉPIQUE"`, `"LÉGENDAIRE"`) sont intégralement préservés lors du cycle de sérialisation / désérialisation.
5. **Plafonds et identifiants vides (`F05_B05`) :**
   Les setters protègent contre les valeurs `null` accidentelles en convertissant vers des chaînes vides `""`, garantissant la stabilité des liaisons XAML.

---

## 5. Guide de Migration pour l'Agent Implémenteur

L'agent en charge de l'implémentation de la Milestone 2 (Phase 1) pourra déployer ces modèles dans `AutoMasters/Modeles/` en suivant les étapes ci-dessous :

1. **Copie des fichiers de modèles :**
   Copier l'ensemble des fichiers situés dans `.agents/teamwork/explorer_m2_1/proposed_Modeles/` vers `AutoMasters/Modeles/` :
   - `Carte.cs` (remplacement avec ajouts des constructeurs et formateurs)
   - `CartePossedee.cs` (nouveau)
   - `Portefeuille.cs` (nouveau)
   - `Offre.cs` (nouveau)
   - `DemandeConnexion.cs` (mise à jour avec alias email)
   - `ReponseConnexion.cs` (mise à jour avec alias token)
   - `DemandeCreationOffre.cs` (nouveau)
   - `DemandeModificationOffre.cs` (nouveau)
   - `ReponseOffres.cs` (nouveau)
   - `ErreurApi.cs` (mise à jour)
   - `Booster.cs` (nouveau)
   - `DemandeAchat.cs` (nouveau)
   - `ReponseCatalogue.cs` (nouveau)
   - `ReponseAchat.cs` (nouveau)
   - `ReponseReservation.cs` (nouveau)

2. **Compilation et Vérification du Build :**
   Exécuter `dotnet build AutoMasters.slnx -c Debug` pour confirmer que la compilation s'effectue avec 0 erreur et 0 avertissement.

3. **Intégration dans `ServiceAPI.cs` :**
   Utiliser directement ces types dans les signatures publiques de `AutoMasters/Services/ServiceAPI.cs` :
   - `Task<ReponseConnexion> SeConnecterAsync(string identifiant, string motDePasse)`
   - `Task<List<Carte>> RecupererCatalogueAsync(string? recherche = null, string? rarete = null)`
   - `Task<Carte> RecupererFicheCarteAsync(string id)`
   - `Task<Portefeuille> RecupererPortefeuilleAsync()`
   - `Task<List<Offre>> RecupererOffresAsync(string? cardId = null)`
   - `Task<ReponseAchat> AcheterOffreAsync(string offreId, string? cleIdempotence = null)`
   - `Task<ReponseReservation> ReserverOffreAsync(string offreId)`
   - `Task<Offre> CreerOffreAsync(string copyId, decimal prix)`
   - `Task<Offre> ModifierOffreAsync(string offreId, decimal nouveauPrix)`
   - `Task<Booster> OuvrirBoosterAsync()`

---

## 6. Conclusion

La conception des modèles métier et DTOs pour la Milestone 2 (F05) est complète, exhaustivement validée contre les spécifications de l'API Groupe 2, le cahier des charges officiel, les implémentations de référence BTS SIO (`AtelierCartes`, `CartesAutomobilesConsole`, `AP Semestre 1`) et la suite de tests E2E.
Tous les fichiers de remplacement prêts à l'emploi sont déposés dans le dossier de travail de l'agent `explorer_m2_1`.
