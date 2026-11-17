# AutoMasters

Application mobile multiplateforme **.NET MAUI** de collection et d'échange de cartes numériques de véhicules automobiles d'exception.

---

## 1. Contexte Académique & Objectifs

Ce projet s'inscrit dans le cadre de la formation **BTS SIO SLAM 2ème année** (Services Informatiques aux Organisations - Option Solutions Logicielles et Applications Métiers).

Il a été conçu pour respecter rigoureusement les normes pédagogiques et professionnelles d'évaluation BTS SIO :
- **Conformité au projet de référence** : Respect strict de l'arborescence du projet modèle [`TestVoiture.git`](https://github.com/Thierrysio/TestVoiture.git).
- **Architecture logicielle standardisée** : Mise en œuvre du patron **MVVM (Modèle - Vue - VueModèle)** avec découplage complet de l'interface graphique et de la logique métier.
- **Convention académique de structuration C#** : Organisation uniforme de l'ensemble des classes en 4 régions françaises explicites (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`).
- **Contrainte du service unique** : Centralisation de tous les flux réseau au sein d'un seul fichier de service dans le dossier `Services/` (`ServiceAPI.cs`).
- **Paternité et intégrité** : Aucun contournement, aucune donnée truquée, communication réelle via requêtes HTTP REST avec sérialisation `Newtonsoft.Json`.

---

## 2. Architecture Logicielle & Choix Techniques

### 2.1 Cadre Applicatif
- **Framework** : .NET MAUI (.NET 10.0) ciblant Windows (`net10.0-windows10.0.19041.0`) en mode unpackaged desktop pour l'évaluation académique immédiate.
- **Format de solution** : Solution Visual Studio 2026 (`AutoMasters.slnx`).

### 2.2 Patron de Conception MVVM
- **Bibliothèque MVVM** : `CommunityToolkit.Mvvm`
  - Héritage de `ObservableObject` pour tous les ViewModels.
  - Déclaration des propriétés observables via l'attribut `[ObservableProperty]`.
  - Commandes asynchrones et synchrones via `[RelayCommand]`.
- **Pureté du Code-Behind (`.xaml.cs`)** :
  - Les fichiers code-behind sont limités à l'appel de `InitializeComponent()` et à l'assignation du `BindingContext = vueModele;`.
  - Aucune logique métier, manipulation de données ou appel HTTP dans les vues.

### 2.3 Sérialisation & Données
- **Newtonsoft.Json** (`JsonConvert`, `[JsonProperty]`, `[JsonIgnore]`) utilisé pour tous les DTOs et échanges avec l'API REST.
- Sérialisation contractuelle tolérant les variations de clés API (ex. `username`/`email`, `remainingBalance`/`balance`).

### 2.4 Service Réseau Unique (`Services/ServiceAPI.cs`)
- Encapsule l'intégralité des endpoints de l'**API Groupe 2** (`https://api-cartes-g2.fldsio.com`).
- Gestion de session avec jeton JWT et stockage sécurisé via `SecureStorage`.
- Support d'un constructeur avec injection d'un `HttpMessageHandler` pour exécuter la suite de tests unitaires et E2E de manière 100% déterministe et hors-ligne.
- Respect strict de la règle métier d'idempotence (en-tête HTTP `Idempotency-Key` et clé dans le payload JSON) pour les ordres d'achat immédiat.

---

## 3. Fonctionnalités de l'Application

| Module | Fonctionnalité | Description |
|---|---|---|
| **Connexion** | Authentification JWT | Écran d'accueil fidèle à la maquette Figma avec logo calandre, gestion des erreurs d'authentification et nettoyage sécurisé du mot de passe en mémoire. |
| **Catalogue** | Exploration & Filtres | Grille réactive de véhicules avec filtre par rareté (Commune, Rare, Épique, Légendaire) et barre de recherche temps réel. |
| **Fiche Véhicule** | Détails Techniques | Vue détaillée avec puissance (ch), vitesse maximale, 0-100 km/h, cote officielle et nombre d'exemplaires en circulation. |
| **Marché** | Place de Marché en Direct | Liste des offres de vente en temps réel avec badge "🟢 EN DIRECT", acheteurs/vendeurs et bouton d'action flottant (+). |
| **Achat Immédiat** | Achat & Réservation | Dialogue de confirmation d'achat immédiat sécurisé par clé d'idempotence GUID unique et réservation temporaire d'offres. |
| **Vente** | Mise en Vente | Formulaire modal permettant de choisir une carte possédée, définir son prix de vente et la publier sur le marché. |
| **Garage** | Portefeuille du Joueur | Tableau de bord avec valorisation totale (ex. "482 500 coins"), suivi des plus/moins-values nettes et badges de quantité multiple ("x2"). |
| **Boosters** | Révélation de Paquets | Dialogue d'ouverture de booster (paquets disponibles, compte à rebours) et séquence de révélation interactive carte par carte ("Carte 1 sur 5", "RETOURNER ▶"). |

---

## 4. Données Étendues (`cars_database.json`)

Le fichier `cars_database.json` situé à la racine du dépôt constitue une base de données autonome enrichie de supercars et hypercars (Ferrari, Koenigsegg, Lamborghini, Porsche, McLaren, Bugatti, Rimac, Pagani, etc.).
- Chaque véhicule respecte scrupuleusement la structure de l'API (`id`, `brand`, `model`, `variant`, `year`, `specs`, `rarity`, `officialPrice`, `circulationCount`).
- Toutes les entrées intègrent des URLs d'images réelles haute définition hébergées sur le web (Unsplash).

---

## 5. Structure du Dépôt

L'arborescence reproduit fidèlement la nomenclature de `TestVoiture.git` :

```text
AutoMasters/
├── AutoMasters.slnx                    # Solution Visual Studio 2026
├── cars_database.json                  # Base de données étendue de véhicules
├── .gitignore                          # Règles d'exclusion Git pour .NET / MAUI
├── README.md                           # Documentation du projet
│
├── AutoMasters/                        # Projet d'application .NET MAUI
│   ├── AutoMasters.csproj
│   ├── App.xaml / App.xaml.cs
│   ├── AppShell.xaml / AppShell.xaml.cs
│   ├── MauiProgram.cs
│   ├── Modeles/                        # DTOs et Modèles du domaine
│   │   ├── Booster.cs
│   │   ├── Carte.cs
│   │   ├── CartePossedee.cs
│   │   ├── DemandeAchat.cs
│   │   ├── DemandeAchatImmediat.cs
│   │   ├── DemandeConnexion.cs
│   │   ├── DemandeCreationOffre.cs
│   │   ├── DemandeModificationOffre.cs
│   │   ├── DemandeReservation.cs
│   │   ├── ErreurApi.cs
│   │   ├── Offre.cs
│   │   ├── Portefeuille.cs
│   │   ├── ReponseAchat.cs
│   │   ├── ReponseAchatImmediat.cs
│   │   ├── ReponseCatalogue.cs
│   │   ├── ReponseConnexion.cs
│   │   ├── ReponseOffres.cs
│   │   └── ReponseReservation.cs
│   ├── Services/                       # Couche d'accès réseau (Service unique)
│   │   └── ServiceAPI.cs               # SEUL fichier présent dans Services/
│   ├── VuesModeles/                    # ViewModels (CommunityToolkit.Mvvm)
│   │   ├── BaseVueModele.cs
│   │   ├── BoosterVueModele.cs
│   │   ├── CatalogueVueModele.cs
│   │   ├── ConnexionVueModele.cs
│   │   ├── CreationOffreVueModele.cs
│   │   ├── FicheCarteVueModele.cs
│   │   ├── GarageVueModele.cs
│   │   └── MarcheVueModele.cs
│   ├── Vues/                           # Vues XAML (Code-behind 100% pur)
│   │   ├── BoosterPage.xaml / .xaml.cs
│   │   ├── CataloguePage.xaml / .xaml.cs
│   │   ├── ConnexionPage.xaml / .xaml.cs
│   │   ├── CreationOffrePage.xaml / .xaml.cs
│   │   ├── FicheCartePage.xaml / .xaml.cs
│   │   ├── GaragePage.xaml / .xaml.cs
│   │   └── MarchePage.xaml / .xaml.cs
│   ├── Platforms/
│   │   └── Windows/                    # Configuration et point d'entrée WinUI 3
│   │       ├── App.xaml / App.xaml.cs
│   │       ├── app.manifest
│   │       └── Package.appxmanifest
│   └── Resources/                      # Ressources graphiques et thèmes
│       ├── AppIcon/
│       ├── Fonts/
│       ├── Images/
│       └── Styles/
│           ├── Colors.xaml
│           └── Styles.xaml
│
├── AutoMasters.Tests/                  # Suite de tests unitaires (xUnit)
│   ├── AutoMasters.Tests.csproj
│   ├── MockHttpMessageHandler.cs
│   └── ServiceApiTests.cs
│
└── AutoMasters.E2ETests/               # Suite de tests E2E / Validation fonctionnelle
    ├── AutoMasters.E2ETests.csproj
    └── E2ETests.cs
```

---

## 6. Compilation et Exécution

### 6.1 Prérequis
- **SDK .NET** : version 10.0 ou supérieure avec la charge de travail MAUI (`dotnet workload install maui`).
- **Visual Studio 2026** (avec support .NET MAUI et C# 13+) ou environnement CLI standard Windows.

### 6.2 Compilation de la Solution
Pour compiler la solution complète avec 0 avertissement et 0 erreur :

```powershell
dotnet build AutoMasters.slnx -c Debug
```

### 6.3 Exécution des Tests Unitaires
Pour lancer les tests unitaires du service API :

```powershell
dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj
```

### 6.4 Exécution des Tests E2E
Pour exécuter les 300 tests de validation fonctionnelle et d'architecture :

```powershell
dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj
```

### 6.5 Exécution Complète de la Suite de Tests
Pour lancer l'ensemble des 397 tests automatisés :

```powershell
dotnet test AutoMasters.slnx
```

### 6.6 Lancement de l'Application MAUI sur Windows
Pour exécuter directement l'application sur le bureau Windows :

```powershell
dotnet run --project AutoMasters/AutoMasters.csproj -f net10.0-windows10.0.19041.0
```

---

## 7. Charte Graphique & Design Automobile

L'interface utilisateur a été conçue pour offrir une esthétique automobile sombre et dynamique, inspirée des interfaces de télémétrie sportive et conforme aux maquettes Figma du projet :
- **Arrière-plan sombre principal** : `#10080B`
- **Rouge performance / Accentuation** : `#E63946`
- **Surfaces et cartes secondaires** : `#221215` / `#1E1014`
- **Indicateurs de succès et statuts en direct** : `#10B981`
- **Bordures et séparateurs discrets** : `#3A1E24`
