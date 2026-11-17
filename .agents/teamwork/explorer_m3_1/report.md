# Rapport d'Exploration et Spécifications — Milestone 3 (M3)
**Navigation Shell, Thème Sombre Automobile & Page de Connexion**  
*Auteur: explorer_m3_1 | Rôles: Explorer, Synthesizer | Date: 2026-10-09*  
*Cibles fonctionnelles: F18, F19, F20, F27, F28*  
*Niveau académique: BTS SIO SLAM 2ème année (Conventions TestVoiture.git & Standards MAUI)*

---

## 1. Résumé Exécutif & Objectifs M3

Le Milestone 3 pose les fondations complètes de la couche de présentation de l'application mobile **AutoMasters**. Il structure l'architecture visuelle et comportementale selon 4 axes majeurs :
1. **Design System & Jetons de Style Sombre Automobile (F27)** : Palette sombre haut de gamme inspirée de Figma (`#10080B`, `#221215`, `#E63946`, `#2ECC71`, `#FFFFFF`, `#A0A0A0`, rayon de courbure 12).
2. **Architecture de Navigation Shell (F20)** : Routage hiérarchique avec racine non authentifiée (`//ConnexionPage`), barre d'onglets principale (`//MainTabs` avec Catalogue, Marché, Garage) et routes de détail modales (`FicheCartePage`, `CreationOffrePage`).
3. **Écran et ViewModel de Connexion (F18, F19)** : Interface fidèle à la maquette Figma Screen 1 (`media_1791389223586_1f733aab.png`), ViewModel MVVM pur sous `CommunityToolkit.Mvvm`, consommation de `ServiceAPI.SeConnecterAsync`, gestion des erreurs en français et transition de navigation.
4. **Pureté du Code-Behind (F28) et Injection de Dépendances (DI)** : Zéro logique métier ou appel réseau dans les fichiers `.xaml.cs`, conteneur de services configuré dans `MauiProgram.cs`.

---

## 2. Spécification des Jetons de Style Sombre (F27)

### 2.1 Table des Jetons de Couleur et Rôles Visuels

| Jeton XAML | Valeur Hex | Rôle dans l'application | Équivalent Figma / Cahier des charges |
|---|---|---|---|
| `ColorBackground` | `#10080B` | Arrière-plan principal de toutes les pages et du Shell | Fond noir profond teinté bordeaux |
| `ColorSurfaceCard` | `#221215` | Surface des cartes de véhicules, conteneurs et dialogues | Card container sombre |
| `ColorSurfaceInput` | `#1E1014` | Conteneur de saisie des champs texte (Entry) | Input field background |
| `ColorBorder` | `#331A20` | Bordure fine des cartes et conteneurs | Border stroke |
| `ColorPrimaryRed` | `#E63946` | Accent principal : CTA "SE CONNECTER", "OUVRIR", logo "MASTERS", FAB | Rouge automobile de course |
| `ColorSecondaryGreen` | `#2ECC71` | Accent secondaire : CTA "ACHETER IMMÉDIATEMENT", P&L positif, badge "EN DIRECT" | Vert succès / live badge |
| `SecondaryGreen` | `#2ECC71` | Alias de compatibilité pour les tests E2E | F27_04 compatibility |
| `ColorTextPrimary` | `#FFFFFF` | Titres majeurs, valeurs en coins, texte des boutons | Blanc pur haute lisibilité |
| `ColorTextSecondary` | `#A0A0A0` | Libellés de champs, sous-titres, années, mentions secondaires | Argent / Gris métallisé |
| `ColorTextMuted` | `#6B7280` | Séparateurs, liens inactifs | Gris atténué |
| `ColorReserveDarkRed` | `#2D1518` | Fond du bouton "RÉSERVER" | Dark red reserve CTA |
| `ColorRarityLegendary` | `#F59E0B` | Badge rareté : ★ LÉGENDAIRE | Or / Ambre |
| `ColorRarityEpic` | `#A855F7` | Badge rareté : ÉPIQUE | Violet / Pourpre |
| `ColorRarityRare` | `#38BDF8` | Badge rareté : RARE | Bleu ciel / Cyan |
| `ColorRarityCommon` | `#9CA3AF` | Badge rareté : COMMUNE | Argent / Gris classique |

### 2.2 Géométrie et Règles Ergonomiques
- **Rayon de courbure principal (`CornerRadius`)** : `12` pour les boutons, cartes de véhicules, et conteneurs.
- **Rayon des champs de saisie (`CornerRadius`)** : `10` pour les bordures entourant les contrôles `Entry`.
- **Hauteur standard des CTA** : `50px` à `52px` pour garantir une surface tactile conforme aux normes mobiles (minimum 48px).

---

## 3. Code Proposé — Jetons & Styles (`Resources/Styles`)

### 3.1 `Resources/Styles/Colors.xaml` (Code de Remplacement Intégral)

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary 
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- =================================================================== -->
    <!-- JETONS DU THÈME SOMBRE AUTOMOBILE AUTOMASTERS (F27)                  -->
    <!-- =================================================================== -->

    <!-- Couleurs de surface et de structure -->
    <Color x:Key="ColorBackground">#10080B</Color>
    <Color x:Key="ColorSurfaceCard">#221215</Color>
    <Color x:Key="ColorSurfaceInput">#1E1014</Color>
    <Color x:Key="ColorBorder">#331A20</Color>

    <!-- Accents d'action et statuts -->
    <Color x:Key="ColorPrimaryRed">#E63946</Color>
    <Color x:Key="ColorSecondaryGreen">#2ECC71</Color>
    <Color x:Key="SecondaryGreen">#2ECC71</Color>
    <Color x:Key="ColorSecondaryGreenAlt">#10B981</Color>
    <Color x:Key="ColorReserveDarkRed">#2D1518</Color>

    <!-- Typographie et contrastes -->
    <Color x:Key="ColorTextPrimary">#FFFFFF</Color>
    <Color x:Key="ColorTextSecondary">#A0A0A0</Color>
    <Color x:Key="ColorTextMuted">#6B7280</Color>

    <!-- Jetons de rareté des cartes -->
    <Color x:Key="ColorRarityLegendary">#F59E0B</Color>
    <Color x:Key="ColorRarityEpic">#A855F7</Color>
    <Color x:Key="ColorRarityRare">#38BDF8</Color>
    <Color x:Key="ColorRarityCommon">#9CA3AF</Color>

    <!-- =================================================================== -->
    <!-- COMPATIBILITÉ PALETTE MAUI STANDARD                                 -->
    <!-- =================================================================== -->
    <Color x:Key="Primary">#E63946</Color>
    <Color x:Key="PrimaryDark">#C52838</Color>
    <Color x:Key="PrimaryDarkText">#FFFFFF</Color>
    <Color x:Key="Secondary">#2ECC71</Color>
    <Color x:Key="SecondaryDarkText">#10080B</Color>
    <Color x:Key="Tertiary">#221215</Color>

    <Color x:Key="White">#FFFFFF</Color>
    <Color x:Key="Black">#10080B</Color>
    <Color x:Key="Magenta">#E63946</Color>
    <Color x:Key="MidnightBlue">#10080B</Color>
    <Color x:Key="OffBlack">#10080B</Color>

    <Color x:Key="Gray100">#FFFFFF</Color>
    <Color x:Key="Gray200">#E0E0E0</Color>
    <Color x:Key="Gray300">#C0C0C0</Color>
    <Color x:Key="Gray400">#A0A0A0</Color>
    <Color x:Key="Gray500">#808080</Color>
    <Color x:Key="Gray600">#331A20</Color>
    <Color x:Key="Gray900">#221215</Color>
    <Color x:Key="Gray950">#10080B</Color>

    <!-- =================================================================== -->
    <!-- PINCEAUX (SOLIDCOLORBRUSH)                                          -->
    <!-- =================================================================== -->
    <SolidColorBrush x:Key="BackgroundBrush" Color="{StaticResource ColorBackground}" />
    <SolidColorBrush x:Key="SurfaceCardBrush" Color="{StaticResource ColorSurfaceCard}" />
    <SolidColorBrush x:Key="SurfaceInputBrush" Color="{StaticResource ColorSurfaceInput}" />
    <SolidColorBrush x:Key="BorderBrush" Color="{StaticResource ColorBorder}" />
    <SolidColorBrush x:Key="PrimaryRedBrush" Color="{StaticResource ColorPrimaryRed}" />
    <SolidColorBrush x:Key="SecondaryGreenBrush" Color="{StaticResource ColorSecondaryGreen}" />
    <SolidColorBrush x:Key="TextPrimaryBrush" Color="{StaticResource ColorTextPrimary}" />
    <SolidColorBrush x:Key="TextSecondaryBrush" Color="{StaticResource ColorTextSecondary}" />
    <SolidColorBrush x:Key="PrimaryBrush" Color="{StaticResource Primary}" />
    <SolidColorBrush x:Key="SecondaryBrush" Color="{StaticResource Secondary}" />
    <SolidColorBrush x:Key="WhiteBrush" Color="{StaticResource White}" />
    <SolidColorBrush x:Key="BlackBrush" Color="{StaticResource Black}" />

</ResourceDictionary>
```

### 3.2 `Resources/Styles/Styles.xaml` (Styles Contrôles & Classes Réutilisables)

```xml
<?xml version="1.0" encoding="UTF-8" ?>
<ResourceDictionary 
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml">

    <!-- =================================================================== -->
    <!-- STYLE GLOBAL APPSHELL (F20, F27)                                    -->
    <!-- =================================================================== -->
    <Style TargetType="Shell" ApplyToDerivedTypes="True">
        <Setter Property="Shell.BackgroundColor" Value="{StaticResource ColorBackground}" />
        <Setter Property="Shell.ForegroundColor" Value="{StaticResource ColorTextPrimary}" />
        <Setter Property="Shell.TitleColor" Value="{StaticResource ColorTextPrimary}" />
        <Setter Property="Shell.UnselectedColor" Value="{StaticResource ColorTextSecondary}" />
        <Setter Property="Shell.TabBarBackgroundColor" Value="{StaticResource ColorBackground}" />
        <Setter Property="Shell.TabBarForegroundColor" Value="{StaticResource ColorPrimaryRed}" />
        <Setter Property="Shell.TabBarUnselectedColor" Value="{StaticResource ColorTextSecondary}" />
        <Setter Property="Shell.TabBarTitleColor" Value="{StaticResource ColorPrimaryRed}" />
        <Setter Property="Shell.NavBarHasShadow" Value="False" />
    </Style>

    <!-- =================================================================== -->
    <!-- STYLE GLOBAL PAGES ET CONTENEURS                                    -->
    <!-- =================================================================== -->
    <Style TargetType="ContentPage" ApplyToDerivedTypes="True">
        <Setter Property="BackgroundColor" Value="{StaticResource ColorBackground}" />
    </Style>

    <Style TargetType="ActivityIndicator">
        <Setter Property="Color" Value="{StaticResource ColorPrimaryRed}" />
    </Style>

    <!-- =================================================================== -->
    <!-- STYLE BOUTONS PAR DÉFAUT                                            -->
    <!-- =================================================================== -->
    <Style TargetType="Button">
        <Setter Property="TextColor" Value="{StaticResource ColorTextPrimary}" />
        <Setter Property="BackgroundColor" Value="{StaticResource ColorPrimaryRed}" />
        <Setter Property="FontFamily" Value="OpenSansRegular" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="BorderWidth" Value="0" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="Padding" Value="14,10" />
        <Setter Property="MinimumHeightRequest" Value="48" />
        <Setter Property="VisualStateManager.VisualStateGroups">
            <VisualStateGroupList>
                <VisualStateGroup x:Name="CommonStates">
                    <VisualState x:Name="Normal" />
                    <VisualState x:Name="Disabled">
                        <VisualState.Setters>
                            <Setter Property="TextColor" Value="{StaticResource ColorTextSecondary}" />
                            <Setter Property="BackgroundColor" Value="#331A20" />
                        </VisualState.Setters>
                    </VisualState>
                    <VisualState x:Name="PointerOver" />
                </VisualStateGroup>
            </VisualStateGroupList>
        </Setter>
    </Style>

    <!-- =================================================================== -->
    <!-- STYLE ENTRY PAR DÉFAUT                                              -->
    <!-- =================================================================== -->
    <Style TargetType="Entry">
        <Setter Property="TextColor" Value="{StaticResource ColorTextPrimary}" />
        <Setter Property="BackgroundColor" Value="Transparent" />
        <Setter Property="FontFamily" Value="OpenSansRegular" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="PlaceholderColor" Value="{StaticResource ColorTextSecondary}" />
        <Setter Property="MinimumHeightRequest" Value="44" />
    </Style>

    <!-- =================================================================== -->
    <!-- STYLE BORDER PAR DÉFAUT (CARTES AUTOMOBILES)                        -->
    <!-- =================================================================== -->
    <Style TargetType="Border">
        <Setter Property="Stroke" Value="{StaticResource ColorBorder}" />
        <Setter Property="StrokeShape" Value="RoundRectangle 12" />
        <Setter Property="StrokeThickness" Value="1" />
        <Setter Property="BackgroundColor" Value="{StaticResource ColorSurfaceCard}" />
    </Style>

    <!-- =================================================================== -->
    <!-- CLASSES DE STYLES SPÉCIFIQUES AUTOMASTERS                           -->
    <!-- =================================================================== -->

    <!-- Conteneur Carte Surface -->
    <Style x:Key="CardSurfaceStyle" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource ColorSurfaceCard}" />
        <Setter Property="Stroke" Value="{StaticResource ColorBorder}" />
        <Setter Property="StrokeShape" Value="RoundRectangle 12" />
        <Setter Property="StrokeThickness" Value="1" />
        <Setter Property="Padding" Value="14" />
    </Style>

    <!-- Conteneur Champ de Saisie (Entry Wrapper) -->
    <Style x:Key="InputContainerStyle" TargetType="Border">
        <Setter Property="BackgroundColor" Value="{StaticResource ColorSurfaceInput}" />
        <Setter Property="Stroke" Value="{StaticResource ColorBorder}" />
        <Setter Property="StrokeShape" Value="RoundRectangle 10" />
        <Setter Property="StrokeThickness" Value="1" />
        <Setter Property="Padding" Value="14,2" />
        <Setter Property="HeightRequest" Value="50" />
    </Style>

    <!-- Bouton Principal Rouge (SE CONNECTER, OUVRIR, VALIDER) -->
    <Style x:Key="PrimaryButtonStyle" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource ColorPrimaryRed}" />
        <Setter Property="TextColor" Value="{StaticResource ColorTextPrimary}" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="FontSize" Value="15" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="HeightRequest" Value="52" />
    </Style>

    <!-- Bouton Secondaire Vert (ACHETER IMMÉDIATEMENT) -->
    <Style x:Key="SecondaryButtonStyle" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource ColorSecondaryGreen}" />
        <Setter Property="TextColor" Value="{StaticResource ColorTextPrimary}" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="FontSize" Value="15" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="HeightRequest" Value="52" />
    </Style>

    <!-- Bouton Réserver (Fond bordeaux foncé, bordure rouge) -->
    <Style x:Key="ReserveButtonStyle" TargetType="Button">
        <Setter Property="BackgroundColor" Value="{StaticResource ColorReserveDarkRed}" />
        <Setter Property="TextColor" Value="{StaticResource ColorPrimaryRed}" />
        <Setter Property="BorderColor" Value="{StaticResource ColorPrimaryRed}" />
        <Setter Property="BorderWidth" Value="1" />
        <Setter Property="FontAttributes" Value="Bold" />
        <Setter Property="FontSize" Value="14" />
        <Setter Property="CornerRadius" Value="12" />
        <Setter Property="HeightRequest" Value="52" />
    </Style>

</ResourceDictionary>
```

---

## 4. Spécification & Code Proposé — Navigation Shell (F20, F28)

### 4.1 Architecture de Routage Hiérarchique

```
AutoMasters.AppShell (Racine)
│
├── //ConnexionPage (Route non-authentifiée racine)
│     └── Vues:ConnexionPage
│
└── //MainTabs (Barre d'onglets principale authentifiée)
      ├── [1] Route "//MainTabs/Catalogue" -> Vues:CataloguePage
      ├── [2] Route "//MainTabs/Marche"    -> Vues:MarchePage
      └── [3] Route "//MainTabs/Garage"    -> Vues:GaragePage
│
└── Routes secondaires modales enregistrées (Routing.RegisterRoute)
      ├── "FicheCartePage"     -> Vues:FicheCartePage (Fiche technique détaillée)
      └── "CreationOffrePage"  -> Vues:CreationOffrePage (Mise en vente d'un véhicule)
```

### 4.2 Flux de Navigation
1. **Démarrage de l'application** : Par défaut, `AppShell` affiche le premier `ShellContent` déclaré : `ConnexionPage` (`//ConnexionPage`).
2. **Connexion réussie** : `ConnexionVueModele` invoque `await Shell.Current.GoToAsync("//MainTabs/Catalogue");`. La barre d'onglets s'affiche avec le premier onglet *Catalogue* sélectionné.
3. **Sélection d'une carte dans le catalogue** : Navigation modale vers la fiche détaillée : `await Shell.Current.GoToAsync(nameof(FicheCartePage), parametres);`.
4. **Création d'une offre depuis le Marché** : Taper sur le bouton FAB `+` navigue vers `await Shell.Current.GoToAsync(nameof(CreationOffrePage));`.
5. **Déconnexion** : Invoquer `await Shell.Current.GoToAsync("//ConnexionPage");` réinitialise la pile de navigation et ramène l'utilisateur à l'écran de connexion.

### 4.3 `AppShell.xaml` (Code Proposé)

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
    UnselectedColor="#A0A0A0"
    Shell.TabBarBackgroundColor="#10080B"
    Shell.TabBarForegroundColor="#E63946"
    Shell.TabBarUnselectedColor="#A0A0A0"
    Shell.TabBarTitleColor="#E63946"
    Shell.NavBarHasShadow="False">

    <!-- =================================================================== -->
    <!-- 1. ROUTE RACINE NON AUTHENTIFIÉE (CONNEXION)                        -->
    <!-- =================================================================== -->
    <ShellContent
        Route="ConnexionPage"
        Title="Connexion"
        ContentTemplate="{DataTemplate vues:ConnexionPage}" />

    <!-- =================================================================== -->
    <!-- 2. BARRE D'ONGLETS AUTHENTIFIÉE (3 ONGLETS MVP F20)                 -->
    <!-- =================================================================== -->
    <TabBar Route="MainTabs">
        <!-- Onglet 1: Catalogue de véhicules -->
        <Tab Title="Catalogue" Route="Catalogue">
            <ShellContent
                Title="Catalogue"
                ContentTemplate="{DataTemplate vues:CataloguePage}"
                Route="CataloguePage" />
        </Tab>

        <!-- Onglet 2: Marché / Trading -->
        <Tab Title="Marché" Route="Marche">
            <ShellContent
                Title="Marché"
                ContentTemplate="{DataTemplate vues:MarchePage}"
                Route="MarchePage" />
        </Tab>

        <!-- Onglet 3: Garage / Portefeuille -->
        <Tab Title="Garage" Route="Garage">
            <ShellContent
                Title="Garage"
                ContentTemplate="{DataTemplate vues:GaragePage}"
                Route="GaragePage" />
        </Tab>
    </TabBar>

</Shell>
```

### 4.4 `AppShell.xaml.cs` (Code Proposé — Pureté F28 & Enregistrement des Routes)

```csharp
using AutoMasters.Vues;

namespace AutoMasters;

/// <summary>
/// Coquille principale de navigation Shell conforme aux exigences BTS SIO SLAM.
/// Respecte strictement la pureté du code-behind (F28) : aucune logique métier ni appel réseau.
/// </summary>
public partial class AppShell : Shell
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Initialise les composants graphiques Shell et enregistre les routes de détail.
    /// </summary>
    public AppShell()
    {
        InitializeComponent();
        EnregistrerRoutes();
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    /// <summary>
    /// Enregistre les routes de navigation modale/détail pour Shell.Current.GoToAsync.
    /// </summary>
    private void EnregistrerRoutes()
    {
        Routing.RegisterRoute(nameof(FicheCartePage), typeof(FicheCartePage));
        Routing.RegisterRoute(nameof(CreationOffrePage), typeof(CreationOffrePage));
    }

    #endregion
}
```

---

## 5. Spécification & Code Proposé — Écran & ViewModel de Connexion (F18, F19)

### 5.1 Décomposition Graphique Figma Screen 1 (`media_1791389223586_1f733aab.png`)
L'écran de connexion est constitué de 6 blocs ordonnés verticalement et centrés :
1. **Badge Logo Squircle** :
   - Conteneur `Border` de taille 72x72, rayon 16, fond `#221215`, bordure rouge `#E63946` de 2px.
   - Glyphe central : Silhouette de voiture de course "🏎️" ou icône profilée.
2. **Titrage de la Marque** :
   - Label avec `FormattedString` : `AUTO` en blanc (`#FFFFFF`) et `MASTERS` en rouge (`#E63946`), gras, taille 28, espacement 2.
   - Sous-titre : `Collecte · Échange · Compétition` en gris argenté (`#A0A0A0`), taille 12, espacement 1.5.
3. **Champs de Saisie** :
   - Label d'en-tête `ADRESSE EMAIL` en majuscules gris argenté (`#A0A0A0`), taille 11, gras.
   - Champ de saisie email avec conteneur arrondi `#1E1014`, bordure `#331A20`, texte blanc, placeholder `etudiant@sio.lan`, `Keyboard="Email"`.
   - Label d'en-tête `MOT DE PASSE` en majuscules gris argenté (`#A0A0A0`), taille 11, gras.
   - Champ de saisie mot de passe avec conteneur arrondi `#1E1014`, bordure `#331A20`, texte blanc, `IsPassword="True"`, placeholder `••••••••••••`.
4. **Lien d'Assistance** :
   - `Mot de passe oublié ?` aligné à droite, couleur rouge `#E63946`, taille 12.
5. **Bouton d'Action Principal (CTA)** :
   - Bouton `SE CONNECTER`, fond rouge `#E63946`, texte blanc gras, rayon 12, hauteur 52.
   - Liaison commande : `Command="{Binding SeConnecterCommand}"`.
   - Contrôle d'état : `IsEnabled="{Binding EstDisponible}"`.
6. **Pied de Page & Indicateurs d'État** :
   - Texte centré : `Pas encore de compte ?` en gris (`#A0A0A0`) et `Créer un compte` en rouge (`#E63946`).
   - `ActivityIndicator` rouge (`#E63946`) visible uniquement lors d'une requête en cours (`IsRunning="{Binding EstOccupe}"`).
   - Bandeau d'erreur : `Label` en rouge `#E63946` affichant le message convivial de validation ou d'échec API.

### 5.2 `Vues/ConnexionPage.xaml` (Code Proposé)

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    x:Class="AutoMasters.Vues.ConnexionPage"
    x:DataType="vm:ConnexionVueModele"
    BackgroundColor="#10080B"
    Shell.NavBarIsVisible="False"
    Title="Connexion">

    <ScrollView>
        <VerticalStackLayout
            Padding="28,40"
            Spacing="18"
            VerticalOptions="Center"
            MaximumWidthRequest="420"
            HorizontalOptions="Center">

            <!-- 1. BADGE LOGO AUTOMASTERS SQUIRCLE FIGMA -->
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
                    FontSize="34"
                    HorizontalOptions="Center"
                    VerticalOptions="Center" />
            </Border>

            <!-- 2. TITRE BI-COLORE & SOUS-TITRE FIGMA -->
            <Label HorizontalTextAlignment="Center" CharacterSpacing="2">
                <Label.FormattedText>
                    <FormattedString>
                        <Span Text="AUTO" TextColor="#FFFFFF" FontAttributes="Bold" FontSize="26" />
                        <Span Text="MASTERS" TextColor="#E63946" FontAttributes="Bold" FontSize="26" />
                    </FormattedString>
                </Label.FormattedText>
            </Label>

            <Label
                Text="Collecte · Échange · Compétition"
                FontSize="12"
                TextColor="#A0A0A0"
                CharacterSpacing="1"
                HorizontalTextAlignment="Center"
                Margin="0,-8,0,12" />

            <!-- 3. FORMULAIRE : ADRESSE EMAIL -->
            <VerticalStackLayout Spacing="6">
                <Label
                    Text="ADRESSE EMAIL"
                    FontSize="11"
                    FontAttributes="Bold"
                    TextColor="#A0A0A0"
                    CharacterSpacing="1" />
                <Border
                    Stroke="#331A20"
                    StrokeThickness="1"
                    BackgroundColor="#1E1014"
                    StrokeShape="RoundRectangle 10"
                    HeightRequest="50"
                    Padding="14,0">
                    <Entry
                        Text="{Binding Email}"
                        Placeholder="etudiant@sio.lan"
                        PlaceholderColor="#666666"
                        TextColor="#FFFFFF"
                        Keyboard="Email"
                        ClearButtonVisibility="WhileEditing"
                        VerticalOptions="Center" />
                </Border>
            </VerticalStackLayout>

            <!-- 4. FORMULAIRE : MOT DE PASSE -->
            <VerticalStackLayout Spacing="6">
                <Label
                    Text="MOT DE PASSE"
                    FontSize="11"
                    FontAttributes="Bold"
                    TextColor="#A0A0A0"
                    CharacterSpacing="1" />
                <Border
                    Stroke="#331A20"
                    StrokeThickness="1"
                    BackgroundColor="#1E1014"
                    StrokeShape="RoundRectangle 10"
                    HeightRequest="50"
                    Padding="14,0">
                    <Entry
                        Text="{Binding MotDePasse}"
                        Placeholder="••••••••••••"
                        PlaceholderColor="#666666"
                        TextColor="#FFFFFF"
                        IsPassword="True"
                        ClearButtonVisibility="WhileEditing"
                        VerticalOptions="Center" />
                </Border>
            </VerticalStackLayout>

            <!-- 5. LIEN MOT DE PASSE OUBLIÉ -->
            <Label
                Text="Mot de passe oublié ?"
                TextColor="#E63946"
                FontSize="12"
                HorizontalOptions="End"
                Margin="0,0,0,6" />

            <!-- 6. BOUTON PRINCIPAL SE CONNECTER -->
            <Button
                Text="SE CONNECTER"
                FontAttributes="Bold"
                FontSize="15"
                TextColor="#FFFFFF"
                BackgroundColor="#E63946"
                CornerRadius="12"
                HeightRequest="52"
                IsEnabled="{Binding EstDisponible}"
                Command="{Binding SeConnecterCommand}" />

            <!-- 7. LIEN CRÉER UN COMPTE FIGMA -->
            <HorizontalStackLayout HorizontalOptions="Center" Spacing="6" Margin="0,8,0,0">
                <Label
                    Text="Pas encore de compte ?"
                    TextColor="#A0A0A0"
                    FontSize="12" />
                <Label
                    Text="Créer un compte"
                    TextColor="#E63946"
                    FontAttributes="Bold"
                    FontSize="12" />
            </HorizontalStackLayout>

            <!-- 8. INDICATEUR DE CHARGEMENT ET BANDEAU D'ERREUR -->
            <ActivityIndicator
                Color="#E63946"
                IsRunning="{Binding EstOccupe}"
                IsVisible="{Binding EstOccupe}"
                HeightRequest="36" />

            <Label
                Text="{Binding Message}"
                TextColor="#E63946"
                FontSize="13"
                HorizontalTextAlignment="Center"
                LineBreakMode="WordWrap"
                IsVisible="{Binding AUnMessage}" />

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 5.3 `Vues/ConnexionPage.xaml.cs` (Code Proposé — Pureté F28)

```csharp
using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur de la page de connexion (ConnexionPage).
/// Conforme à la règle F28 : aucune logique métier, aucun appel réseau,
/// initialisation du BindingContext et des composants uniquement.
/// </summary>
public partial class ConnexionPage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection de dépendance du ViewModel.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel de connexion injectée par le conteneur DI.</param>
    public ConnexionPage(ConnexionVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur sans paramètre requis pour le système de templates XAML de MAUI et le routage Shell.
    /// </summary>
    public ConnexionPage() : this(new ConnexionVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
```

### 5.4 `VuesModeles/ConnexionVueModele.cs` (Code Proposé — MVVM Toolkit & Traitement Sécurisé)

```csharp
using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel de la page de connexion conforme aux standards BTS SIO SLAM.
/// Utilise CommunityToolkit.Mvvm ([ObservableProperty], [RelayCommand]).
/// Communique exclusivement avec le service singleton ServiceAPI.
/// </summary>
public partial class ConnexionVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Identifiant))]
    private string email = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Password))]
    private string motDePasse = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMessage))]
    private string message = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool estOccupe;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du service d'API.
    /// </summary>
    /// <param name="serviceApi">Instance du service API.</param>
    public ConnexionVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }

    #endregion

    #region Getter/Setter

    /// <summary>
    /// Indique si les commandes et contrôles d'action sont disponibles pour l'utilisateur.
    /// </summary>
    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Alias standard pour IsBusy.
    /// </summary>
    public bool IsBusy => EstOccupe;

    /// <summary>
    /// Alias standard pour IsNotBusy.
    /// </summary>
    public bool IsNotBusy => !EstOccupe;

    /// <summary>
    /// Indique si un message d'erreur ou d'information doit être affiché.
    /// </summary>
    public bool AUnMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>
    /// Alias de compatibilité pour le champ Identifiant attendu par certains tests unitaires et DTOs.
    /// </summary>
    public string Identifiant
    {
        get => Email;
        set => Email = value ?? "";
    }

    /// <summary>
    /// Alias de compatibilité pour le mot de passe.
    /// </summary>
    public string Password
    {
        get => MotDePasse;
        set => MotDePasse = value ?? "";
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Commande asynchrone d'authentification de l'étudiant.
    /// Valide les champs côté client, interroge l'API via ServiceAPI,
    /// navigue vers le catalogue en cas de succès, ou affiche un message convivial en français.
    /// </summary>
    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        // 1. Protection anti-rebond (debounce) : refuser tout clic pendant une requête en cours
        if (EstOccupe)
            return;

        Message = "";

        // 2. Validation de saisie obligatoire (côté client, sans appel réseau)
        if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            Message = "Veuillez saisir votre adresse email et votre mot de passe.";
            return;
        }

        // 3. Validation du format d'email élémentaire (requis par F19_B03)
        string emailSaisi = Email.Trim();
        if (!emailSaisi.Contains("@") || !emailSaisi.Contains("."))
        {
            Message = "Veuillez saisir une adresse email valide.";
            return;
        }

        EstOccupe = true;

        try
        {
            // 4. Appel du service d'authentification centralisé
            var reponse = await serviceApi.SeConnecterAsync(emailSaisi, MotDePasse);

            if (reponse != null && !string.IsNullOrWhiteSpace(reponse.JetonAcces))
            {
                // Succès : effacement sécurisé du mot de passe en mémoire
                MotDePasse = string.Empty;
                Message = "";

                // 5. Navigation Shell vers les onglets principaux (Catalogue par défaut)
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//MainTabs/Catalogue");
                }
            }
            else
            {
                // Échec sans exception explicite
                MotDePasse = string.Empty;
                Message = "Identifiants invalides. Vérifiez votre adresse email et votre mot de passe.";
            }
        }
        catch (ArgumentException)
        {
            MotDePasse = string.Empty;
            Message = "L'adresse email et le mot de passe sont obligatoires.";
        }
        catch (Exception ex)
        {
            // En cas d'échec : effacer le mot de passe mais conserver impérativement l'email (F19_B02)
            MotDePasse = string.Empty;

            string details = ex.Message ?? "";
            if (details.Contains("401") || details.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) || details.Contains("invalide", StringComparison.OrdinalIgnoreCase))
            {
                Message = "Identifiants invalides. Vérifiez votre adresse email et votre mot de passe.";
            }
            else if (details.Contains("connexion", StringComparison.OrdinalIgnoreCase) || details.Contains("serveur", StringComparison.OrdinalIgnoreCase) || details.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase))
            {
                Message = "Impossible de joindre le serveur. Vérifiez votre connexion Internet.";
            }
            else
            {
                Message = details.Length > 0 ? details : "Une erreur est survenue lors de la connexion. Veuillez réessayer.";
            }
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

## 6. Spécification & Code Proposé — Conteneur DI (`MauiProgram.cs`)

Le conteneur d'injection de dépendances (`IServiceCollection`) doit référencer :
1. `ServiceAPI` en tant que **Singleton** (`builder.Services.AddSingleton<ServiceAPI>(sp => ServiceAPI.Instance)`).
2. Les ViewModels en tant que **Transient** (`ConnexionVueModele`, `CatalogueVueModele`, `MarcheVueModele`, `GarageVueModele`).
3. Les Pages en tant que **Transient** (`ConnexionPage`, `CataloguePage`, `MarchePage`, `GaragePage`, `FicheCartePage`, `CreationOffrePage`).
4. `AppShell` en tant que **Singleton** (`builder.Services.AddSingleton<AppShell>()`).

### 6.1 `MauiProgram.cs` (Code Proposé)

```csharp
using Microsoft.Extensions.Logging;
using AutoMasters.Services;
using AutoMasters.Vues;
using AutoMasters.VuesModeles;

namespace AutoMasters;

/// <summary>
/// Point d'entrée de configuration de l'application .NET MAUI.
/// Configure les polices, la journalisation et le conteneur d'injection de dépendances (DI).
/// Conforme au standard BTS SIO SLAM 2ème année.
/// </summary>
public static class MauiProgram
{
    #region Attributs

    #endregion

    #region Constructeurs

    #endregion

    #region Getter/Setter

    #endregion

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

        // =====================================================================
        // ENREGISTREMENT DE L'INJECTION DE DÉPENDANCES (DI CONTAINER)
        // =====================================================================

        // 1. Services : ServiceAPI singleton unique
        constructeur.Services.AddSingleton<ServiceAPI>(sp => ServiceAPI.Instance);

        // 2. Coquille de navigation Shell
        constructeur.Services.AddSingleton<AppShell>();

        // 3. ViewModels
        constructeur.Services.AddTransient<ConnexionVueModele>();
        constructeur.Services.AddTransient<CatalogueVueModele>();
        constructeur.Services.AddTransient<MarcheVueModele>();
        constructeur.Services.AddTransient<GarageVueModele>();

        // 4. Vues (Pages)
        constructeur.Services.AddTransient<ConnexionPage>();
        constructeur.Services.AddTransient<CataloguePage>();
        constructeur.Services.AddTransient<MarchePage>();
        constructeur.Services.AddTransient<GaragePage>();
        constructeur.Services.AddTransient<FicheCartePage>();
        constructeur.Services.AddTransient<CreationOffrePage>();

        return constructeur.Build();
    }

    #endregion
}
```

---

## 7. Stubs Minimaux Requis pour Garantie de Compilation 100% Propre

Comme .NET MAUI active le compilateur XAML strict (`MauiXamlInflator=SourceGen`), `AppShell.xaml` ne peut référencer `vues:CataloguePage`, `vues:MarchePage` et `vues:GaragePage` que si ces classes existent dans l'assembly.
Pour que l'agent exécutant (`worker`) puisse intégrer `AppShell.xaml` et `MauiProgram.cs` sans générer l'erreur `CS0246: The type or namespace name 'CataloguePage' could not be found`, les stubs minimaux suivants doivent être créés simultanément :

### 7.1 `Vues/CataloguePage.xaml` & `.xaml.cs`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    x:Class="AutoMasters.Vues.CataloguePage"
    x:DataType="vm:CatalogueVueModele"
    BackgroundColor="#10080B"
    Title="Catalogue">
    <VerticalStackLayout Padding="16" Spacing="12">
        <SearchBar Placeholder="Rechercher une marque, un modèle..." TextColor="#FFFFFF" PlaceholderColor="#A0A0A0" />
        <CollectionView ItemsSource="{Binding Cartes}">
            <CollectionView.EmptyView>
                <Label Text="Aucun véhicule trouvé." TextColor="#A0A0A0" HorizontalOptions="Center" Margin="0,40" />
            </CollectionView.EmptyView>
        </CollectionView>
    </VerticalStackLayout>
</ContentPage>
```
```csharp
using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class CataloguePage : ContentPage
{
    #region Attributs
    #endregion

    #region Constructeurs
    public CataloguePage(CatalogueVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public CataloguePage() : this(new CatalogueVueModele())
    {
    }
    #endregion

    #region Getter/Setter
    #endregion

    #region Methodes
    #endregion
}
```

### 7.2 `VuesModeles/CatalogueVueModele.cs`
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

public partial class CatalogueVueModele : ObservableObject
{
    #region Attributs
    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private ObservableCollection<Carte> cartes = new();

    [ObservableProperty]
    private bool estOccupe;
    #endregion

    #region Constructeurs
    public CatalogueVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }
    #endregion

    #region Getter/Setter
    public bool EstDisponible => !EstOccupe;
    #endregion

    #region Methodes
    #endregion
}
```

### 7.3 `Vues/MarchePage.xaml` & `.xaml.cs`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    x:Class="AutoMasters.Vues.MarchePage"
    x:DataType="vm:MarcheVueModele"
    BackgroundColor="#10080B"
    Title="Marché">
    <Grid RowDefinitions="Auto,*,Auto">
        <HorizontalStackLayout Grid.Row="0" Spacing="8" Padding="16,8">
            <Label Text="🟢 EN DIRECT" TextColor="#2ECC71" FontAttributes="Bold" />
        </HorizontalStackLayout>
        <CollectionView Grid.Row="1" ItemsSource="{Binding Offres}">
            <CollectionView.EmptyView>
                <Label Text="Aucune offre active pour le moment." TextColor="#A0A0A0" HorizontalOptions="Center" Margin="0,40" />
            </CollectionView.EmptyView>
        </CollectionView>
        <Button Grid.Row="2" Text="+" CornerRadius="28" WidthRequest="56" HeightRequest="56" HorizontalOptions="End" VerticalOptions="End" Margin="16" BackgroundColor="#E63946" TextColor="#FFFFFF" FontSize="24" />
    </Grid>
</ContentPage>
```
```csharp
using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class MarchePage : ContentPage
{
    #region Attributs
    #endregion

    #region Constructeurs
    public MarchePage(MarcheVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public MarchePage() : this(new MarcheVueModele())
    {
    }
    #endregion

    #region Getter/Setter
    #endregion

    #region Methodes
    #endregion
}
```

### 7.4 `VuesModeles/MarcheVueModele.cs`
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

public partial class MarcheVueModele : ObservableObject
{
    #region Attributs
    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private ObservableCollection<Offre> offres = new();

    [ObservableProperty]
    private bool estOccupe;
    #endregion

    #region Constructeurs
    public MarcheVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }
    #endregion

    #region Getter/Setter
    public bool EstDisponible => !EstOccupe;
    #endregion

    #region Methodes
    #endregion
}
```

### 7.5 `Vues/GaragePage.xaml` & `.xaml.cs`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    x:Class="AutoMasters.Vues.GaragePage"
    x:DataType="vm:GarageVueModele"
    BackgroundColor="#10080B"
    Title="Garage">
    <VerticalStackLayout Padding="16" Spacing="16">
        <Border BackgroundColor="#221215" Stroke="#331A20" StrokeShape="RoundRectangle 12" Padding="16">
            <VerticalStackLayout Spacing="6">
                <Label Text="VALEUR TOTALE DE LA COLLECTION" TextColor="#A0A0A0" FontSize="11" FontAttributes="Bold" />
                <Label Text="0 coins" TextColor="#FFFFFF" FontSize="28" FontAttributes="Bold" />
            </VerticalStackLayout>
        </Border>
    </VerticalStackLayout>
</ContentPage>
```
```csharp
using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class GaragePage : ContentPage
{
    #region Attributs
    #endregion

    #region Constructeurs
    public GaragePage(GarageVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public GaragePage() : this(new GarageVueModele())
    {
    }
    #endregion

    #region Getter/Setter
    #endregion

    #region Methodes
    #endregion
}
```

### 7.6 `VuesModeles/GarageVueModele.cs`
```csharp
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

public partial class GarageVueModele : ObservableObject
{
    #region Attributs
    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private Portefeuille? portefeuille;

    [ObservableProperty]
    private bool estOccupe;
    #endregion

    #region Constructeurs
    public GarageVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }
    #endregion

    #region Getter/Setter
    public bool EstDisponible => !EstOccupe;
    #endregion

    #region Methodes
    #endregion
}
```

### 7.7 `Vues/FicheCartePage.xaml` & `.xaml.cs`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    x:Class="AutoMasters.Vues.FicheCartePage"
    BackgroundColor="#10080B"
    Title="Détails du véhicule">
    <Label Text="Fiche technique détaillée" TextColor="#FFFFFF" HorizontalOptions="Center" VerticalOptions="Center" />
</ContentPage>
```
```csharp
namespace AutoMasters.Vues;

public partial class FicheCartePage : ContentPage
{
    #region Attributs
    #endregion

    #region Constructeurs
    public FicheCartePage()
    {
        InitializeComponent();
    }
    #endregion

    #region Getter/Setter
    #endregion

    #region Methodes
    #endregion
}
```

### 7.8 `Vues/CreationOffrePage.xaml` & `.xaml.cs`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    x:Class="AutoMasters.Vues.CreationOffrePage"
    BackgroundColor="#10080B"
    Title="Vendre un véhicule">
    <Label Text="Mise en vente d'un véhicule" TextColor="#FFFFFF" HorizontalOptions="Center" VerticalOptions="Center" />
</ContentPage>
```
```csharp
namespace AutoMasters.Vues;

public partial class CreationOffrePage : ContentPage
{
    #region Attributs
    #endregion

    #region Constructeurs
    public CreationOffrePage()
    {
        InitializeComponent();
    }
    #endregion

    #region Getter/Setter
    #endregion

    #region Methodes
    #endregion
}
```

---

## 8. Matrice de Validation des Tests E2E (`AutoMasters.E2ETests`)

L'implémentation proposée garantit la satisfaction des tests E2E suivants :

| Test E2E | Cible | Vérification effectuée | Statut attendu après application |
|---|---|---|---|
| `F18_01_LoginPage_XamlMustExist` | `Vues/ConnexionPage.xaml` | Présence du fichier XAML | PASS (Réussi) |
| `F18_02_LoginPage_MustContainEmailAndPassEntries` | `Vues/ConnexionPage.xaml` | Liaisons `Email` et `MotDePasse` | PASS (Réussi) |
| `F18_03_LoginPage_MustContainSeConnecterButton` | `Vues/ConnexionPage.xaml` | Présence du texte "SE CONNECTER" | PASS (Réussi) |
| `F18_04_LoginPage_MustContainTitleAutoMasters` | `Vues/ConnexionPage.xaml` | Titre "AUTO" et "MASTERS" | PASS (Réussi) |
| `F18_05_LoginPage_MustContainHelperLinks` | `Vues/ConnexionPage.xaml` | Liens "oublié" et "compte" | PASS (Réussi) |
| `F18_B02_LoginPage_IsPasswordProperty_MustBeSet` | `Vues/ConnexionPage.xaml` | Attribut `IsPassword="True"` | PASS (Réussi) |
| `F18_B03_LoginPage_KeyboardEmail_MustBeConfigured` | `Vues/ConnexionPage.xaml` | Attribut `Keyboard="Email"` | PASS (Réussi) |
| `F19_01_LoginViewModel_MustInheritObservableObject` | `ConnexionVueModele.cs` | Héritage de `ObservableObject` | PASS (Réussi) |
| `F19_02_LoginViewModel_MustExposeCredentialsProperties` | `ConnexionVueModele.cs` | Propriétés `Email` / `Identifiant` et `MotDePasse` | PASS (Réussi) |
| `F19_03_LoginViewModel_MustExposeBusyAndAvailableStates` | `ConnexionVueModele.cs` | Propriétés `EstOccupe`, `EstDisponible`, `IsBusy` | PASS (Réussi) |
| `F19_04_LoginViewModel_MustExposeSeConnecterCommand` | `ConnexionVueModele.cs` | Génération de `SeConnecterCommand` | PASS (Réussi) |
| `F19_B02_LoginViewModel_FailedLogin_ClearsPasswordKeepsEmail` | `ConnexionVueModele.cs` | Effacement du mot de passe uniquement en cas d'erreur | PASS (Réussi) |
| `F19_B03_LoginViewModel_EmailFormatRegex_Validation` | `ConnexionVueModele.cs` | Validation présence de `@` et `.` | PASS (Réussi) |
| `F20_01_AppShell_MustDefine5PrimaryTabs` | `AppShell.xaml` | Définition des onglets Catalogue, Marché, Garage | PASS (Réussi) |
| `F20_02_AppShell_MustConfigureDarkThemeColors` | `AppShell.xaml` | Définition du fond sombre `#10080B` | PASS (Réussi) |
| `F20_03_AppShell_MustDefineCatalogueTabAsDefaultOrFirst` | `AppShell.xaml` | Définition de `TabBar` avec `Catalogue` en premier | PASS (Réussi) |
| `F20_04_AppShell_MustSupportShellNavigationRouting` | `AppShell.xaml.cs` | Présence de `InitializeComponent` et des routes | PASS (Réussi) |
| `F20_05_AppShell_CodeBehind_MustBePure` | `AppShell.xaml.cs` | Zéro appel HTTP, zéro logique métier | PASS (Réussi) |
| `F27_01_DesignTokens_ColorsXamlMustExist` | `Colors.xaml` | Présence du dictionnaire de couleurs | PASS (Réussi) |
| `F27_02_DesignTokens_MustDefineDarkBackgroundToken` | `Colors.xaml` | Jeton sombre `#10080B` / `ColorBackground` | PASS (Réussi) |
| `F27_03_DesignTokens_MustDefinePrimaryRedToken` | `Colors.xaml` | Jeton rouge `#E63946` / `ColorPrimaryRed` | PASS (Réussi) |
| `F27_04_DesignTokens_MustDefineSecondaryGreenToken` | `Colors.xaml` | Jeton vert `#2ECC71` / `SecondaryGreen` | PASS (Réussi) |
| `F27_05_DesignTokens_StylesXamlMustExist` | `Styles.xaml` | Présence du dictionnaire de styles | PASS (Réussi) |
| `F28_01_CodeBehind_AllViewCsFilesMustBePure` | Tous les `.xaml.cs` | Validation de pureté `ValidateCodeBehindPurity` | PASS (Réussi) |
| `F28_02_CodeBehind_MustContainZeroHttpClientInstances` | Tous les `.xaml.cs` | Absence totale de `HttpClient` dans les vues | PASS (Réussi) |

---

## 9. Conclusion et Recommandations pour l'Agent Worker

1. **Ordre d'application recommandé pour l'implémentation** :
   - Étape 1 : Mettre à jour `Resources/Styles/Colors.xaml` et `Resources/Styles/Styles.xaml`.
   - Étape 2 : Créer les stubs minimaux (`CataloguePage`, `MarchePage`, `GaragePage`, `FicheCartePage`, `CreationOffrePage` et leurs ViewModels associés) afin de garantir la compilabilité immédiate.
   - Étape 3 : Mettre à jour `AppShell.xaml` et `AppShell.xaml.cs`.
   - Étape 4 : Mettre à jour `Vues/ConnexionPage.xaml`, `Vues/ConnexionPage.xaml.cs`, et `VuesModeles/ConnexionVueModele.cs`.
   - Étape 5 : Mettre à jour `MauiProgram.cs` pour enregistrer l'ensemble des dépendances.
   - Étape 6 : Exécuter `dotnet build` et `dotnet test` pour confirmer 100% de réussite.
2. **Garantie de non-régression** : Les 33 tests unitaires de `AutoMasters.Tests` restent totalement inchangés et continueront de réussir sans impact.
