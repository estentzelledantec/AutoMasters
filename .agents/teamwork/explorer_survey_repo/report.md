# Comprehensive Survey & Architectural Blueprint: Reference Repository (TestVoiture) & Local .NET Build Environment

**Author:** `explorer_survey_repo`  
**Date:** 2026-10-07  
**Working Directory:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_survey_repo`  
**Reference Repository:** `https://github.com/Thierrysio/TestVoiture.git`  
**Target Solution:** `AutoMasters` (.NET MAUI, MVVM, Newtonsoft.Json, VS 2026 `.slnx`)

---

## 1. Executive Summary

This investigation analyzed the reference GitHub repository (`https://github.com/Thierrysio/TestVoiture.git`) and thoroughly surveyed the host machine's build environment.

### Key Discoveries:
1. **Academic Origin & Pedagogy:**
   The reference repository `TestVoiture` is the reference implementation for the national BTS SIO 2nd-year TP course (*"TP MAUI Recettes Pas à Pas"*, 47 pages, authored by Thierry SIO). It demonstrates strict, classical French naming conventions (`Modeles`, `Services`, `Vues`, `VuesModeles`), explicit C# region structures (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`), classical MVVM with `CommunityToolkit.Mvvm`, JSON handling via `Newtonsoft.Json`, and an in-memory singleton HTTP service (`Services/ServiceApi.cs`).
2. **Local Environment & Tooling:**
   - **.NET SDK:** `10.0.401` installed at `C:\Program Files\dotnet\sdk`.
   - **IDE:** Visual Studio 2026 Community (v18.10.x).
   - **Workloads Installed:** `maui-windows (10.0.20/10.0.100)`, `android (36.1.69/10.0.100)`, `ios (26.5.10301/10.0.100)`, `maccatalyst (26.5.10301/10.0.100)`.
   - **Compilation Target:** The reference project compiles for `net10.0-windows10.0.19041.0` targeting Windows Unpackaged (`<WindowsPackageType>None</WindowsPackageType>`). Verified compilation succeeds with `0 errors`.
   - **Solution Format (`.slnx`):** Native `.slnx` format is supported by .NET 10 SDK (`dotnet solution migrate`). The `.slnx` file is clean, modern XML and compiles directly with `dotnet build <Solution>.slnx`.
   - **Unit Testing Verification:** Verified that an MSTest / xUnit project targeting `net10.0-windows10.0.19041.0` references the MAUI project, restores, compiles, and passes unit tests with `0 failures`.
3. **Core Architectural Blueprint:**
   The AutoMasters project will replicate the structure of `TestVoiture.git` with French folder and namespace naming, strict single-file `Services/ServiceAPI.cs`, full MVVM separation, and models matching Group 2 API specifications.

---

## 2. Local Build Environment Survey

### 2.1 SDK and Runtime Diagnostics
- Command output for `dotnet --version`: `10.0.401`
- SDK Location: `C:\Program Files\dotnet\sdk\10.0.401`
- Workload Manifest: `10.0.400-manifests.b0700452`

### 2.2 Installed Workloads (`dotnet workload list`)
| Workload ID | Manifest Version | Installation Source | Status |
|-------------|------------------|---------------------|--------|
| `maui-windows` | `10.0.20/10.0.100` | VS 18.10.12224.181 | **Installed & Operational** |
| `android` | `36.1.69/10.0.100` | VS 18.10.12224.181 | Installed |
| `ios` | `26.5.10301/10.0.100` | VS 18.10.12224.181 | Installed |
| `maccatalyst` | `26.5.10301/10.0.100` | VS 18.10.12224.181 | Installed |

### 2.3 Visual Studio 2026 & `.slnx` Format Compatibility
- **Visual Studio Version:** Visual Studio 2026 Community (Internal Version 18.x).
- **Git Binary Location:** `C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\CommonExtensions\Microsoft\TeamFoundation\Team Explorer\Git\cmd\git.exe`
- **`.slnx` File Syntax:**
  ```xml
  <Solution>
    <Project Path="AutoMasters.csproj" />
    <Project Path="AutoMasters.Tests/AutoMasters.Tests.csproj" />
  </Solution>
  ```
- **CLI Verification:** Successfully migrated and built with `dotnet build TestVoiture.slnx -c Debug` -> Build succeeded in 15.6s with 0 errors.

### 2.4 Unit Test Project Compatibility
When referencing a Windows MAUI project (`net10.0-windows10.0.19041.0`), standard `net10.0` test projects fail due to framework mismatch.
**Solution verified on system:** The test project must target `<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>`.
With this target, `dotnet test` executes successfully.

---

## 3. Reference Repository Deep-Dive (`TestVoiture.git`)

### 3.1 Repository Tree Structure
```
TestVoiture/
│
├── App.xaml                            # Application resources (MergedDictionaries Colors & Styles)
├── App.xaml.cs                         # CreateWindow with NavigationPage
├── MauiProgram.cs                      # Host builder, fonts, debug logging
├── LISEZ-MOI.md                        # Documentation and course recipe mapping
├── TestVoiture.csproj                  # SDK-style MAUI project file (net10.0-windows10.0.19041.0)
├── TestVoiture.sln                     # Visual Studio solution file (can be .slnx)
│
├── Modeles/                            # 10 Data Transfer and Domain Models
│   ├── Carte.cs                        # Vehicle card definition (id, brand, model, variant)
│   ├── CartePossedee.cs                # Owned card instance (id, card, currentValue)
│   ├── DemandeConnexion.cs             # Login request payload (username, password)
│   ├── DemandeCreationOffre.cs         # Offer creation payload (copyId, price)
│   ├── DemandeModificationOffre.cs     # Offer modification payload (price)
│   ├── ErreurApi.cs                    # Error payload (message)
│   ├── Offre.cs                        # Market offer (id, copyId, price, status)
│   ├── Portefeuille.cs                 # User portfolio (positions: ObservableCollection<CartePossedee>)
│   ├── ReponseConnexion.cs             # Login response (accessToken)
│   └── ReponseOffres.cs                # Offers response (items: ObservableCollection<Offre>)
│
├── Services/                           # ONLY ONE FILE ALLOWED
│   └── ServiceApi.cs                   # Singleton HTTP client with all API endpoints
│
├── VuesModeles/                        # MVVM ViewModels
│   ├── ConnexionVueModele.cs           # Login logic, busy state, navigation
│   ├── CartesVueModele.cs              # Portfolio list management, selection, navigation
│   └── OffreVueModele.cs               # Offer CRUD, price parsing, state management
│
├── Vues/                               # XAML ContentPages
│   ├── ConnexionPage.xaml              # Login UI
│   ├── ConnexionPage.xaml.cs           # Pure code-behind (BindingContext = new ConnexionVueModele())
│   ├── CartesPage.xaml                 # CollectionView list of owned cars
│   ├── CartesPage.xaml.cs              # Pure code-behind
│   ├── OffrePage.xaml                  # Offer detail and CRUD actions
│   └── OffrePage.xaml.cs               # Pure code-behind
│
├── Platforms/
│   └── Windows/
│       ├── App.xaml                    # MauiWinUIApplication definition
│       ├── App.xaml.cs                 # WinUI entry point invoking MauiProgram
│       ├── Package.appxmanifest        # Windows application manifest
│       └── app.manifest                # Windows common controls & DPI manifest
│
├── Properties/
│   └── launchSettings.json             # Profiles for Windows Machine debugging
│
└── Resources/
    ├── AppIcon/                        # appicon.svg, appiconfg.svg
    ├── Fonts/                          # OpenSans-Regular.ttf, OpenSans-Semibold.ttf
    ├── Images/                         # App graphic assets
    ├── Raw/                            # Static asset files
    ├── Splash/                         # splash.svg
    └── Styles/
        ├── Colors.xaml                 # Color palette resources
        └── Styles.xaml                 # Control styles (Button, Label, Entry, etc.)
```

---

## 4. Code Patterns & Conventions in `TestVoiture`

### 4.1 Strict French Academic Terminology
All folders, files, classes, properties, and variables follow French naming conventions:
- `Modeles` (Models)
- `Vues` (Views)
- `VuesModeles` (ViewModels)
- `Services` (Services)
- `Carte`, `CartePossedee`, `Portefeuille`, `Offre`
- `Identifiant`, `MotDePasse`, `JetonAcces`, `EstOccupe`, `EstDisponible`

### 4.2 C# Region Structure Discipline
Every class adheres to a standardized region pattern:
#### In Models (`Modeles/`):
```csharp
namespace AutoMasters.Modeles;

public class Carte
{
    #region Attributs
    private string identifiant = "";
    private string marque = "";
    private string modele = "";
    private string variante = "";
    #endregion

    #region Constructeurs
    // Empty or parameterized
    #endregion

    #region Getter/Setter
    [JsonProperty("id")]
    public string Identifiant
    {
        get { return identifiant; }
        set { identifiant = value; }
    }
    // Other properties...
    #endregion

    #region Methodes
    // Empty or helper methods
    #endregion
}
```

#### In ViewModels (`VuesModeles/`):
```csharp
namespace AutoMasters.VuesModeles;

public partial class ConnexionVueModele : ObservableObject
{
    #region Attributs
    private ServiceAPI serviceApi = ServiceAPI.Instance;

    [ObservableProperty]
    private string identifiant = "";

    [ObservableProperty]
    private string motDePasse = "";

    [ObservableProperty]
    private string message = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;
    #endregion

    #region Constructeurs
    // If needed
    #endregion

    #region Proprietes
    public bool EstDisponible => !EstOccupe;
    #endregion

    #region Methodes
    // Private helper methods
    #endregion

    #region Commandes
    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        // ...
    }
    #endregion
}
```

#### In Views (`Vues/`):
Code-behind contains **only** the constructor initializing components and binding the ViewModel:
```csharp
namespace AutoMasters.Vues;

public partial class ConnexionPage : ContentPage
{
    #region Constructeurs
    public ConnexionPage()
    {
        InitializeComponent();
        BindingContext = new ConnexionVueModele();
    }
    #endregion
}
```
**Strict rule:** Absolutely zero business logic in code-behind!

---

## 5. Design of `Services/ServiceAPI.cs`

### 5.1 Strict Requirement
The `Services` folder must contain **only one file** named `ServiceAPI` (i.e. `ServiceAPI.cs`).
No second file, interface, or auxiliary helper is permitted inside `Services/`.

### 5.2 Implementation Pattern
- **Singleton Lifecycle:**
  ```csharp
  private static ServiceAPI instance = new ServiceAPI();
  public static ServiceAPI Instance => instance;
  ```
- **HttpClient Setup:**
  ```csharp
  private HttpClient client;

  private ServiceAPI()
  {
      client = new HttpClient();
      client.BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/");
      client.Timeout = TimeSpan.FromSeconds(15);
  }
  ```
  *(Note the mandatory trailing slash `/` in `BaseAddress`)*.
- **Authentication Header Injection:**
  ```csharp
  client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", connexion.JetonAcces);
  ```
- **HTTP Methods Used:**
  - `POST` for `auth/login` and `offers`
  - `GET` for `portfolio`, `cards/{id}/offers`, and `cards`
  - `PATCH` (via `HttpRequestMessage(HttpMethod.Patch, ...)`) for `offers/{id}`
  - `DELETE` for `offers/{id}`
- **Error Handling Helper:**
  `LireReponseAsync(HttpResponseMessage reponse)` deserializes `ErreurApi` from JSON and raises clean descriptive exceptions.

---

## 6. Project Configuration Specifications

### 6.1 `AutoMasters.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <OutputType>Exe</OutputType>
    <RootNamespace>AutoMasters</RootNamespace>
    <UseMaui>true</UseMaui>
    <SingleProject>true</SingleProject>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <MauiXamlInflator>SourceGen</MauiXamlInflator>
    <ApplicationTitle>AutoMasters</ApplicationTitle>
    <ApplicationId>fr.fldsio.automasters</ApplicationId>
    <ApplicationDisplayVersion>1.0</ApplicationDisplayVersion>
    <ApplicationVersion>1</ApplicationVersion>
    <WindowsPackageType>None</WindowsPackageType>
    <SupportedOSPlatformVersion>10.0.17763.0</SupportedOSPlatformVersion>
    <TargetPlatformMinVersion>10.0.17763.0</TargetPlatformMinVersion>
  </PropertyGroup>

  <ItemGroup>
    <MauiIcon Include="Resources\AppIcon\appicon.svg" ForegroundFile="Resources\AppIcon\appiconfg.svg" Color="#0D0D0E" />
    <MauiSplashScreen Include="Resources\Splash\splash.svg" Color="#0D0D0E" BaseSize="128,128" />
    <MauiImage Include="Resources\Images\*" />
    <MauiFont Include="Resources\Fonts\*" />
    <MauiAsset Include="Resources\Raw\**" LogicalName="%(RecursiveDir)%(Filename)%(Extension)" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.Maui.Controls" Version="10.0.20" />
    <PackageReference Include="CommunityToolkit.Mvvm" Version="8.4.2" />
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
    <PackageReference Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />
  </ItemGroup>
</Project>
```

### 6.2 Solution File (`AutoMasters.slnx`)
```xml
<Solution>
  <Project Path="AutoMasters.csproj" />
  <Project Path="AutoMasters.Tests/AutoMasters.Tests.csproj" />
</Solution>
```

### 6.3 Test Project File (`AutoMasters.Tests/AutoMasters.Tests.csproj`)
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.13.0" />
    <PackageReference Include="MSTest.TestAdapter" Version="3.8.2" />
    <PackageReference Include="MSTest.TestFramework" Version="3.8.2" />
    <PackageReference Include="coverlet.collector" Version="6.0.4" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\AutoMasters.csproj" />
  </ItemGroup>
</Project>
```

---

## 7. AutoMasters Feature Mapping Blueprint

To incorporate the full scope from `ORIGINAL_REQUEST.md` and the Figma mockups while respecting the `TestVoiture.git` structure:

| Functional Area | Model (`Modeles/`) | ViewModel (`VuesModeles/`) | View (`Vues/`) | Service Method in `ServiceAPI.cs` |
|-----------------|--------------------|----------------------------|----------------|-----------------------------------|
| **Connexion (Login)** | `DemandeConnexion`, `ReponseConnexion` | `ConnexionVueModele` | `ConnexionPage` | `SeConnecterAsync(id, mdp)` |
| **Garage / Portefeuille** | `Portefeuille`, `CartePossedee` | `GarageVueModele` (or `CartesVueModele`) | `GaragePage` (or `CartesPage`) | `RecupererCartesPossedeesAsync()` |
| **Catalogue des Cartes** | `Carte`, `ReponseCatalogue` | `CatalogueVueModele` | `CataloguePage` | `RecupererCatalogueAsync(search, filter)` |
| **Marché / Trading** | `Offre`, `ReponseOffres`, `DemandeAchat` | `MarcheVueModele` | `MarchePage` | `RecupererOffresAsync()`, `AcheterOffreAsync()` |
| **Gestion d'Offre (CRUD)** | `DemandeCreationOffre`, `DemandeModificationOffre` | `OffreVueModele` | `OffrePage` | `CreerOffreAsync()`, `ModifierOffreAsync()`, `SupprimerOffreAsync()` |
| **Booster Pack** | `BoosterPack`, `ReponseBooster` | `BoosterVueModele` | `BoosterPage` (modal) | `OuvrirBoosterAsync()` |

### Data Expansion Blueprint (`cars_database.json`)
The `cars_database.json` file must reside in the project root (and optionally mirrored in `Resources/Raw/cars_database.json`) formatted with valid car objects containing:
- `id`: unique string ID
- `brand`: car brand (e.g., Ferrari, Porsche, BMW, Bugatti)
- `model`: car model name
- `variant`: edition/variant (e.g., GT3 RS, Nürburgring Edition)
- `year`: manufacture year
- `powerHp`: horsepower rating
- `rarity`: "Commune", "Rare", "Épique", "Légendaire"
- `copiesInCirculation`: circulation quantity
- `officialPrice`: base currency valuation
- `imageUrl`: verified real web URL pointing to a high quality car photo

---

## 8. Summary of Verification Checks Performed

1. Cloned reference repository to clean workspace.
2. Verified project and solution structure against course recipes (LISEZ-MOI.md).
3. Built `TestVoiture.sln` using `dotnet build` with .NET 10 SDK -> Result: Success (0 errors).
4. Migrated solution to `.slnx` format and executed `dotnet build TestVoiture.slnx` -> Result: Success (0 errors).
5. Created unit test project referencing MAUI project, validated target framework resolution (`net10.0-windows10.0.19041.0`) and executed `dotnet test` -> Result: Success (0 failures).
