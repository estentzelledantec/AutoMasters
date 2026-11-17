# Milestone 1 Architectural Report: Exact Folder Layout & French Academic Coding Standards

**Document ID:** `M1-LAYOUT-STANDARDS`  
**Author:** `explorer_m1_2`  
**Milestone:** Milestone 1 — Solution Foundation & Data Expansion  
**Target Solution:** `AutoMasters` (.NET MAUI, Windows Unpackaged `net10.0-windows10.0.19041.0`, Visual Studio 2026 `.slnx`)  
**Reference Implementations:** `https://github.com/Thierrysio/TestVoiture.git` & BTS SIO SLAM 2nd-Year Academic Standard (*TP MAUI Recettes Pas à Pas*)  
**Target File:** `c:\Users\stent\source\repos\AutoMasters\.agents\teamwork\explorer_m1_2\report.md`  

---

## 1. Executive Summary

This report establishes the complete structural implementation strategy, directory hierarchy, placeholder file definitions, and French coding region standards for **Milestone 1** of the `AutoMasters` application.

### Key Structural Directives:
1. **Replication of `TestVoiture.git` Hierarchy:**
   The repository mirrors the exact French naming and folder structure of the national BTS SIO 2nd-year reference repository:
   - `AutoMasters/Modeles/`
   - `AutoMasters/Services/` (**MANDATORY RULE:** exactly **one** file, `ServiceAPI.cs`, is permitted in this directory)
   - `AutoMasters/VuesModeles/`
   - `AutoMasters/Vues/`
   - `AutoMasters/Platforms/Windows/`
   - `AutoMasters/Resources/` (`AppIcon/`, `Fonts/`, `Images/`, `Splash/`, `Raw/`, `Styles/Colors.xaml`, `Styles/Styles.xaml`)
2. **Initial Foundational Files:**
   Complete, compilable C# and XAML templates are defined for `App.xaml`, `App.xaml.cs`, `AppShell.xaml`, `AppShell.xaml.cs`, `MauiProgram.cs`, the Windows platform entry points, resources, and initial placeholder models/views to guarantee an immediate zero-error build (`dotnet build AutoMasters.slnx -c Debug`).
3. **Strict French Academic Region Standards:**
   Every C# class must strictly follow the 4-region convention in uniform order:
   - `#region Attributs`
   - `#region Constructeurs`
   - `#region Getter/Setter` (or `#region Proprietes`)
   - `#region Methodes` (including `#region Commandes` in ViewModels)
4. **Code-Behind MVVM Purity:**
   All `.xaml.cs` code-behind files in `Vues/` contain strictly the constructor with component initialization and ViewModel binding (`BindingContext = new ...VueModele()`). Zero business logic, zero event handling logic, zero API calls.

---

## 2. Directory Hierarchy Blueprint

### 2.1 Complete Solution Tree
```
c:\Users\stent\source\repos\AutoMasters\
├── AutoMasters.slnx                           # VS 2026 Solution file (managed by M1.1)
├── cars_database.json                         # Seed dataset >= 14 vehicles (managed by M1.3)
│
├── AutoMasters/                               # Primary MAUI Application Project
│   ├── AutoMasters.csproj                     # SDK-style MAUI project targeting Windows
│   ├── App.xaml                               # Global application resource dictionaries
│   ├── App.xaml.cs                            # Application window lifecycle & shell instantiation
│   ├── AppShell.xaml                          # Shell navigation root with dark automotive styling
│   ├── AppShell.xaml.cs                       # Shell code-behind (pure constructor)
│   ├── MauiProgram.cs                         # Host builder, fonts, debug logging
│   │
│   ├── Modeles/                               # Domain models & DTOs (Newtonsoft.Json)
│   │   ├── Carte.cs                           # Vehicle card model (placeholder / foundational)
│   │   ├── DemandeConnexion.cs                # Login request DTO
│   │   ├── ReponseConnexion.cs                # Login response DTO
│   │   └── ErreurApi.cs                       # Standardized API error payload
│   │
│   ├── Services/                              # MANDATORY: STRICT SINGLE FILE DIRECTORY
│   │   └── ServiceAPI.cs                      # Centralized HTTP singleton (ONLY file here)
│   │
│   ├── VuesModeles/                           # MVVM ViewModels (CommunityToolkit.Mvvm)
│   │   └── ConnexionVueModele.cs              # Authentication ViewModel with regions & commands
│   │
│   ├── Vues/                                  # XAML Pages with pure code-behind
│   │   ├── ConnexionPage.xaml                 # Login screen UI
│   │   └── ConnexionPage.xaml.cs              # Pure code-behind (BindingContext initialization only)
│   │
│   ├── Platforms/
│   │   └── Windows/                           # Windows unpackaged desktop execution
│   │       ├── App.xaml                       # MauiWinUIApplication XAML
│   │       ├── App.xaml.cs                    # WinUI entry point invoking MauiProgram
│   │       ├── app.manifest                   # DPI & long path Windows manifest
│   │       └── Package.appxmanifest           # AppX packaging manifest (unpackaged)
│   │
│   ├── Properties/
│   │   └── launchSettings.json                # Windows Machine debug profile
│   │
│   └── Resources/                             # Application visual assets & styles
│       ├── AppIcon/                           # App icon vector assets
│       │   ├── appicon.svg
│       │   └── appiconfg.svg
│       ├── Fonts/                             # Typography assets
│       │   ├── OpenSans-Regular.ttf
│       │   └── OpenSans-Semibold.ttf
│       ├── Images/                            # Static images
│       │   └── dotnet_bot.png
│       ├── Raw/                               # Raw bundled assets
│       ├── Splash/                            # Splash screen vector assets
│       │   └── splash.svg
│       └── Styles/                            # Visual design tokens & control templates
│           ├── Colors.xaml                    # Automotive dark palette & theme colors
│           └── Styles.xaml                    # MAUI control styling rules
│
└── AutoMasters.Tests/                         # Unit Test Project (referenced in .slnx for M2)
    └── AutoMasters.Tests.csproj
```

### 2.2 Strict Single-Service Constraint (`Services/`)
- **Rule:** The `AutoMasters/Services/` directory must contain **exactly one file**, named `ServiceAPI.cs`.
- **Prohibited:**
  - No `IServiceAPI.cs` interface file.
  - No subdirectories (e.g. `Services/Auth/`, `Services/Interfaces/`).
  - No helper classes in separate files (e.g. `ApiConstants.cs`, `HttpHelper.cs`).
  - No secondary service classes (e.g. `BoosterService.cs`, `MarketService.cs`).
- **Encapsulation Strategy:**
  If auxiliary helpers (such as private constants, response parsing methods, or internal DTO handlers) are required by the service layer, they must be declared as `private` or `internal` members or nested classes inside `ServiceAPI.cs`.

---

## 3. Initial Placeholder & Core Files Specification

All files below are calibrated for the BTS SIO 2nd-year academic standard and ready for implementation.

### 3.1 `AutoMasters/App.xaml`
Merges application resource dictionaries (`Colors.xaml` and `Styles.xaml`).

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Application xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
             xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
             xmlns:local="clr-namespace:AutoMasters"
             x:Class="AutoMasters.App">
    <Application.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="Resources/Styles/Colors.xaml" />
                <ResourceDictionary Source="Resources/Styles/Styles.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </Application.Resources>
</Application>
```

---

### 3.2 `AutoMasters/App.xaml.cs`
Window creation lifecycle entry point. Utilizes the French academic region structure and French parameter naming.

```csharp
namespace AutoMasters;

public partial class App : Application
{
    #region Constructeurs

    public App()
    {
        InitializeComponent();
    }

    #endregion

    #region Methodes

    protected override Window CreateWindow(IActivationState? etatActivation)
    {
        return new Window(new AppShell());
    }

    #endregion
}
```

---

### 3.3 `AutoMasters/AppShell.xaml`
Defines the application navigation shell. Configured with dark automotive styling tokens (`#10080B`, `#E63946`) matching the Figma mockups, and routes to `ConnexionPage` as the initial landing screen for M1.

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<Shell
    x:Class="AutoMasters.AppShell"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vues="clr-namespace:AutoMasters.Vues"
    Title="AutoMasters"
    BackgroundColor="#10080B"
    ForegroundColor="#FFFFFF"
    TitleColor="#FFFFFF"
    Shell.NavBarHasShadow="False">

    <!-- Route initiale pour l'évaluation Milestone 1 -->
    <ShellContent
        Title="Connexion"
        ContentTemplate="{DataTemplate vues:ConnexionPage}"
        Route="ConnexionPage" />

</Shell>
```

---

### 3.4 `AutoMasters/AppShell.xaml.cs`
Shell code-behind adhering strictly to pure constructor initialization and French region standards.

```csharp
namespace AutoMasters;

public partial class AppShell : Shell
{
    #region Constructeurs

    public AppShell()
    {
        InitializeComponent();
    }

    #endregion

    #region Methodes

    #endregion
}
```

---

### 3.5 `AutoMasters/MauiProgram.cs`
Host builder entry point configuring MAUI fonts, logging, and application bootstrap. Uses French academic identifiers (`constructeur`, `polices`).

```csharp
using Microsoft.Extensions.Logging;

namespace AutoMasters;

public static class MauiProgram
{
    #region Methodes

    public static MauiApp CreateMauiApp()
    {
        var constructeur = MauiApp.CreateBuilder();
        constructeur
            .UseMauiApp<App>()
            .ConfigureFonts(polices =>
            {
                polices.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                polices.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        constructeur.Logging.AddDebug();
#endif

        return constructeur.Build();
    }

    #endregion
}
```

---

### 3.6 Foundational Models (`AutoMasters/Modeles/`)

#### `Carte.cs`
```csharp
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

public class Carte
{
    #region Attributs

    private string identifiant = "";
    private string marque = "";
    private string modele = "";
    private string variante = "";
    private int annee;
    private int puissance;
    private string rarete = "";
    private decimal prixOfficiel;
    private int exemplairesEnCirculation;
    private string imageUrl = "";
    private string description = "";

    #endregion

    #region Constructeurs

    public Carte()
    {
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("id")]
    public string Identifiant
    {
        get { return identifiant; }
        set { identifiant = value; }
    }

    [JsonProperty("brand")]
    public string Marque
    {
        get { return marque; }
        set { marque = value; }
    }

    [JsonProperty("model")]
    public string Modele
    {
        get { return modele; }
        set { modele = value; }
    }

    [JsonProperty("variant")]
    public string Variante
    {
        get { return variante; }
        set { variante = value; }
    }

    [JsonProperty("year")]
    public int Annee
    {
        get { return annee; }
        set { annee = value; }
    }

    [JsonProperty("powerHp")]
    public int Puissance
    {
        get { return puissance; }
        set { puissance = value; }
    }

    [JsonProperty("rarity")]
    public string Rarete
    {
        get { return rarete; }
        set { rarete = value; }
    }

    [JsonProperty("officialPrice")]
    public decimal PrixOfficiel
    {
        get { return prixOfficiel; }
        set { prixOfficiel = value; }
    }

    [JsonProperty("copiesInCirculation")]
    public int ExemplairesEnCirculation
    {
        get { return exemplairesEnCirculation; }
        set { exemplairesEnCirculation = value; }
    }

    [JsonProperty("imageUrl")]
    public string ImageUrl
    {
        get { return imageUrl; }
        set { imageUrl = value; }
    }

    [JsonProperty("description")]
    public string Description
    {
        get { return description; }
        set { description = value; }
    }

    #endregion

    #region Methodes

    #endregion
}
```

#### `DemandeConnexion.cs`
```csharp
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

public class DemandeConnexion
{
    #region Attributs

    private string identifiant = "";
    private string motDePasse = "";

    #endregion

    #region Constructeurs

    public DemandeConnexion()
    {
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("username")]
    public string Identifiant
    {
        get { return identifiant; }
        set { identifiant = value; }
    }

    [JsonProperty("password")]
    public string MotDePasse
    {
        get { return motDePasse; }
        set { motDePasse = value; }
    }

    #endregion

    #region Methodes

    #endregion
}
```

#### `ReponseConnexion.cs`
```csharp
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

public class ReponseConnexion
{
    #region Attributs

    private string jetonAcces = "";

    #endregion

    #region Constructeurs

    public ReponseConnexion()
    {
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("accessToken")]
    public string JetonAcces
    {
        get { return jetonAcces; }
        set { jetonAcces = value; }
    }

    #endregion

    #region Methodes

    #endregion
}
```

#### `ErreurApi.cs`
```csharp
using Newtonsoft.Json;

namespace AutoMasters.Modeles;

public class ErreurApi
{
    #region Attributs

    private string message = "";

    #endregion

    #region Constructeurs

    public ErreurApi()
    {
    }

    #endregion

    #region Getter/Setter

    [JsonProperty("message")]
    public string Message
    {
        get { return message; }
        set { message = value; }
    }

    #endregion

    #region Methodes

    #endregion
}
```

---

### 3.7 Foundational Single Service (`AutoMasters/Services/ServiceAPI.cs`)

Initializes the singleton HTTP client connected to Group 2 API (`https://api-cartes-g2.fldsio.com/`). Supports an optional `HttpMessageHandler` constructor to enable mock injection during unit tests.

```csharp
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using AutoMasters.Modeles;

namespace AutoMasters.Services;

public class ServiceAPI
{
    #region Attributs

    private static ServiceAPI instance = new ServiceAPI();
    private HttpClient client;

    #endregion

    #region Constructeurs

    private ServiceAPI()
    {
        client = new HttpClient();
        client.BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/");
        client.Timeout = TimeSpan.FromSeconds(15);
    }

    /// <summary>
    /// Constructeur avec gestionnaire HTTP injectable pour les tests unitaires.
    /// </summary>
    public ServiceAPI(HttpMessageHandler gestionnaire)
    {
        client = new HttpClient(gestionnaire);
        client.BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/");
        client.Timeout = TimeSpan.FromSeconds(15);
    }

    #endregion

    #region Getter/Setter

    public static ServiceAPI Instance
    {
        get { return instance; }
    }

    public HttpClient ClientHttp => client;

    #endregion

    #region Methodes

    private async Task<string> LireReponseAsync(HttpResponseMessage reponse)
    {
        string json = await reponse.Content.ReadAsStringAsync();

        if (!reponse.IsSuccessStatusCode)
        {
            ErreurApi? erreur = null;
            try
            {
                erreur = JsonConvert.DeserializeObject<ErreurApi>(json);
            }
            catch (JsonException)
            {
                // Réponse non-JSON du serveur
            }

            if (reponse.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");

            throw new Exception(erreur?.Message ?? "Le serveur ne peut pas traiter la demande.");
        }

        return json;
    }

    public async Task SeConnecterAsync(string identifiant, string motDePasse)
    {
        var demande = new DemandeConnexion
        {
            Identifiant = identifiant,
            MotDePasse = motDePasse
        };

        string json = JsonConvert.SerializeObject(demande);
        using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync("auth/login", contenu);

        string resultat = await LireReponseAsync(reponse);
        var connexion = JsonConvert.DeserializeObject<ReponseConnexion>(resultat);

        if (connexion == null || string.IsNullOrWhiteSpace(connexion.JetonAcces))
            throw new Exception("Le jeton d'accès est absent.");

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", connexion.JetonAcces);
    }

    #endregion
}
```

---

### 3.8 Foundational ViewModel (`AutoMasters/VuesModeles/ConnexionVueModele.cs`)

Implements MVVM with `CommunityToolkit.Mvvm`, `[ObservableProperty]`, and `[RelayCommand]`.

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Services;

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

    public ConnexionVueModele()
    {
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    #endregion

    #region Methodes

    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        if (EstOccupe)
            return;

        if (string.IsNullOrWhiteSpace(Identifiant) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            Message = "Saisissez les deux identifiants.";
            return;
        }

        EstOccupe = true;
        Message = "";
        try
        {
            await serviceApi.SeConnecterAsync(Identifiant.Trim(), MotDePasse);
            MotDePasse = "";
            Message = "Connexion réussie !";
        }
        catch (Exception erreur)
        {
            Message = erreur.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    #endregion
}
```

---

### 3.9 Foundational View (`AutoMasters/Vues/ConnexionPage.xaml` & `.xaml.cs`)

#### `ConnexionPage.xaml`
Matching Figma Mockup 1 aesthetics (dark automotive palette, badge, high-contrast inputs, red action button).

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    x:Class="AutoMasters.Vues.ConnexionPage"
    x:DataType="vm:ConnexionVueModele"
    BackgroundColor="#10080B"
    Title="Connexion">

    <ScrollView>
        <VerticalStackLayout Padding="32,48" Spacing="20" VerticalOptions="Center">
            <!-- Badge Logo -->
            <Border
                Stroke="#E63946"
                StrokeThickness="2"
                WidthRequest="72"
                HeightRequest="72"
                HorizontalOptions="Center"
                StrokeShape="RoundRectangle 16"
                BackgroundColor="#221215">
                <Label
                    Text="🏎️"
                    FontSize="36"
                    HorizontalOptions="Center"
                    VerticalOptions="Center" />
            </Border>

            <!-- Titres -->
            <Label
                Text="AUTO MASTERS"
                FontSize="28"
                FontAttributes="Bold"
                TextColor="#FFFFFF"
                HorizontalTextAlignment="Center" />

            <Label
                Text="COLLECTION AUTOMOBILE EXCLUSIVE"
                FontSize="11"
                TextColor="#E63946"
                CharacterSpacing="2"
                HorizontalTextAlignment="Center" />

            <!-- Formulaire de saisie -->
            <VerticalStackLayout Spacing="12" Margin="0,16,0,0">
                <Border
                    Stroke="#331A20"
                    StrokeThickness="1"
                    BackgroundColor="#1E1014"
                    StrokeShape="RoundRectangle 10"
                    Padding="14,4">
                    <Entry
                        Placeholder="Identifiant / Email"
                        PlaceholderColor="#888888"
                        TextColor="#FFFFFF"
                        Text="{Binding Identifiant}" />
                </Border>

                <Border
                    Stroke="#331A20"
                    StrokeThickness="1"
                    BackgroundColor="#1E1014"
                    StrokeShape="RoundRectangle 10"
                    Padding="14,4">
                    <Entry
                        Placeholder="Mot de passe"
                        PlaceholderColor="#888888"
                        TextColor="#FFFFFF"
                        IsPassword="True"
                        Text="{Binding MotDePasse}" />
                </Border>
            </VerticalStackLayout>

            <!-- Bouton de connexion -->
            <Button
                Text="SE CONNECTER"
                FontAttributes="Bold"
                FontSize="15"
                TextColor="#FFFFFF"
                BackgroundColor="#E63946"
                CornerRadius="10"
                HeightRequest="50"
                IsEnabled="{Binding EstDisponible}"
                Command="{Binding SeConnecterCommand}" />

            <!-- Indicateur de chargement & Message d'erreur -->
            <ActivityIndicator
                Color="#E63946"
                IsRunning="{Binding EstOccupe}"
                IsVisible="{Binding EstOccupe}" />

            <Label
                Text="{Binding Message}"
                TextColor="#E63946"
                FontSize="13"
                HorizontalTextAlignment="Center" />
        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

#### `ConnexionPage.xaml.cs`
**Purity Verification:** Exactly 0 lines of business logic. Code-behind contains only the constructor.

```csharp
using AutoMasters.VuesModeles;

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

---

### 3.10 Windows Platform Files (`AutoMasters/Platforms/Windows/`)

1. **`App.xaml`**:
   ```xml
   <maui:MauiWinUIApplication
       x:Class="AutoMasters.WinUI.App"
       xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
       xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
       xmlns:maui="using:Microsoft.Maui"
       xmlns:local="using:AutoMasters.WinUI">

   </maui:MauiWinUIApplication>
   ```

2. **`App.xaml.cs`**:
   ```csharp
   using Microsoft.UI.Xaml;

   namespace AutoMasters.WinUI;

   /// <summary>
   /// Point de démarrage Windows fourni par le modèle MAUI.
   /// </summary>
   public partial class App : MauiWinUIApplication
   {
       public App()
       {
           this.InitializeComponent();
       }

       protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
   }
   ```

3. **`app.manifest`**:
   Standard Win32 manifest declaring DPI awareness (`PerMonitorV2`) and `longPathAware`. Matches `TestVoiture.git`.

4. **`Package.appxmanifest`**:
   Unpackaged desktop application manifest configuring app identity, square icons, and `runFullTrust` capability. Matches `TestVoiture.git`.

---

### 3.11 Resource Files & Styling Tokens (`AutoMasters/Resources/`)

#### `Resources/Styles/Colors.xaml`
Provides the automotive dark design palette:
- Background: `#10080B`
- Primary Accent: `#E63946`
- Card/Surface Dark: `#221215` / `#1E1014`
- Border Muted: `#331A20`
- Success / Profit: `#10B981`
- Text White: `#FFFFFF`
- Muted Gray: `#888888`

#### `Resources/Styles/Styles.xaml`
MAUI style rules for `Button`, `Entry`, `Label`, `ActivityIndicator`, `Border`, and `ScrollView`.

#### Asset Copy Blueprint:
During Milestone 1 creation, the binary and vector files must be copied directly from the verified reference repository scratch cache:
- From `C:\Users\stent\.gemini\antigravity\brain\29fc683b-b361-4496-a392-ebcaaa323559\scratch\TestVoiture\Resources\`:
  - `AppIcon/appicon.svg`, `AppIcon/appiconfg.svg` -> `AutoMasters/Resources/AppIcon/`
  - `Fonts/OpenSans-Regular.ttf`, `Fonts/OpenSans-Semibold.ttf` -> `AutoMasters/Resources/Fonts/`
  - `Images/dotnet_bot.png` -> `AutoMasters/Resources/Images/`
  - `Splash/splash.svg` -> `AutoMasters/Resources/Splash/`

---

## 4. French Code Region Standards Specification

### 4.1 Strict Canonical Order
In accordance with BTS SIO SLAM 2nd-year academic evaluation criteria (*TP MAUI Recettes Pas à Pas*, Recettes 01–19, and Lab 527), every C# source file must enforce the following four regions in exact sequence:

```csharp
#region Attributs

// Champs privés (backing fields), instances singleton, variables de travail

#endregion

#region Constructeurs

// Constructeurs sans paramètre, avec paramètres ou d'initialisation

#endregion

#region Getter/Setter

// Propriétés publiques avec accesseurs explicites get/set ou calculés

#endregion

#region Methodes

// Méthodes métier, méthodes d'assistance privées, commandes [RelayCommand]

#endregion
```

### 4.2 Layer-by-Layer Rules

| Layer | Region | Permitted Elements & Rules |
|---|---|---|
| **Modeles (`Modeles/`)** | `Attributs` | Private backing fields in French `camelCase` (e.g. `private string identifiant = "";`). Never public. |
| | `Constructeurs` | Public default parameterless constructor. Mandatory presence even if empty. |
| | `Getter/Setter` | Public PascalCase properties (e.g. `public string Identifiant`). Explicit get/set syntax (`get { return ...; } set { ... = value; }`). Annotated with `[JsonProperty("api_field")]`. |
| | `Methodes` | Helper methods (e.g. formatting, calculations) or left empty. Region tags remain intact. |
| **Services (`Services/`)** | `Attributs` | Static singleton instance (`private static ServiceAPI instance`), `private HttpClient client`. |
| | `Constructeurs` | Private default constructor configuring base URL & timeout. Injectable constructor for tests. |
| | `Getter/Setter` | `public static ServiceAPI Instance => instance;`, client accessors. |
| | `Methodes` | All private helpers (`LireReponseAsync`) and public asynchronous API methods (`SeConnecterAsync`, `RecupererCatalogueAsync`, etc.). |
| **VuesModeles (`VuesModeles/`)** | `Attributs` | Service references, `[ObservableProperty]` private fields (e.g. `private string message = "";`). |
| | `Constructeurs` | Public constructors initializing defaults or dependency bindings. |
| | `Getter/Setter` | Computed read-only properties (e.g. `public bool EstDisponible => !EstOccupe;`). |
| | `Methodes` | `[RelayCommand]` asynchronous actions and private helpers. |
| **Vues (`Vues/` .xaml.cs)** | `Constructeurs` | Public constructor executing `InitializeComponent();` and setting `BindingContext`. |
| | *Purity Rule* | **ZERO business logic allowed.** No API calls, no event listeners performing calculations. |
| **App & MauiProgram** | `Constructeurs` / `Methodes` | `App()` calling `InitializeComponent()`, `CreateWindow()` creating `Window(new AppShell())`. `MauiProgram` contains `CreateMauiApp()`. |

---

## 5. Milestone 1 Implementation Action Plan

To implement Milestone 1 cleanly:

1. **Step 1: Directory Structure Creation**
   Execute filesystem directory creation for:
   - `AutoMasters/Modeles`
   - `AutoMasters/Services`
   - `AutoMasters/VuesModeles`
   - `AutoMasters/Vues`
   - `AutoMasters/Platforms/Windows`
   - `AutoMasters/Properties`
   - `AutoMasters/Resources/AppIcon`
   - `AutoMasters/Resources/Fonts`
   - `AutoMasters/Resources/Images`
   - `AutoMasters/Resources/Raw`
   - `AutoMasters/Resources/Splash`
   - `AutoMasters/Resources/Styles`

2. **Step 2: Copy Binary & Font Assets**
   Copy `.ttf` and `.svg` files from the scratch reference folder:
   ```powershell
   Copy-Item "C:\Users\stent\.gemini\antigravity\brain\29fc683b-b361-4496-a392-ebcaaa323559\scratch\TestVoiture\Resources\*" "c:\Users\stent\source\repos\AutoMasters\AutoMasters\Resources\" -Recurse -Force
   ```

3. **Step 3: Deploy Styling & Configuration Dictionaries**
   Write `Colors.xaml` and `Styles.xaml` into `AutoMasters/Resources/Styles/`.

4. **Step 4: Deploy Platform Manifests**
   Write `App.xaml`, `App.xaml.cs`, `app.manifest`, and `Package.appxmanifest` into `AutoMasters/Platforms/Windows/`.

5. **Step 5: Deploy Foundational C# and XAML Code**
   Write:
   - `AutoMasters/App.xaml` & `AutoMasters/App.xaml.cs`
   - `AutoMasters/AppShell.xaml` & `AutoMasters/AppShell.xaml.cs`
   - `AutoMasters/MauiProgram.cs`
   - `AutoMasters/Services/ServiceAPI.cs`
   - `AutoMasters/Modeles/Carte.cs`, `DemandeConnexion.cs`, `ReponseConnexion.cs`, `ErreurApi.cs`
   - `AutoMasters/VuesModeles/ConnexionVueModele.cs`
   - `AutoMasters/Vues/ConnexionPage.xaml` & `ConnexionPage.xaml.cs`

6. **Step 6: Solution and Project Linking**
   Together with `explorer_m1_1`'s `.csproj` and `.slnx` definitions, run:
   ```powershell
   dotnet build c:\Users\stent\source\repos\AutoMasters\AutoMasters.slnx -c Debug
   ```
   Verify 0 errors.

---

## 6. Verification and Validation Checklist

- [x] Exact folder names verified against `TestVoiture.git` (`Modeles`, `Services`, `VuesModeles`, `Vues`, `Platforms/Windows`, `Resources`).
- [x] `Services/` verified to contain ONLY `ServiceAPI.cs`.
- [x] All C# classes adhere to `#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`.
- [x] Code-behind purity confirmed (zero business logic in `.xaml.cs`).
- [x] All models feature explicit backing fields, explicit get/set accessors, and `[JsonProperty]` mappings.
- [x] AppShell navigation architecture supports both M1 login and M3 5-tab expansion.
- [x] Asset copying path verified on the host filesystem.
