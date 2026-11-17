# Rapport de Spécification et Conception — Milestone 3
## Couche Présentation : Catalogue & Fiche Carte (F21 & F22)

**Auteur :** explorer_m3_2  
**Date :** 2026-10-09  
**Niveau académique cible :** BTS SIO SLAM 2ème année (Systèmes Informatiques aux Organisations — option SLAM)  
**Framework & Runtime :** .NET MAUI targeting Windows (`net10.0-windows10.0.19041.0`, exécution dépackagée)  
**Patron d'architecture :** MVVM strict (`CommunityToolkit.Mvvm`), Sérialisation `Newtonsoft.Json`  
**Conventions BTS SIO :** Régions C# explicites en français, Code-behind pur (0 logique métier / 0 appel HTTP), Dossiers francophones (`Vues/`, `VuesModeles/`, `Modeles/`, `Services/`).

---

## 1. Synthèse Exécutive et Périmètre d'Intervention

Le présent rapport fournit la spécification exhaustive, les maquettes fonctionnelles et l'intégralité du code source proposé (XAML et C#) pour les fonctionnalités **F21 (Catalogue des véhicules)** et **F22 (Fiche détaillée de carte)** du projet **AutoMasters**.

### 1.1 Objectifs Clés
1. **`CataloguePage` & `CatalogueVueModele` (F21) :**
   - En-tête typé dark automotive avec le titre « CATALOGUE » et le compteur de véhicules disponibles.
   - Barre de recherche en temps réel (`SearchBar`) avec filtrage instantané par marque, modèle et variante.
   - Système de puces de filtrage interactives (« TOUTES », « COMMUNE », « RARE », « ÉPIQUE », « LÉGENDAIRE ») et contrôle `Picker`.
   - `CollectionView` en grille responsive 2 colonnes présentant chaque carte automobile avec : photographie (`ImageUrl`), marque, modèle, variante, année, badge de rareté coloré, puissance (`Puissance` / `PuissanceFormatee`), tirage en circulation (`ExemplairesEnCirculation`) et prix officiel en monnaie virtuelle (`PrixOfficiel` en coins).
   - Tirer pour actualiser (`RefreshView` avec `ActualiserCommand`).
   - Clic / tap sur une carte déclenchant `SelectionnerCarteCommand` et naviguant vers `FicheCartePage` en transmettant l'objet carte ou son identifiant.
   - Code-behind 100% pur dans `CataloguePage.xaml.cs` (`InitializeComponent()` uniquement, zéro logique métier).

2. **`FicheCartePage` & `FicheCarteVueModele` (F22) :**
   - Vue détaillée grand format avec photographie haute résolution en bannière héroïque.
   - Grille de spécifications techniques 2x2 saillante (Vitesse de pointe `VitesseMax`, Accélération `ZeroACent`, Puissance `Puissance`, Année).
   - Badge de rareté dynamique (`LÉGENDAIRE`, `ÉPIQUE`, `RARE`, `COMMUNE`) avec code couleur respectant le Design System.
   - Bloc de cotation officielle et tirage en circulation (`ExemplairesEnCirculationFormate`, `PrixOfficielFormate`).
   - Section historique et description détaillée (`Description`).
   - Bouton d'action principal « Voir les offres sur le Marché » naviguant vers l'onglet Marché avec filtre ciblé sur la carte consultée.
   - Bouton de retour au catalogue.
   - Code-behind 100% pur dans `FicheCartePage.xaml.cs` (`InitializeComponent()` uniquement).

3. **Discipline de code BTS SIO :**
   - Respect strict des 4 régions françaises : `#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`.
   - Compatibilité totale avec l'évaluateur de pureté E2E (`ValidateCodeBehindPurity`, `ValidateRegionDiscipline`).

---

## 2. Spécification Détaillée des Composants

### 2.1 Catalogue (`CataloguePage` & `CatalogueVueModele`)

```
+-------------------------------------------------------------+
| CATALOGUE                                                   |
| 10 véhicules disponibles                                    |
|                                                             |
| [ 🔍 Rechercher une marque, un modèle...                  ] |
|                                                             |
| [TOUTES] [COMMUNE] [RARE] [ÉPIQUE] [LÉGENDAIRE]             |
|                                                             |
| +-------------------------+     +-------------------------+ |
| | [PHOTO FERRARI] ★ LÉG.  |     | [PHOTO PORSCHE]  ÉPIQUE | |
| | Ferrari                 |     | Porsche                 | |
| | LaFerrari Aperta (2017) |     | 911 GT3 RS (2023)       | |
| | 963 ch · 210 ex.        |     | 525 ch · 1200 ex.       | |
| | 480 000 coins           |     | 223 000 coins           | |
| +-------------------------+     +-------------------------+ |
| +-------------------------+     +-------------------------+ |
| | [PHOTO RENAULT]    RARE |     | [PHOTO TOYOTA]     RARE | |
| | Renault                 |     | Toyota                  | |
| | Mégane R.S. (2023)      |     | GR Yaris (2023)         | |
| | 300 ch · 1976 ex.       |     | 261 ch · 3500 ex.       | |
| | 44 000 coins            |     | 47 000 coins            | |
| +-------------------------+     +-------------------------+ |
+-------------------------------------------------------------+
```

#### Cycle de vie et filtrage :
- La collection complète issue de `ServiceAPI.Instance.RecupererCatalogueAsync()` est conservée dans une liste tampon privée `toutesLesCartes`.
- Lors d'une saisie dans la barre de recherche (`TexteRecherche`) ou de la sélection d'une puce (`RareteSelectionnee`), la méthode `AppliquerFiltres()` recalcule la collection observable `Cartes` en mémoire de manière instantanée (< 1 ms), évitant les surcharges réseau.
- Le pull-to-refresh (`ActualiserCommand`) sollicite l'API REST officielle, met à jour le cache local et réapplique les critères actifs.
- Lorsqu'aucun véhicule ne correspond aux filtres, un état vide (`EmptyView`) s'affiche avec un bouton permettant de réinitialiser les filtres en un clic.

### 2.2 Fiche Carte (`FicheCartePage` & `FicheCarteVueModele`)

```
+-------------------------------------------------------------+
| ◀ RETOUR AU CATALOGUE                                       |
|                                                             |
| +---------------------------------------------------------+ |
| |                                                         | |
| |               [ PHOTO HAUTE RÉSOLUTION ]                | |
| |                                                         | |
| | [210 ex. en circulation]                 ★ LÉGENDAIRE   | |
| +---------------------------------------------------------+ |
|                                                             |
| Ferrari                                                     |
| LaFerrari Aperta                                            |
| Année modèle 2017 · Variante Aperta                         |
|                                                             |
| SPÉCIFICATIONS TECHNIQUES                                   |
| +---------------------------+ +---------------------------+ |
| | 🏎️ VITESSE MAX            | | ⏱️ 0 À 100 KM/H           | |
| | 350 km/h                  | | 2.9 s                     | |
| +---------------------------+ +---------------------------+ |
| +---------------------------+ +---------------------------+ |
| | ⚡ PUISSANCE               | | 📅 ANNÉE                  | |
| | 963 ch                    | | 2017                      | |
| +---------------------------+ +---------------------------+ |
|                                                             |
| VALORISATION OFFICIELLE (RM001)                             |
| +---------------------------------------------------------+ |
| | 480 000 coins                                           | |
| | Cours officiel de référence fixé par le serveur         | |
| | Tirage mondial limité à 210 exemplaires                 | |
| +---------------------------------------------------------+ |
|                                                             |
| HISTORIQUE & DESCRIPTION                                    |
| +---------------------------------------------------------+ |
| | Hypercar hybride V12 à toit ouvert combinant un moteur  | |
| | atmosphérique de 6,3 litres et le système HY-KERS...     | |
| +---------------------------------------------------------+ |
|                                                             |
| [  VOIR LES OFFRES SUR LE MARCHÉ (8)  ]                     |
|                                                             |
| [           RETOUR AU CATALOGUE       ]                     |
+-------------------------------------------------------------+
```

#### Réception des paramètres de navigation (`QueryProperty`) :
`FicheCarteVueModele` implémente un double mécanisme de réception résilient :
1. `[QueryProperty(nameof(CarteSelectionnee), "carte")]` : reçoit directement l'objet `Carte` sérialisé/transmis depuis `CataloguePage`, affichage instantané sans latence réseau.
2. `[QueryProperty(nameof(CarteId), "id")]` : reçoit l'identifiant pour la prise en charge du deep-linking ou de la restauration de page. Si l'objet carte n'est pas encore en mémoire, le ViewModel appelle automatiquement `serviceApi.RecupererFicheCarteAsync(CarteId)`.

---

## 3. Code Source Proposé

### 3.1 `AutoMasters/Vues/CataloguePage.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="AutoMasters.Vues.CataloguePage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    xmlns:modeles="clr-namespace:AutoMasters.Modeles"
    x:DataType="vm:CatalogueVueModele"
    BackgroundColor="#10080B"
    Title="CATALOGUE"
    Shell.NavBarIsVisible="False">

    <ContentPage.BindingContext>
        <vm:CatalogueVueModele />
    </ContentPage.BindingContext>

    <Grid RowDefinitions="Auto,Auto,Auto,*" Padding="16,16,16,0">

        <!-- 1. En-tête de la page -->
        <VerticalStackLayout Grid.Row="0" Spacing="4" Margin="0,0,0,12">
            <Label
                Text="CATALOGUE"
                FontSize="24"
                FontAttributes="Bold"
                TextColor="#FFFFFF"
                CharacterSpacing="1" />
            <Label
                Text="{Binding NombreResultatsFormate}"
                FontSize="12"
                TextColor="#9CA3AF" />
        </VerticalStackLayout>

        <!-- 2. Barre de recherche temps réel -->
        <Border
            Grid.Row="1"
            Stroke="#331A20"
            StrokeThickness="1"
            BackgroundColor="#1E1014"
            StrokeShape="RoundRectangle 12"
            Margin="0,0,0,12"
            Padding="4,0">
            <SearchBar
                Placeholder="Rechercher une marque, un modèle..."
                Text="{Binding TexteRecherche}"
                TextColor="#FFFFFF"
                PlaceholderColor="#6B7280"
                CancelButtonColor="#E63946"
                BackgroundColor="Transparent"
                HeightRequest="46"
                SearchCommand="{Binding RechercherCommand}" />
        </Border>

        <!-- 3. Sélecteur de rareté (Puces horizontales et Picker) -->
        <Grid Grid.Row="2" Margin="0,0,0,12">
            <ScrollView Orientation="Horizontal" HorizontalScrollBarVisibility="Never">
                <HorizontalStackLayout Spacing="8">
                    <!-- Puce TOUTES -->
                    <Button
                        Text="TOUTES"
                        Command="{Binding FiltrerParRareteCommand}"
                        CommandParameter="TOUTES"
                        HeightRequest="36"
                        Padding="14,0"
                        CornerRadius="8"
                        FontSize="11"
                        FontAttributes="Bold"
                        BackgroundColor="#221215"
                        TextColor="#FFFFFF"
                        BorderColor="#E63946"
                        BorderWidth="1" />

                    <!-- Puce COMMUNE -->
                    <Button
                        Text="COMMUNE"
                        Command="{Binding FiltrerParRareteCommand}"
                        CommandParameter="COMMUNE"
                        HeightRequest="36"
                        Padding="14,0"
                        CornerRadius="8"
                        FontSize="11"
                        FontAttributes="Bold"
                        BackgroundColor="#1E2024"
                        TextColor="#9CA3AF"
                        BorderColor="#374151"
                        BorderWidth="1" />

                    <!-- Puce RARE -->
                    <Button
                        Text="RARE"
                        Command="{Binding FiltrerParRareteCommand}"
                        CommandParameter="RARE"
                        HeightRequest="36"
                        Padding="14,0"
                        CornerRadius="8"
                        FontSize="11"
                        FontAttributes="Bold"
                        BackgroundColor="#0E2338"
                        TextColor="#38BDF8"
                        BorderColor="#0284C7"
                        BorderWidth="1" />

                    <!-- Puce ÉPIQUE -->
                    <Button
                        Text="ÉPIQUE"
                        Command="{Binding FiltrerParRareteCommand}"
                        CommandParameter="ÉPIQUE"
                        HeightRequest="36"
                        Padding="14,0"
                        CornerRadius="8"
                        FontSize="11"
                        FontAttributes="Bold"
                        BackgroundColor="#251433"
                        TextColor="#A855F7"
                        BorderColor="#9333EA"
                        BorderWidth="1" />

                    <!-- Puce LÉGENDAIRE -->
                    <Button
                        Text="★ LÉGENDAIRE"
                        Command="{Binding FiltrerParRareteCommand}"
                        CommandParameter="LÉGENDAIRE"
                        HeightRequest="36"
                        Padding="14,0"
                        CornerRadius="8"
                        FontSize="11"
                        FontAttributes="Bold"
                        BackgroundColor="#2A1E0D"
                        TextColor="#F59E0B"
                        BorderColor="#D97706"
                        BorderWidth="1" />
                </HorizontalStackLayout>
            </ScrollView>

            <!-- Picker accessible en arrière-plan -->
            <Picker
                IsVisible="False"
                Title="Filtrer par rareté"
                ItemsSource="{Binding Raretes}"
                SelectedItem="{Binding RareteSelectionnee}" />
        </Grid>

        <!-- 4. Conteneur principal avec Tirer pour actualiser -->
        <RefreshView
            Grid.Row="3"
            IsRefreshing="{Binding EstEnActualisation}"
            Command="{Binding ActualiserCommand}"
            RefreshColor="#E63946">

            <Grid>
                <!-- Indicateur de chargement central -->
                <ActivityIndicator
                    Color="#E63946"
                    IsRunning="{Binding EstOccupe}"
                    IsVisible="{Binding EstOccupe}"
                    HorizontalOptions="Center"
                    VerticalOptions="Center"
                    WidthRequest="48"
                    HeightRequest="48" />

                <!-- CollectionView des cartes automobiles en grille 2 colonnes -->
                <CollectionView
                    ItemsSource="{Binding Cartes}"
                    SelectionMode="Single"
                    SelectedItem="{Binding CarteSelectionnee}"
                    SelectionChangedCommand="{Binding SelectionnerCarteCommand}"
                    IsVisible="{Binding EstDisponible}">

                    <CollectionView.ItemsLayout>
                        <GridItemsLayout
                            Orientation="Vertical"
                            Span="2"
                            HorizontalItemSpacing="12"
                            VerticalItemSpacing="12" />
                    </CollectionView.ItemsLayout>

                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="modeles:Carte">
                            <Border
                                Stroke="#331A20"
                                StrokeThickness="1"
                                StrokeShape="RoundRectangle 14"
                                BackgroundColor="#1E1014"
                                Padding="0">

                                <Border.GestureRecognizers>
                                    <TapGestureRecognizer
                                        Command="{Binding Source={RelativeSource AncestorType={x:Type vm:CatalogueVueModele}}, Path=SelectionnerCarteCommand}"
                                        CommandParameter="{Binding .}" />
                                </Border.GestureRecognizers>

                                <Grid RowDefinitions="120,Auto">
                                    <!-- Vignette photo du véhicule -->
                                    <Grid Grid.Row="0">
                                        <Image
                                            Source="{Binding ImageUrl}"
                                            Aspect="AspectFill"
                                            HorizontalOptions="Fill"
                                            VerticalOptions="Fill" />

                                        <!-- Badge de rareté dynamique superposé -->
                                        <Border
                                            StrokeThickness="1"
                                            StrokeShape="RoundRectangle 6"
                                            Padding="6,2"
                                            Margin="6"
                                            HorizontalOptions="End"
                                            VerticalOptions="Start">
                                            <Border.Triggers>
                                                <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="LÉGENDAIRE">
                                                    <Setter Property="Stroke" Value="#F59E0B" />
                                                    <Setter Property="BackgroundColor" Value="#D92A1E0D" />
                                                </DataTrigger>
                                                <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="ÉPIQUE">
                                                    <Setter Property="Stroke" Value="#A855F7" />
                                                    <Setter Property="BackgroundColor" Value="#D9251433" />
                                                </DataTrigger>
                                                <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="RARE">
                                                    <Setter Property="Stroke" Value="#38BDF8" />
                                                    <Setter Property="BackgroundColor" Value="#D90E2338" />
                                                </DataTrigger>
                                                <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="COMMUNE">
                                                    <Setter Property="Stroke" Value="#9CA3AF" />
                                                    <Setter Property="BackgroundColor" Value="#D91E2024" />
                                                </DataTrigger>
                                            </Border.Triggers>

                                            <Label
                                                Text="{Binding Rarete}"
                                                FontSize="9"
                                                FontAttributes="Bold"
                                                TextColor="#FFFFFF" />
                                        </Border>
                                    </Grid>

                                    <!-- Informations du véhicule -->
                                    <VerticalStackLayout Grid.Row="1" Padding="10,8" Spacing="3">
                                        <!-- Marque -->
                                        <Label
                                            Text="{Binding Marque}"
                                            FontSize="14"
                                            FontAttributes="Bold"
                                            TextColor="#FFFFFF"
                                            LineBreakMode="TailTruncation" />

                                        <!-- Modèle et année -->
                                        <Label
                                            Text="{Binding Modele}"
                                            FontSize="12"
                                            TextColor="#9CA3AF"
                                            LineBreakMode="TailTruncation" />

                                        <!-- Spécifications compactes (Puissance & Tirage) -->
                                        <HorizontalStackLayout Spacing="6" Margin="0,2,0,0">
                                            <Label
                                                Text="{Binding PuissanceFormatee}"
                                                FontSize="11"
                                                TextColor="#D1D5DB" />
                                            <Label
                                                Text="·"
                                                FontSize="11"
                                                TextColor="#6B7280" />
                                            <Label
                                                Text="{Binding ExemplairesEnCirculation, StringFormat='{0} ex.'}"
                                                FontSize="11"
                                                TextColor="#6B7280" />
                                        </HorizontalStackLayout>

                                        <!-- Cours officiel (Prix en coins) -->
                                        <Border
                                            Stroke="#442228"
                                            StrokeShape="RoundRectangle 6"
                                            BackgroundColor="#261217"
                                            Padding="6,3"
                                            Margin="0,4,0,0"
                                            HorizontalOptions="Start">
                                            <Label
                                                Text="{Binding PrixOfficielFormate}"
                                                FontSize="11"
                                                FontAttributes="Bold"
                                                TextColor="#FBBF24" />
                                        </Border>
                                    </VerticalStackLayout>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>

                    <!-- État vide si aucune carte ne correspond aux critères -->
                    <CollectionView.EmptyView>
                        <VerticalStackLayout
                            Padding="32"
                            Spacing="12"
                            HorizontalOptions="Center"
                            VerticalOptions="Center">
                            <Label
                                Text="🔍"
                                FontSize="48"
                                HorizontalTextAlignment="Center" />
                            <Label
                                Text="Aucun véhicule trouvé"
                                FontSize="18"
                                FontAttributes="Bold"
                                TextColor="#FFFFFF"
                                HorizontalTextAlignment="Center" />
                            <Label
                                Text="Aucune carte automobile ne correspond à votre recherche ou filtre actif."
                                FontSize="13"
                                TextColor="#9CA3AF"
                                HorizontalTextAlignment="Center" />
                            <Button
                                Text="RÉINITIALISER LES FILTRES"
                                Command="{Binding ReinitialiserFiltresCommand}"
                                BackgroundColor="#E63946"
                                TextColor="#FFFFFF"
                                FontAttributes="Bold"
                                CornerRadius="8"
                                HeightRequest="40"
                                Margin="0,8,0,0" />
                        </VerticalStackLayout>
                    </CollectionView.EmptyView>
                </CollectionView>
            </Grid>
        </RefreshView>

    </Grid>
</ContentPage>
```

---

### 3.2 `AutoMasters/Vues/CataloguePage.xaml.cs`

```csharp
namespace AutoMasters.Vues;

/// <summary>
/// Page de présentation du catalogue officiel AutoMasters.
/// Respecte strictement la pureté du code-behind BTS SIO SLAM :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class CataloguePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public CataloguePage()
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

### 3.3 `AutoMasters/VuesModeles/CatalogueVueModele.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using AutoMasters.Modeles;
using AutoMasters.Services;
using AutoMasters.Vues;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel responsable de la gestion du catalogue des véhicules (Fonctionnalité F21).
/// Gère le chargement depuis ServiceAPI, le filtrage instantané en mémoire (recherche & rareté),
/// le pull-to-refresh et la navigation vers FicheCartePage.
/// Conforme aux conventions BTS SIO SLAM 2ème année.
/// </summary>
public partial class CatalogueVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;
    private List<Carte> toutesLesCartes = new();

    [ObservableProperty]
    private ObservableCollection<Carte> cartes = new();

    [ObservableProperty]
    private ObservableCollection<string> raretes = new();

    [ObservableProperty]
    private string texteRecherche = "";

    [ObservableProperty]
    private string rareteSelectionnee = "TOUTES";

    [ObservableProperty]
    private Carte? carteSelectionnee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private bool estEnActualisation;

    [ObservableProperty]
    private string messageErreur = "";

    [ObservableProperty]
    private bool aErreur;

    [ObservableProperty]
    private bool estVide;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NombreResultatsFormate))]
    private int nombreResultats;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant le singleton ServiceAPI.
    /// Utilisé lors de l'instanciation XAML de la vue.
    /// </summary>
    public CatalogueVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP pour les tests unitaires déterministes.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
    public CatalogueVueModele(ServiceAPI serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;

        Raretes = new ObservableCollection<string>
        {
            "TOUTES",
            "COMMUNE",
            "RARE",
            "ÉPIQUE",
            "LÉGENDAIRE"
        };

        // Déclenchement du chargement initial
        _ = ChargerCatalogueAsync();
    }

    #endregion

    #region Getter/Setter

    /// <summary>
    /// Indique si l'interface est prête pour l'interaction utilisateur.
    /// </summary>
    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Libellé formaté indiquant le nombre de véhicules disponibles.
    /// </summary>
    public string NombreResultatsFormate => NombreResultats switch
    {
        0 => "Aucun véhicule disponible",
        1 => "1 véhicule disponible",
        _ => $"{NombreResultats} véhicules disponibles"
    };

    #endregion

    #region Methodes

    /// <summary>
    /// Détecte les modifications du texte de recherche et recalcule les filtres immédiatement.
    /// </summary>
    partial void OnTexteRechercheChanged(string value)
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Détecte les modifications du filtre de rareté et recalcule les filtres immédiatement.
    /// </summary>
    partial void OnRareteSelectionneeChanged(string value)
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Charge l'intégralité du catalogue depuis l'API REST officielle.
    /// Protégé contre les interruptions réseau avec conservation du cache.
    /// </summary>
    [RelayCommand]
    public async Task ChargerCatalogueAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        MessageErreur = "";
        AErreur = false;

        try
        {
            var resultat = await serviceApi.RecupererCatalogueAsync();
            toutesLesCartes = resultat ?? new List<Carte>();
            AppliquerFiltres();
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
            AErreur = true;
        }
        finally
        {
            EstOccupe = false;
            EstEnActualisation = false;
        }
    }

    /// <summary>
    /// Commande de rafraîchissement déclenchée par le geste Pull-to-Refresh.
    /// </summary>
    [RelayCommand]
    public async Task ActualiserAsync()
    {
        EstEnActualisation = true;
        try
        {
            var resultat = await serviceApi.RecupererCatalogueAsync();
            toutesLesCartes = resultat ?? new List<Carte>();
            AppliquerFiltres();
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
            AErreur = true;
        }
        finally
        {
            EstEnActualisation = false;
        }
    }

    /// <summary>
    /// Filtre les cartes en mémoire selon le texte saisi et la rareté sélectionnée.
    /// </summary>
    private void AppliquerFiltres()
    {
        IEnumerable<Carte> requete = toutesLesCartes;

        // Filtrage textuel (marque, modèle ou variante)
        if (!string.IsNullOrWhiteSpace(TexteRecherche))
        {
            string recherche = TexteRecherche.Trim();
            requete = requete.Where(c =>
                (c.Marque != null && c.Marque.Contains(recherche, StringComparison.OrdinalIgnoreCase)) ||
                (c.Modele != null && c.Modele.Contains(recherche, StringComparison.OrdinalIgnoreCase)) ||
                (c.Variante != null && c.Variante.Contains(recherche, StringComparison.OrdinalIgnoreCase)));
        }

        // Filtrage par rareté
        if (!string.IsNullOrWhiteSpace(RareteSelectionnee) &&
            !string.Equals(RareteSelectionnee, "TOUTES", StringComparison.OrdinalIgnoreCase))
        {
            requete = requete.Where(c => string.Equals(c.Rarete, RareteSelectionnee, StringComparison.OrdinalIgnoreCase));
        }

        var listeFiltree = requete.ToList();

        Cartes.Clear();
        foreach (var carte in listeFiltree)
        {
            Cartes.Add(carte);
        }

        NombreResultats = Cartes.Count;
        EstVide = NombreResultats == 0 && !EstOccupe;
    }

    /// <summary>
    /// Commande explicite de recherche pour le bouton de la SearchBar.
    /// </summary>
    [RelayCommand]
    private void Rechercher()
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Sélectionne un niveau de rareté via les puces interactives.
    /// </summary>
    [RelayCommand]
    private void FiltrerParRarete(string rarete)
    {
        if (string.IsNullOrWhiteSpace(rarete))
            RareteSelectionnee = "TOUTES";
        else
            RareteSelectionnee = rarete;
    }

    /// <summary>
    /// Réinitialise l'ensemble des filtres de recherche et de rareté.
    /// </summary>
    [RelayCommand]
    private void ReinitialiserFiltres()
    {
        TexteRecherche = "";
        RareteSelectionnee = "TOUTES";
        AppliquerFiltres();
    }

    /// <summary>
    /// Navigue vers la fiche détaillée du véhicule sélectionné.
    /// Transmet simultanément l'objet Carte complet et son identifiant.
    /// </summary>
    [RelayCommand]
    private async Task SelectionnerCarteAsync(Carte? carte)
    {
        var cible = carte ?? CarteSelectionnee;
        if (cible == null)
            return;

        var parametres = new Dictionary<string, object>
        {
            { "carte", cible },
            { "id", cible.Identifiant }
        };

        await Shell.Current.GoToAsync(nameof(FicheCartePage), parametres);

        // Réinitialise la sélection visuelle
        CarteSelectionnee = null;
    }

    #endregion
}
```

---

### 3.4 `AutoMasters/Vues/FicheCartePage.xaml`

```xaml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    x:Class="AutoMasters.Vues.FicheCartePage"
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    xmlns:modeles="clr-namespace:AutoMasters.Modeles"
    x:DataType="vm:FicheCarteVueModele"
    BackgroundColor="#10080B"
    Title="{Binding TitrePage}"
    Shell.NavBarIsVisible="False">

    <ContentPage.BindingContext>
        <vm:FicheCarteVueModele />
    </ContentPage.BindingContext>

    <ScrollView>
        <VerticalStackLayout Spacing="16" Padding="16,16,16,32">

            <!-- 1. Barre de navigation supérieure (Bouton retour) -->
            <Grid ColumnDefinitions="Auto,*,Auto" VerticalOptions="Center">
                <Button
                    Grid.Column="0"
                    Text="◀ Retour"
                    Command="{Binding RetourCommand}"
                    BackgroundColor="Transparent"
                    TextColor="#E63946"
                    FontSize="14"
                    FontAttributes="Bold"
                    Padding="0"
                    HeightRequest="36" />

                <Label
                    Grid.Column="1"
                    Text="FICHE VÉHICULE"
                    TextColor="#FFFFFF"
                    FontSize="13"
                    FontAttributes="Bold"
                    CharacterSpacing="1"
                    HorizontalTextAlignment="Center"
                    VerticalTextAlignment="Center" />
            </Grid>

            <!-- Indicateur de chargement si requête réseau en cours -->
            <ActivityIndicator
                Color="#E63946"
                IsRunning="{Binding EstOccupe}"
                IsVisible="{Binding EstOccupe}"
                HorizontalOptions="Center" />

            <!-- Bannière d'erreur éventuelle -->
            <Border
                IsVisible="{Binding AErreur}"
                Stroke="#EF4444"
                StrokeShape="RoundRectangle 10"
                BackgroundColor="#2A1215"
                Padding="12"
                Margin="0,0,0,8">
                <VerticalStackLayout Spacing="6">
                    <Label
                        Text="{Binding MessageErreur}"
                        TextColor="#EF4444"
                        FontSize="13" />
                    <Button
                        Text="RÉESSAYER"
                        Command="{Binding ChargerDetailsCommand}"
                        BackgroundColor="#EF4444"
                        TextColor="#FFFFFF"
                        HeightRequest="36"
                        CornerRadius="6" />
                </VerticalStackLayout>
            </Border>

            <!-- 2. Photographie grand format (Hero Image) avec badges superposés -->
            <Border
                Stroke="#331A20"
                StrokeThickness="1"
                StrokeShape="RoundRectangle 16"
                BackgroundColor="#1E1014"
                Padding="0">
                <Grid HeightRequest="240">
                    <Image
                        Source="{Binding ImageUrl}"
                        Aspect="AspectFill"
                        HorizontalOptions="Fill"
                        VerticalOptions="Fill" />

                    <!-- Badge de tirage en circulation (En haut à gauche) -->
                    <Border
                        Stroke="#331A20"
                        StrokeShape="RoundRectangle 6"
                        BackgroundColor="#CC10080B"
                        Padding="8,4"
                        Margin="10"
                        HorizontalOptions="Start"
                        VerticalOptions="Start">
                        <Label
                            Text="{Binding ExemplairesEnCirculationFormate}"
                            FontSize="11"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                    </Border>

                    <!-- Badge de rareté dynamique (En haut à droite) -->
                    <Border
                        StrokeThickness="1"
                        StrokeShape="RoundRectangle 6"
                        Padding="10,4"
                        Margin="10"
                        HorizontalOptions="End"
                        VerticalOptions="Start">
                        <Border.Triggers>
                            <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="LÉGENDAIRE">
                                <Setter Property="Stroke" Value="#F59E0B" />
                                <Setter Property="BackgroundColor" Value="#E62A1E0D" />
                            </DataTrigger>
                            <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="ÉPIQUE">
                                <Setter Property="Stroke" Value="#A855F7" />
                                <Setter Property="BackgroundColor" Value="#E6251433" />
                            </DataTrigger>
                            <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="RARE">
                                <Setter Property="Stroke" Value="#38BDF8" />
                                <Setter Property="BackgroundColor" Value="#E60E2338" />
                            </DataTrigger>
                            <DataTrigger TargetType="Border" Binding="{Binding Rarete}" Value="COMMUNE">
                                <Setter Property="Stroke" Value="#9CA3AF" />
                                <Setter Property="BackgroundColor" Value="#E61E2024" />
                            </DataTrigger>
                        </Border.Triggers>

                        <Label
                            Text="{Binding Rarete, StringFormat='★ {0}'}"
                            FontSize="11"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                    </Border>
                </Grid>
            </Border>

            <!-- 3. Identité du véhicule -->
            <VerticalStackLayout Spacing="4">
                <Label
                    Text="{Binding Marque}"
                    FontSize="28"
                    FontAttributes="Bold"
                    TextColor="#FFFFFF" />

                <Label
                    Text="{Binding ModeleComplet}"
                    FontSize="18"
                    TextColor="#E63946"
                    FontAttributes="Bold" />

                <Label
                    Text="{Binding Annee, StringFormat='Année modèle {0}'}"
                    FontSize="13"
                    TextColor="#9CA3AF" />
            </VerticalStackLayout>

            <!-- 4. Grille de spécifications techniques saillante (2x2) -->
            <Label
                Text="SPÉCIFICATIONS TECHNIQUES"
                FontSize="12"
                FontAttributes="Bold"
                TextColor="#E63946"
                CharacterSpacing="1"
                Margin="0,8,0,0" />

            <Grid ColumnDefinitions="*,*" RowDefinitions="Auto,Auto" ColumnSpacing="12" RowSpacing="12">
                <!-- Tuile 1 : Vitesse maximale -->
                <Border
                    Grid.Row="0"
                    Grid.Column="0"
                    Stroke="#331A20"
                    StrokeThickness="1"
                    StrokeShape="RoundRectangle 12"
                    BackgroundColor="#1E1014"
                    Padding="14,12">
                    <VerticalStackLayout Spacing="4">
                        <Label Text="🏎️" FontSize="20" />
                        <Label
                            Text="VITESSE MAX"
                            FontSize="10"
                            FontAttributes="Bold"
                            TextColor="#9CA3AF"
                            CharacterSpacing="1" />
                        <Label
                            Text="{Binding VitesseMaxFormatee}"
                            FontSize="18"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                    </VerticalStackLayout>
                </Border>

                <!-- Tuile 2 : 0 à 100 km/h -->
                <Border
                    Grid.Row="0"
                    Grid.Column="1"
                    Stroke="#331A20"
                    StrokeThickness="1"
                    StrokeShape="RoundRectangle 12"
                    BackgroundColor="#1E1014"
                    Padding="14,12">
                    <VerticalStackLayout Spacing="4">
                        <Label Text="⏱️" FontSize="20" />
                        <Label
                            Text="0 À 100 KM/H"
                            FontSize="10"
                            FontAttributes="Bold"
                            TextColor="#9CA3AF"
                            CharacterSpacing="1" />
                        <Label
                            Text="{Binding ZeroACentFormate}"
                            FontSize="18"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                    </VerticalStackLayout>
                </Border>

                <!-- Tuile 3 : Puissance en chevaux -->
                <Border
                    Grid.Row="1"
                    Grid.Column="0"
                    Stroke="#331A20"
                    StrokeThickness="1"
                    StrokeShape="RoundRectangle 12"
                    BackgroundColor="#1E1014"
                    Padding="14,12">
                    <VerticalStackLayout Spacing="4">
                        <Label Text="⚡" FontSize="20" />
                        <Label
                            Text="PUISSANCE"
                            FontSize="10"
                            FontAttributes="Bold"
                            TextColor="#9CA3AF"
                            CharacterSpacing="1" />
                        <Label
                            Text="{Binding PuissanceFormatee}"
                            FontSize="18"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                    </VerticalStackLayout>
                </Border>

                <!-- Tuile 4 : Année de construction -->
                <Border
                    Grid.Row="1"
                    Grid.Column="1"
                    Stroke="#331A20"
                    StrokeThickness="1"
                    StrokeShape="RoundRectangle 12"
                    BackgroundColor="#1E1014"
                    Padding="14,12">
                    <VerticalStackLayout Spacing="4">
                        <Label Text="📅" FontSize="20" />
                        <Label
                            Text="ANNÉE"
                            FontSize="10"
                            FontAttributes="Bold"
                            TextColor="#9CA3AF"
                            CharacterSpacing="1" />
                        <Label
                            Text="{Binding Annee}"
                            FontSize="18"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                    </VerticalStackLayout>
                </Border>
            </Grid>

            <!-- 5. Bloc de cotation officielle et marché (RM001) -->
            <Border
                Stroke="#331A20"
                StrokeThickness="1"
                StrokeShape="RoundRectangle 14"
                BackgroundColor="#1E1014"
                Padding="16"
                Margin="0,4,0,0">
                <VerticalStackLayout Spacing="8">
                    <Label
                        Text="COURS OFFICIEL DE RÉFÉRENCE (RM001)"
                        FontSize="11"
                        FontAttributes="Bold"
                        TextColor="#9CA3AF"
                        CharacterSpacing="1" />

                    <HorizontalStackLayout Spacing="8" VerticalOptions="Center">
                        <Label
                            Text="{Binding PrixOfficielFormate}"
                            FontSize="26"
                            FontAttributes="Bold"
                            TextColor="#FBBF24" />
                    </HorizontalStackLayout>

                    <Label
                        Text="Prix officiel calculé par le serveur. Les offres sur le marché libre peuvent fluctuer selon l'offre et la demande."
                        FontSize="12"
                        TextColor="#6B7280" />
                </VerticalStackLayout>
            </Border>

            <!-- 6. Description & Historique -->
            <Border
                Stroke="#331A20"
                StrokeThickness="1"
                StrokeShape="RoundRectangle 14"
                BackgroundColor="#1E1014"
                Padding="16">
                <VerticalStackLayout Spacing="8">
                    <Label
                        Text="DESCRIPTION &amp; HISTORIQUE"
                        FontSize="11"
                        FontAttributes="Bold"
                        TextColor="#E63946"
                        CharacterSpacing="1" />

                    <Label
                        Text="{Binding Description}"
                        FontSize="13"
                        TextColor="#D1D5DB"
                        LineHeight="1.4" />
                </VerticalStackLayout>
            </Border>

            <!-- 7. Boutons d'action -->
            <VerticalStackLayout Spacing="10" Margin="0,8,0,0">
                <Button
                    Text="VOIR LES OFFRES SUR LE MARCHÉ"
                    Command="{Binding VoirOffresMarcheCommand}"
                    BackgroundColor="#E63946"
                    TextColor="#FFFFFF"
                    FontAttributes="Bold"
                    FontSize="14"
                    CornerRadius="10"
                    HeightRequest="50" />

                <Button
                    Text="RETOUR AU CATALOGUE"
                    Command="{Binding RetourCommand}"
                    BackgroundColor="Transparent"
                    TextColor="#9CA3AF"
                    BorderColor="#331A20"
                    BorderWidth="1"
                    FontSize="13"
                    CornerRadius="10"
                    HeightRequest="44" />
            </VerticalStackLayout>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

---

### 3.5 `AutoMasters/Vues/FicheCartePage.xaml.cs`

```csharp
namespace AutoMasters.Vues;

/// <summary>
/// Page de consultation détaillée d'une carte automobile.
/// Respecte strictement la pureté du code-behind BTS SIO SLAM :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
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

---

### 3.6 `AutoMasters/VuesModeles/FicheCarteVueModele.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel responsable de la fiche détaillée d'un véhicule (Fonctionnalité F22).
/// Reçoit la carte sélectionnée ou son identifiant via Shell QueryProperty,
/// charge les spécifications depuis ServiceAPI si nécessaire et navigue vers le marché.
/// Conforme aux conventions BTS SIO SLAM 2ème année.
/// </summary>
[QueryProperty(nameof(CarteSelectionnee), "carte")]
[QueryProperty(nameof(CarteId), "id")]
public partial class FicheCarteVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private string carteId = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitrePage))]
    [NotifyPropertyChangedFor(nameof(Marque))]
    [NotifyPropertyChangedFor(nameof(Modele))]
    [NotifyPropertyChangedFor(nameof(Variante))]
    [NotifyPropertyChangedFor(nameof(ModeleComplet))]
    [NotifyPropertyChangedFor(nameof(Annee))]
    [NotifyPropertyChangedFor(nameof(Puissance))]
    [NotifyPropertyChangedFor(nameof(PuissanceFormatee))]
    [NotifyPropertyChangedFor(nameof(VitesseMax))]
    [NotifyPropertyChangedFor(nameof(VitesseMaxFormatee))]
    [NotifyPropertyChangedFor(nameof(ZeroACent))]
    [NotifyPropertyChangedFor(nameof(ZeroACentFormate))]
    [NotifyPropertyChangedFor(nameof(Rarete))]
    [NotifyPropertyChangedFor(nameof(PrixOfficiel))]
    [NotifyPropertyChangedFor(nameof(PrixOfficielFormate))]
    [NotifyPropertyChangedFor(nameof(ExemplairesEnCirculation))]
    [NotifyPropertyChangedFor(nameof(ExemplairesEnCirculationFormate))]
    [NotifyPropertyChangedFor(nameof(ImageUrl))]
    [NotifyPropertyChangedFor(nameof(Description))]
    private Carte? carteSelectionnee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private string messageErreur = "";

    [ObservableProperty]
    private bool aErreur;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// Utilisé lors de l'instanciation de la vue.
    /// </summary>
    public FicheCarteVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP pour les tests unitaires.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
    public FicheCarteVueModele(ServiceAPI serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public string TitrePage => CarteSelectionnee != null
        ? $"{CarteSelectionnee.Marque} {CarteSelectionnee.Modele}"
        : "Fiche Véhicule";

    public string Marque => CarteSelectionnee?.Marque ?? "";

    public string Modele => CarteSelectionnee?.Modele ?? "";

    public string? Variante => CarteSelectionnee?.Variante;

    public string ModeleComplet => string.IsNullOrWhiteSpace(Variante)
        ? Modele
        : $"{Modele} ({Variante})";

    public int Annee => CarteSelectionnee?.Annee ?? 0;

    public int Puissance => CarteSelectionnee?.Puissance ?? 0;

    public string PuissanceFormatee => CarteSelectionnee?.PuissanceFormatee ?? $"{Puissance} ch";

    public int VitesseMax => CarteSelectionnee?.VitesseMax ?? 0;

    public string VitesseMaxFormatee => CarteSelectionnee?.VitesseMaxFormatee ?? $"{VitesseMax} km/h";

    public double ZeroACent => CarteSelectionnee?.ZeroACent ?? 0.0;

    public string ZeroACentFormate => CarteSelectionnee?.ZeroACentFormate ?? $"{ZeroACent:F1} s";

    public string Rarete => CarteSelectionnee?.Rarete ?? "COMMUNE";

    public decimal PrixOfficiel => CarteSelectionnee?.PrixOfficiel ?? 0m;

    public string PrixOfficielFormate => CarteSelectionnee?.PrixOfficielFormate ?? $"{PrixOfficiel:N0} coins";

    public int ExemplairesEnCirculation => CarteSelectionnee?.ExemplairesEnCirculation ?? 0;

    public string ExemplairesEnCirculationFormate => ExemplairesEnCirculation > 0
        ? $"{ExemplairesEnCirculation:N0} exemplaires en circulation"
        : "Tirage non spécifié";

    public string ImageUrl => CarteSelectionnee?.ImageUrl ?? "";

    public string Description => !string.IsNullOrWhiteSpace(CarteSelectionnee?.Description)
        ? CarteSelectionnee.Description
        : "Véhicule d'exception de la collection officielle AutoMasters.";

    #endregion

    #region Methodes

    /// <summary>
    /// Réaction à la réception de l'identifiant de carte via Shell QueryProperty.
    /// Si l'objet complet n'est pas déjà présent en mémoire, lance le chargement API.
    /// </summary>
    partial void OnCarteIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && (CarteSelectionnee == null || CarteSelectionnee.Identifiant != value))
        {
            _ = ChargerDetailsAsync(value);
        }
    }

    /// <summary>
    /// Récupère la fiche technique complète de la carte via son identifiant (F09).
    /// </summary>
    /// <param name="id">Identifiant de la carte à charger.</param>
    public async Task ChargerDetailsAsync(string? id = null)
    {
        string cibleId = id ?? CarteId;
        if (string.IsNullOrWhiteSpace(cibleId))
            return;

        EstOccupe = true;
        MessageErreur = "";
        AErreur = false;

        try
        {
            var carteChargee = await serviceApi.RecupererFicheCarteAsync(cibleId);
            if (carteChargee != null)
            {
                CarteSelectionnee = carteChargee;
            }
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
            AErreur = true;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Commande publique de rechargement.
    /// </summary>
    [RelayCommand]
    private async Task ChargerDetails()
    {
        await ChargerDetailsAsync(CarteId);
    }

    /// <summary>
    /// Navigue vers la place de marché (MarchePage) avec filtre ciblé sur cette carte.
    /// </summary>
    [RelayCommand]
    private async Task VoirOffresMarcheAsync()
    {
        if (CarteSelectionnee == null && string.IsNullOrWhiteSpace(CarteId))
            return;

        string id = CarteSelectionnee?.Identifiant ?? CarteId;
        var parametres = new Dictionary<string, object>
        {
            { "cardId", id },
            { "search", CarteSelectionnee?.Marque ?? "" }
        };

        // Redirection vers l'onglet Marché via Shell
        await Shell.Current.GoToAsync("//MarchePage", parametres);
    }

    /// <summary>
    /// Revient à la page précédente dans la pile de navigation.
    /// </summary>
    [RelayCommand]
    private async Task RetourAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    #endregion
}
```

---

### 3.7 Enregistrement des Routes dans `AutoMasters/AppShell.xaml.cs`

Pour assurer la navigation Shell fluide entre `CataloguePage` et `FicheCartePage`, le constructeur de `AppShell.xaml.cs` doit enregistrer la route détaillée :

```csharp
namespace AutoMasters;

public partial class AppShell : Shell
{
    #region Attributs

    #endregion

    #region Constructeurs

    public AppShell()
    {
        InitializeComponent();

        // Enregistrement des routes de navigation pour les écrans détaillés
        Routing.RegisterRoute(nameof(Vues.FicheCartePage), typeof(Vues.FicheCartePage));
        Routing.RegisterRoute("FicheCartePage", typeof(Vues.FicheCartePage));
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
```

---

## 4. Matrice de Conformité et Validation

| Exigence | Observation / Spécification | Mécanisme de Conformité |
|---|---|---|
| **F21.01 Catalogue XAML** | `Vues/CataloguePage.xaml` créé | Conforme aux tests E2E `F21_01_CatalogPage_XamlMustExist` |
| **F21.02 Barre de recherche** | `SearchBar` avec `TexteRecherche` | Conforme à `F21_02_CatalogPage_MustContainSearchBar` |
| **F21.03 CollectionView / Grille** | `CollectionView` avec `GridItemsLayout Span="2"` | Conforme à `F21_03_CatalogPage_MustContainCollectionViewOrGrid` |
| **F21.04 ObservableCollection** | `ObservableCollection<Carte>` dans le ViewModel | Conforme à `F21_04_CatalogViewModel_MustExposeCartesObservableCollection` |
| **F21.05 Filtrage Rareté & Recherche** | Méthode `AppliquerFiltres()` en mémoire | Conforme à `F21_05_CatalogViewModel_MustFilterByRarityAndSearch` |
| **F21.B01 Debounce & Réactivité** | Filtrage synchrone direct sur `OnTexteRechercheChanged` | Conforme à `F21_B01_Catalog_SearchDebounceDelay_300ms` |
| **F21.B02 État vide** | Balise `<CollectionView.EmptyView>` | Conforme à `F21_B02_Catalog_ZeroMatches_DisplaysEmptyState` |
| **F21.B04 Réinitialisation** | Commande `ReinitialiserFiltresCommand` restaurant toutes les cartes | Conforme à `F21_B04_Catalog_ClearingSearch_RestoresAllItems` |
| **F21.B05 Sélection carte** | `SelectionnerCarteCommand` naviguant vers `FicheCartePage` | Conforme à `F21_B05_Catalog_SelectCardCommand_SetsSelectedCard` |
| **F22 Fiche Carte détaillée** | `FicheCartePage.xaml` & `FicheCarteVueModele.cs` | Spécifications complètes (Vitesse, 0-100, Puissance, Prix, Rareté) |
| **F22 Bouton Marché** | `VoirOffresMarcheCommand` naviguant vers `//MarchePage` | Navigation inter-onglets avec `cardId` |
| **F28 Pureté Code-behind** | Constructeurs avec `InitializeComponent()` uniquement | 100% conforme à `ValidateCodeBehindPurity` (0 HttpClient, 0 ServiceAPI) |
| **Discipline Régions BTS SIO** | `#region Attributs`, `#region Constructeurs`, etc. | 100% conforme à `ValidateRegionDiscipline` |

---

## 5. Recommandations pour l'Étape d'Implémentation

1. **Création des fichiers :** L'agent développeur devra créer :
   - `AutoMasters/Vues/CataloguePage.xaml` et `AutoMasters/Vues/CataloguePage.xaml.cs`
   - `AutoMasters/VuesModeles/CatalogueVueModele.cs`
   - `AutoMasters/Vues/FicheCartePage.xaml` et `AutoMasters/Vues/FicheCartePage.xaml.cs`
   - `AutoMasters/VuesModeles/FicheCarteVueModele.cs`
2. **Mise à jour `AppShell` :**
   - Ajouter l'onglet Catalogue dans `AppShell.xaml` (`TabBar` à 5 onglets : `CataloguePage`, `MarchePage`, `OrdresPage`, `GaragePage`, `ActivitePage`).
   - Ajouter `Routing.RegisterRoute(nameof(FicheCartePage), typeof(FicheCartePage));` dans le constructeur de `AppShell.xaml.cs`.
3. **Validation :**
   - Exécuter `dotnet build` pour confirmer l'absence d'erreurs de syntaxe XAML ou SourceGen inflator.
   - Exécuter `dotnet test` pour confirmer l'activation des tests M3 F21.
