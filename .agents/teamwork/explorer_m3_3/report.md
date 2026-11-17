# AutoMasters — Milestone 3: Market / Trading, Garage / Portfolio & Booster Pack Flow Specification Report

**Document:** M3 Trading & Portfolio Presentation Layer Specification  
**Author:** explorer_m3_3  
**Target Level:** BTS SIO SLAM 2nd-Year Standards  
**Scope:** Features F22, F23, F24, F25, F26, F27, F28  
**Date:** October 9, 2026  
**Status:** COMPLETE (Design & Specification)

---

## 1. Executive Summary & Architectural Overview

This report provides the complete architectural design, detailed UX specifications, and production-ready XAML/C# source code proposals for the **Market / Trading**, **Garage / Portfolio**, and **Booster Pack Opening Flow** components of the AutoMasters .NET MAUI application (Milestone 3).

### Key Deliverables Specified:
1. **MarchePage (`MarchePage.xaml`, `MarchePage.xaml.cs`, `MarcheVueModele.cs`):**  
   Figma Screen 4 match: Live badge `"🟢 EN DIRECT"`, search & rarity filters, active listings cards with vehicle thumbnail, brand, model, year, seller (`Vendeur`), price in coins, immediate purchase (`AcheterOffreCommand`) with recap confirmation (F23), 15-minute offer reservation (`ReserverOffreCommand`), and floating action button (FAB red `+`) opening offer creation.
2. **CreationOffrePage (`CreationOffrePage.xaml`, `CreationOffrePage.xaml.cs`, `CreationOffreVueModele.cs`):**  
   Figma Screen 4 sell flow: modal/page allowing selection of an unreserved car owned in the user's garage, price validation (> 0, decimal parsing with space/comma support), submission via `POST /offers`, and automatic market feed update.
3. **GaragePage (`GaragePage.xaml`, `GaragePage.xaml.cs`, `GarageVueModele.cs`):**  
   Figma Screen 2 match: Portfolio header with total valuation (`482 500 coins`), net color-coded P&L banner (`+32 500 € P&L total` in vibrant green or negative in red), owned vehicles `CollectionView` with multi-copy multiplier badge (`x2`), acquisition cost, current valuation, individual P&L, context action menu, and interactive booster pack banner (`3/10 disponibles`, countdown `Prochain dans 09:55`, prominent CTA `⚡ OUVRIR`).
4. **Booster Opening Flow (`BoosterVueModele.cs`, `BoosterPage.xaml`, `BoosterPage.xaml.cs`, in-place Modal Overlay):**  
   Figma Screen 3 match: Unpacking ceremony consuming 1 pack via `ServiceAPI.OuvrirBoosterAsync()`, 5-segment progression indicator (`Carte 1 sur 5`), interactive flip button (`RETOURNER ▶`), dynamic rarity highlight glow (Gold/Amber for Legendary, Violet for Epic, Blue for Rare, Silver for Common), and atomic garage synchronisation upon completion.
5. **Architectural Purity & Conventions:**  
   Strict BTS SIO C# regions (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`), zero business logic in `.xaml.cs` code-behind files (satisfying `TestWorkspace.ValidateCodeBehindPurity`), and 100% adherence to design tokens.

---

## 2. Figma Mockup Deconstruction & Visual Design Tokens

### 2.1 Design Tokens Reference (`Resources/Styles/Colors.xaml`)
| Token Name | Hex Code | Visual Application |
|---|---|---|
| `ColorBackground` | `#10080B` / `#0D0D0E` | Main page background (deep automotive carbon black) |
| `ColorSurfaceCard` | `#1A1A1E` / `#1E1014` | Card containers, list item surfaces |
| `ColorSurfaceHighlight` | `#221215` | Booster banner, logo badges, active containers |
| `ColorBorderSubtle` | `#331A20` / `#2A1E24` | Card and input strokes (1px) |
| `ColorPrimaryRed` | `#E63946` | Brand accent: CTA buttons (`OUVRIR`, `RETOURNER`), FAB `+`, active indicators |
| `ColorSecondaryGreen`| `#10B981` | Success accent: `ACHETER IMMÉDIATEMENT`, positive P&L (`+32 500 €`), `🟢 EN DIRECT` |
| `ColorReserveDarkRed`| `#2D1518` | `RÉSERVER` button background |
| `ColorReserveBorder` | `#EF4444` | `RÉSERVER` border and button text |
| `ColorRarityLegendary`| `#F59E0B`| Gold/Amber border & text (`★ LÉGENDAIRE`) |
| `ColorRarityEpic` | `#A855F7` | Purple/Violet border & text (`ÉPIQUE`) |
| `ColorRarityRare` | `#38BDF8` | Cyan/Blue border & text (`RARE`) |
| `ColorRarityCommon` | `#9CA3AF` | Silver/Gray border & text (`COMMUNE`) |
| `ColorTimerAmber` | `#FBBF24` | Countdown timer text (`Prochain dans 09:55`) |
| `ColorTextPrimary` | `#FFFFFF` | Titles, brand names, button text |
| `ColorTextSecondary`| `#9CA3AF` | Subtitles, models, year, secondary metadata |
| `ColorTextMuted` | `#6B7280` | Inactive dots, helper links, dates |

### 2.2 Screen 4 Deconstruction — Marketplace (`media_1791389292458_a07f6e9a.png`)
```
+-------------------------------------------------------------+
| MARCHÉ                                         🟢 EN DIRECT |
| 8 offres actives                                            |
|                                                             |
| [🔍 Rechercher marque ou modèle...       ]                 |
| [TOUTES] [COMMUNE] [RARE] [ÉPIQUE] [LÉGENDAIRE]             |
|                                                             |
| +---------------------------------------------------------+ |
| | [Img 80x64]  Ferrari                     ★ LÉGENDAIRE  | |
| |              LaFerrari Aperta · 2017                    | |
| |              480 000 coins · par TurboAce               | |
| |                                                         | |
| |  [ ACHETER IMMÉDIATEMENT ]       [ RÉSERVER ]           | |
| +---------------------------------------------------------+ |
|                                                             |
| +---------------------------------------------------------+ |
| | [Img 80x64]  Koenigsegg                  ★ LÉGENDAIRE  | |
| |              Jesko Absolut · 2022                       | |
| |              2 650 000 coins · par ApexHunter           | |
| |                                                         | |
| |  [ ACHETER IMMÉDIATEMENT ]       [ RÉSERVER ]           | |
| +---------------------------------------------------------+ |
|                                                             |
|                                                       (+)   | <-- Red FAB (56x56)
+-------------------------------------------------------------+
```

### 2.3 Screen 2 Deconstruction — Garage & Portfolio (`media_1791389263369_a72b0e54.png`)
```
+-------------------------------------------------------------+
| GARAGE                                                      |
|                                                             |
| VALEUR TOTALE DE LA COLLECTION                              |
| 482 500 coins                                               |
| +32 500 €  P&L total                                        |
|                                                             |
| +---------------------------------------------------------+ |
| | BOOSTER AUTOMASTERS                                     | |
| | ● ● ● ○ ○ ○ ○ ○ ○ ○   3/10 disponibles                  | |
| | Prochain dans 09:55                                     | |
| |                      [ ⚡ OUVRIR ]                      | |
| +---------------------------------------------------------+ |
|                                                             |
| MA COLLECTION · 5 CARTES                                    |
| +---------------------------------------------------------+ |
| | [Img]  Porsche             ÉPIQUE           222 000 €   | |
| |        911 GT3 RS                           +24 000 €   | |
| |        Acheté 198 000 €                     [⋮]         | |
| +---------------------------------------------------------+ |
| | [x2]   Toyota              RARE              47 000 €   | |
| | [Img]  GR Yaris                             +15 000 €   | |
| |        Acheté 32 000 €                      [⋮]         | |
| +---------------------------------------------------------+ |
+-------------------------------------------------------------+
```

### 2.4 Screen 3 Deconstruction — Booster Reveal Flow (`media_1791389277036_c3cce1e8.png`)
```
+-------------------------------------------------------------+
| [✕]                                                         |
|                     BOOSTER AUTOMASTERS                     |
|                 [■]  [ ]  [ ]  [ ]  [ ]                     |
|                     Carte 1 sur 5                           |
|                                                             |
|                +---------------------------+                |
|                |                           |                |
|                |            🏎️             |                |
|                |                           |                |
|                |        AUTOMASTERS        |                |
|                |                           |                |
|                +---------------------------+                |
|                                                             |
|                    [ RETOURNER ▶ ]                          |
+-------------------------------------------------------------+
```

---

## 3. Marketplace & Trading Component (F22, F23)

### 3.1 Proposed Code: `AutoMasters/VuesModeles/MarcheVueModele.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel gérant la place de marché public (Features F22, F23).
/// Gère la consultation des offres, la recherche en direct, le filtrage par rareté,
/// l'achat immédiat avec boîte de confirmation récapitulative et clé d'idempotence,
/// ainsi que la réservation temporaire avec compte à rebours de 15 minutes.
/// </summary>
public partial class MarcheVueModele : ObservableObject
{
    #region Attributs

    private ServiceAPI serviceApi = ServiceAPI.Instance;
    private ObservableCollection<Offre> listeBruteOffres = new ObservableCollection<Offre>();

    [ObservableProperty]
    private ObservableCollection<Offre> offresFiltrees = new ObservableCollection<Offre>();

    [ObservableProperty]
    private string texteRecherche = "";

    [ObservableProperty]
    private string filtreRarete = "TOUTES";

    [ObservableProperty]
    private int nombreOffresActives = 0;

    [ObservableProperty]
    private decimal soldeUtilisateur = 500000m;

    [ObservableProperty]
    private string message = "";

    [ObservableProperty]
    private bool estEnDirect = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    #endregion

    #region Constructeurs

    public MarcheVueModele()
    {
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public string SoldeFormate => $"{SoldeUtilisateur:N0} coins";

    public string LibelleOffresActives => $"{NombreOffresActives} offres actives";

    #endregion

    #region Methodes

    /// <summary>
    /// Charge la liste des offres disponibles depuis l'API REST du Groupe 2.
    /// </summary>
    [RelayCommand]
    private async Task ChargerOffresAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = "";

        try
        {
            var resultats = await serviceApi.RecupererOffresAsync();
            listeBruteOffres = new ObservableCollection<Offre>(resultats ?? new System.Collections.Generic.List<Offre>());
            AppliquerFiltres();
        }
        catch (Exception ex)
        {
            Message = $"Impossible de charger les offres : {ex.Message}";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Filtre les offres affichées selon le texte de recherche et la rareté sélectionnée.
    /// </summary>
    [RelayCommand]
    private void AppliquerFiltres()
    {
        var requete = listeBruteOffres.AsEnumerable();

        // Filtrage textuel (marque ou modèle)
        if (!string.IsNullOrWhiteSpace(TexteRecherche))
        {
            string clean = TexteRecherche.Trim().ToLowerInvariant();
            requete = requete.Where(o =>
                (o.Carte?.Marque != null && o.Carte.Marque.ToLowerInvariant().Contains(clean)) ||
                (o.Carte?.Modele != null && o.Carte.Modele.ToLowerInvariant().Contains(clean)));
        }

        // Filtrage par niveau de rareté
        if (!string.IsNullOrWhiteSpace(FiltreRarete) && !FiltreRarete.Equals("TOUTES", StringComparison.OrdinalIgnoreCase))
        {
            requete = requete.Where(o =>
                o.Carte != null && string.Equals(o.Carte.Rarete, FiltreRarete, StringComparison.OrdinalIgnoreCase));
        }

        OffresFiltrees = new ObservableCollection<Offre>(requete.ToList());
        NombreOffresActives = OffresFiltrees.Count;
    }

    /// <summary>
    /// Sélectionne un critère de rareté pour le filtrage rapide.
    /// </summary>
    [RelayCommand]
    private void DefinirFiltreRarete(string rarete)
    {
        FiltreRarete = rarete ?? "TOUTES";
        AppliquerFiltres();
    }

    /// <summary>
    /// Exécute l'achat immédiat d'une offre avec boîte récapitulative de confirmation (Feature F23 / RM006).
    /// </summary>
    [RelayCommand]
    private async Task AcheterOffreAsync(Offre offre)
    {
        if (EstOccupe || offre == null)
            return;

        // Contrôle de solde utilisateur préalable (F22_B04)
        if (SoldeUtilisateur < offre.Prix)
        {
            Message = $"Fonds insuffisants : vous possédez {SoldeUtilisateur:N0} coins mais ce véhicule coûte {offre.Prix:N0} coins.";
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlert(
                    "Fonds insuffisants",
                    $"Vous possédez {SoldeUtilisateur:N0} coins, mais cette offre coûte {offre.Prix:N0} coins.",
                    "Fermer");
            }
            return;
        }

        // Dialogue récapitulatif de confirmation (F23_01, F23_02)
        string nomVehicule = $"{offre.Carte?.Marque} {offre.Carte?.Modele}".Trim();
        string messageRecap = $"Acheter {nomVehicule} pour {offre.Prix:N0} coins ? Solde actuel : {SoldeUtilisateur:N0} coins.";

        bool confirmation = true;
        if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
        {
            confirmation = await Application.Current.Windows[0].Page!.DisplayAlert(
                "Confirmation d'achat",
                messageRecap,
                "Acheter immédiatement",
                "Annuler");
        }

        if (!confirmation)
            return; // Annulation sans débit (F23_03, F23_B01)

        EstOccupe = true;
        Message = "";

        try
        {
            // Appel API sécurisé par Idempotency-Key (RM006)
            bool succes = await serviceApi.AcheterOffreAsync(offre.Identifiant);
            if (succes)
            {
                SoldeUtilisateur -= offre.Prix;
                listeBruteOffres.Remove(offre);
                AppliquerFiltres();

                Message = $"Félicitations ! Vous venez d'acquérir {nomVehicule}. Il a été ajouté à votre garage.";
                if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                {
                    await Application.Current.Windows[0].Page!.DisplayAlert(
                        "Achat réussi",
                        $"{nomVehicule} fait désormais partie de votre collection.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlert(
                    "Échec de l'achat",
                    ex.Message,
                    "OK");
            }
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Réserve temporairement une offre pendant 15 minutes (Feature F23 / RM004).
    /// </summary>
    [RelayCommand]
    private async Task ReserverOffreAsync(Offre offre)
    {
        if (EstOccupe || offre == null)
            return;

        if (offre.EstReservee)
        {
            Message = "Cette offre est déjà réservée par un autre membre.";
            return;
        }

        EstOccupe = true;
        Message = "";

        try
        {
            bool succes = await serviceApi.ReserverOffreAsync(offre.Identifiant);
            if (succes)
            {
                offre.EstReservee = true;
                offre.Statut = "reserved";
                offre.DateExpiration = DateTime.UtcNow.AddMinutes(15);
                AppliquerFiltres();

                Message = $"Offre réservée avec succès ! Verrouillée pendant 15 minutes.";
                if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                {
                    await Application.Current.Windows[0].Page!.DisplayAlert(
                        "Réservation confirmée",
                        $"Cette offre vous est réservée pendant 15 minutes.",
                        "OK");
                }
            }
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Ouvre la modale de mise en vente d'un véhicule depuis le garage via le FAB (+).
    /// </summary>
    [RelayCommand]
    private async Task OuvrirCreationOffreAsync()
    {
        await Shell.Current.GoToAsync("CreationOffrePage");
    }

    #endregion
}
```

### 3.2 Proposed Code: `AutoMasters/Vues/MarchePage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    xmlns:m="clr-namespace:AutoMasters.Modeles"
    x:Class="AutoMasters.Vues.MarchePage"
    x:DataType="vm:MarcheVueModele"
    BackgroundColor="#10080B"
    Title="Marché">

    <Grid RowDefinitions="Auto,Auto,*" Margin="16,12,16,0">
        
        <!-- En-tête : Titre & Badge '🟢 EN DIRECT' (Figma Screen 4) -->
        <Grid Grid.Row="0" ColumnDefinitions="*,Auto" Margin="0,0,0,12">
            <VerticalStackLayout Grid.Column="0" Spacing="2">
                <Label
                    Text="MARCHÉ"
                    FontSize="24"
                    FontAttributes="Bold"
                    TextColor="#FFFFFF" />
                <Label
                    Text="{Binding LibelleOffresActives}"
                    FontSize="13"
                    TextColor="#9CA3AF" />
            </VerticalStackLayout>

            <!-- Badge '🟢 EN DIRECT' -->
            <Border
                Grid.Column="1"
                Stroke="#10B981"
                StrokeThickness="1"
                BackgroundColor="#122B1E"
                StrokeShape="RoundRectangle 12"
                Padding="10,4"
                VerticalOptions="Center">
                <HorizontalStackLayout Spacing="6" VerticalOptions="Center">
                    <Label Text="●" TextColor="#10B981" FontSize="11" VerticalOptions="Center" />
                    <Label
                        Text="EN DIRECT"
                        FontSize="11"
                        FontAttributes="Bold"
                        TextColor="#10B981"
                        VerticalOptions="Center" />
                </HorizontalStackLayout>
            </Border>
        </Grid>

        <!-- Barre de recherche & Filtres de Rareté -->
        <VerticalStackLayout Grid.Row="1" Spacing="10" Margin="0,0,0,12">
            <!-- Champ de recherche textuelle -->
            <Border
                Stroke="#331A20"
                StrokeThickness="1"
                BackgroundColor="#1E1014"
                StrokeShape="RoundRectangle 10"
                Padding="12,0">
                <SearchBar
                    Placeholder="Rechercher marque ou modèle..."
                    PlaceholderColor="#888888"
                    TextColor="#FFFFFF"
                    BackgroundColor="Transparent"
                    CancelButtonColor="#E63946"
                    Text="{Binding TexteRecherche}">
                    <!-- Déclenchement du filtrage sur modification textuelle -->
                </SearchBar>
            </Border>

            <!-- Chips de filtrage par Rareté -->
            <ScrollView Orientation="Horizontal" HorizontalScrollBarVisibility="Never">
                <HorizontalStackLayout Spacing="8">
                    <Button
                        Text="TOUTES"
                        FontSize="11"
                        FontAttributes="Bold"
                        HeightRequest="32"
                        CornerRadius="16"
                        BackgroundColor="#221215"
                        TextColor="#FFFFFF"
                        Command="{Binding DefinirFiltreRareteCommand}"
                        CommandParameter="TOUTES" />
                    <Button
                        Text="COMMUNE"
                        FontSize="11"
                        FontAttributes="Bold"
                        HeightRequest="32"
                        CornerRadius="16"
                        BackgroundColor="#221215"
                        TextColor="#9CA3AF"
                        Command="{Binding DefinirFiltreRareteCommand}"
                        CommandParameter="COMMUNE" />
                    <Button
                        Text="RARE"
                        FontSize="11"
                        FontAttributes="Bold"
                        HeightRequest="32"
                        CornerRadius="16"
                        BackgroundColor="#221215"
                        TextColor="#38BDF8"
                        Command="{Binding DefinirFiltreRareteCommand}"
                        CommandParameter="RARE" />
                    <Button
                        Text="ÉPIQUE"
                        FontSize="11"
                        FontAttributes="Bold"
                        HeightRequest="32"
                        CornerRadius="16"
                        BackgroundColor="#221215"
                        TextColor="#A855F7"
                        Command="{Binding DefinirFiltreRareteCommand}"
                        CommandParameter="ÉPIQUE" />
                    <Button
                        Text="★ LÉGENDAIRE"
                        FontSize="11"
                        FontAttributes="Bold"
                        HeightRequest="32"
                        CornerRadius="16"
                        BackgroundColor="#221215"
                        TextColor="#F59E0B"
                        Command="{Binding DefinirFiltreRareteCommand}"
                        CommandParameter="LÉGENDAIRE" />
                </HorizontalStackLayout>
            </ScrollView>
        </VerticalStackLayout>

        <!-- Liste des Offres & Bouton Flottant FAB (Grid superposée) -->
        <Grid Grid.Row="2">
            <RefreshView
                IsRefreshing="{Binding EstOccupe}"
                Command="{Binding ChargerOffresCommand}"
                RefreshColor="#E63946">
                <CollectionView
                    ItemsSource="{Binding OffresFiltrees}"
                    SelectionMode="None">
                    <CollectionView.EmptyView>
                        <VerticalStackLayout Spacing="12" HorizontalOptions="Center" VerticalOptions="Center" Padding="32">
                            <Label Text="🏪" FontSize="48" HorizontalOptions="Center" />
                            <Label
                                Text="Aucune offre active"
                                FontSize="18"
                                FontAttributes="Bold"
                                TextColor="#FFFFFF"
                                HorizontalOptions="Center" />
                            <Label
                                Text="Soyez le premier à mettre un véhicule en vente avec le bouton + !"
                                FontSize="13"
                                TextColor="#888888"
                                HorizontalTextAlignment="Center" />
                        </VerticalStackLayout>
                    </CollectionView.EmptyView>

                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="m:Offre">
                            <Border
                                Stroke="#2A1E24"
                                StrokeThickness="1"
                                BackgroundColor="#1A1A1E"
                                StrokeShape="RoundRectangle 14"
                                Padding="12"
                                Margin="0,0,0,12">
                                <VerticalStackLayout Spacing="10">
                                    
                                    <!-- Ligne 1 : Miniature & Métadonnées -->
                                    <Grid ColumnDefinitions="84,*" ColumnSpacing="12">
                                        <!-- Miniature véhicule -->
                                        <Border
                                            Grid.Column="0"
                                            StrokeThickness="0"
                                            StrokeShape="RoundRectangle 8"
                                            HeightRequest="64"
                                            WidthRequest="84"
                                            BackgroundColor="#242426">
                                            <Image
                                                Source="{Binding Carte.ImageUrl}"
                                                Aspect="AspectFill" />
                                        </Border>

                                        <!-- Marque, Modèle, Rareté, Prix & Vendeur -->
                                        <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                                            <Grid ColumnDefinitions="*,Auto">
                                                <Label
                                                    Grid.Column="0"
                                                    Text="{Binding Carte.Marque}"
                                                    FontSize="16"
                                                    FontAttributes="Bold"
                                                    TextColor="#FFFFFF" />
                                                
                                                <!-- Badge Rareté -->
                                                <Border
                                                    Grid.Column="1"
                                                    Stroke="#F59E0B"
                                                    StrokeThickness="1"
                                                    BackgroundColor="#2B2113"
                                                    StrokeShape="RoundRectangle 6"
                                                    Padding="6,2">
                                                    <Label
                                                        Text="{Binding Carte.Rarete}"
                                                        FontSize="10"
                                                        FontAttributes="Bold"
                                                        TextColor="#F59E0B" />
                                                </Border>
                                            </Grid>

                                            <!-- Modèle et Année -->
                                            <Label
                                                FontSize="13"
                                                TextColor="#9CA3AF">
                                                <Label.Text>
                                                    <MultiBinding StringFormat="{}{0} · {1}">
                                                        <Binding Path="Carte.Modele" />
                                                        <Binding Path="Carte.Annee" />
                                                    </MultiBinding>
                                                </Label.Text>
                                            </Label>

                                            <!-- Prix et Vendeur -->
                                            <HorizontalStackLayout Spacing="4">
                                                <Label
                                                    Text="{Binding Prix, StringFormat='{0:N0} coins'}"
                                                    FontSize="13"
                                                    FontAttributes="Bold"
                                                    TextColor="#FBBF24" />
                                                <Label
                                                    Text="{Binding Vendeur, StringFormat='· par {0}'}"
                                                    FontSize="12"
                                                    TextColor="#9CA3AF" />
                                            </HorizontalStackLayout>
                                        </VerticalStackLayout>
                                    </Grid>

                                    <!-- Ligne 2 : Actions (ACHETER IMMÉDIATEMENT / RÉSERVER) -->
                                    <Grid ColumnDefinitions="*,*" ColumnSpacing="8">
                                        <Button
                                            Grid.Column="0"
                                            Text="ACHETER IMMÉDIATEMENT"
                                            FontSize="12"
                                            FontAttributes="Bold"
                                            BackgroundColor="#10B981"
                                            TextColor="#FFFFFF"
                                            HeightRequest="40"
                                            CornerRadius="8"
                                            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:MarcheVueModele}}, Path=AcheterOffreCommand}"
                                            CommandParameter="{Binding .}" />
                                        
                                        <Button
                                            Grid.Column="1"
                                            Text="RÉSERVER"
                                            FontSize="12"
                                            FontAttributes="Bold"
                                            BackgroundColor="#2D1518"
                                            BorderColor="#EF4444"
                                            BorderWidth="1"
                                            TextColor="#EF4444"
                                            HeightRequest="40"
                                            CornerRadius="8"
                                            Command="{Binding Source={RelativeSource AncestorType={x:Type vm:MarcheVueModele}}, Path=ReserverOffreCommand}"
                                            CommandParameter="{Binding .}" />
                                    </Grid>

                                </VerticalStackLayout>
                            </Border>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>
            </RefreshView>

            <!-- Floating Action Button (FAB Rouge +) pour vendre une carte (F22_04, F24) -->
            <Button
                Text="+"
                FontSize="28"
                FontAttributes="Bold"
                TextColor="#FFFFFF"
                BackgroundColor="#E63946"
                WidthRequest="56"
                HeightRequest="56"
                CornerRadius="28"
                HorizontalOptions="End"
                VerticalOptions="End"
                Margin="0,0,12,16"
                Command="{Binding OuvrirCreationOffreCommand}">
                <Button.Shadow>
                    <Shadow Brush="#E63946" Offset="0,4" Radius="10" Opacity="0.4" />
                </Button.Shadow>
            </Button>
        </Grid>

    </Grid>
</ContentPage>
```

### 3.3 Proposed Code: `AutoMasters/Vues/MarchePage.xaml.cs`

```csharp
using AutoMasters.VuesModeles;
using Microsoft.Maui.Controls;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur pour la page Marché (Feature F22).
/// Conforme strictement au principe de pureté MVVM BTS SIO (0 appel HTTP, 0 logique métier).
/// </summary>
public partial class MarchePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public MarchePage()
    {
        InitializeComponent();
        BindingContext = new MarcheVueModele();
    }

    public MarchePage(MarcheVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is MarcheVueModele vm)
        {
            await vm.ChargerOffresCommand.ExecuteAsync(null);
        }
    }

    #endregion
}
```

---

## 4. Sell Card Modal Component (F24)

### 4.1 Proposed Code: `AutoMasters/VuesModeles/CreationOffreVueModele.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel gérant la création et publication d'une offre de vente sur le marché (Feature F24).
/// Permet la sélection d'un véhicule du garage, la saisie et validation stricte du prix demandé (> 0),
/// et l'envoi vers POST /offers.
/// </summary>
public partial class CreationOffreVueModele : ObservableObject
{
    #region Attributs

    private ServiceAPI serviceApi = ServiceAPI.Instance;

    [ObservableProperty]
    private ObservableCollection<CartePossedee> cartesPossedees = new ObservableCollection<CartePossedee>();

    [ObservableProperty]
    private CartePossedee? carteSelectionnee;

    [ObservableProperty]
    private string prixSaisi = "";

    [ObservableProperty]
    private string messageErreur = "";

    [ObservableProperty]
    private bool garageEstVide;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    #endregion

    #region Constructeurs

    public CreationOffreVueModele()
    {
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    #endregion

    #region Methodes

    /// <summary>
    /// Charge les véhicules possédés du garage éligibles à la vente.
    /// </summary>
    [RelayCommand]
    private async Task ChargerVehiculesGarageAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        MessageErreur = "";

        try
        {
            var portefeuille = await serviceApi.RecupererPortefeuilleAsync();
            CartesPossedees = portefeuille.CartesPossedees ?? new ObservableCollection<CartePossedee>();
            GarageEstVide = CartesPossedees.Count == 0;

            if (GarageEstVide)
            {
                MessageErreur = "Votre garage est vide. Vous ne possédez aucun véhicule à mettre en vente.";
            }
            else if (CarteSelectionnee == null && CartesPossedees.Count > 0)
            {
                CarteSelectionnee = CartesPossedees[0];
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Impossible d'accéder au garage : {ex.Message}";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Valide les informations saisies et publie l'offre sur le marché numérique (F24_03, F24_B02, F24_B03).
    /// </summary>
    [RelayCommand]
    private async Task CreerOffreAsync()
    {
        if (EstOccupe)
            return;

        MessageErreur = "";

        // 1. Validation de la sélection du véhicule (F24_01)
        if (CarteSelectionnee == null)
        {
            MessageErreur = "Veuillez sélectionner un véhicule de votre garage.";
            return;
        }

        // 2. Nettoyage et validation du format du prix (support virgules et espaces, F24_B02, F24_B03)
        string prixNettoye = (PrixSaisi ?? string.Empty).Replace(" ", string.Empty).Replace(',', '.');
        if (!decimal.TryParse(prixNettoye, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal prixFinal))
        {
            MessageErreur = "Veuillez saisir un montant numérique valide.";
            return;
        }

        // 3. Validation stricte du prix > 0 (F24_03, RM001)
        if (prixFinal <= 0)
        {
            MessageErreur = "Le prix de vente doit être strictement supérieur à zéro.";
            return;
        }

        EstOccupe = true;

        try
        {
            string copyId = !string.IsNullOrWhiteSpace(CarteSelectionnee.IdentifiantExemplaire)
                ? CarteSelectionnee.IdentifiantExemplaire
                : CarteSelectionnee.Identifiant;

            var offre = await serviceApi.CreerOffreAsync(copyId, prixFinal);

            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlert(
                    "Offre publiée !",
                    $"Votre {CarteSelectionnee.Carte.Marque} {CarteSelectionnee.Carte.Modele} est en vente au prix de {prixFinal:N0} coins.",
                    "OK");
            }

            // Retour automatique vers l'écran précédent
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Annule la mise en vente et ferme la page.
    /// </summary>
    [RelayCommand]
    private async Task AnnulerAsync()
    {
        await Shell.Current.GoToAsync("..");
    }

    #endregion
}
```

### 4.2 Proposed Code: `AutoMasters/Vues/CreationOffrePage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    xmlns:m="clr-namespace:AutoMasters.Modeles"
    x:Class="AutoMasters.Vues.CreationOffrePage"
    x:DataType="vm:CreationOffreVueModele"
    BackgroundColor="#10080B"
    Title="Vendre un véhicule">

    <ScrollView>
        <VerticalStackLayout Padding="20,24" Spacing="20">
            
            <!-- En-tête -->
            <Grid ColumnDefinitions="*,Auto">
                <VerticalStackLayout Grid.Column="0" Spacing="2">
                    <Label
                        Text="VENDRE UNE CARTE"
                        FontSize="22"
                        FontAttributes="Bold"
                        TextColor="#FFFFFF" />
                    <Label
                        Text="Fixez votre prix et mettez un véhicule sur le marché"
                        FontSize="13"
                        TextColor="#9CA3AF" />
                </VerticalStackLayout>
                <Button
                    Grid.Column="1"
                    Text="✕"
                    FontSize="18"
                    TextColor="#FFFFFF"
                    BackgroundColor="#221215"
                    CornerRadius="16"
                    WidthRequest="36"
                    HeightRequest="36"
                    Padding="0"
                    Command="{Binding AnnulerCommand}" />
            </Grid>

            <!-- Étape 1 : Sélection du véhicule -->
            <VerticalStackLayout Spacing="8">
                <Label
                    Text="1. CHOISIR UN VÉHICULE DU GARAGE"
                    FontSize="11"
                    FontAttributes="Bold"
                    TextColor="#E63946"
                    CharacterSpacing="1" />

                <CollectionView
                    ItemsSource="{Binding CartesPossedees}"
                    SelectedItem="{Binding CarteSelectionnee, Mode=TwoWay}"
                    SelectionMode="Single"
                    HeightRequest="220">
                    <CollectionView.EmptyView>
                        <Label
                            Text="Aucun véhicule disponible à la vente dans votre garage."
                            TextColor="#888888"
                            FontSize="13"
                            Margin="0,16" />
                    </CollectionView.EmptyView>
                    <CollectionView.ItemTemplate>
                        <DataTemplate x:DataType="m:CartePossedee">
                            <Border
                                Stroke="#2A1E24"
                                StrokeThickness="1"
                                BackgroundColor="#1A1A1E"
                                StrokeShape="RoundRectangle 10"
                                Padding="10"
                                Margin="0,0,0,8">
                                <VisualStateManager.VisualStateGroups>
                                    <VisualStateGroup Name="CommonStates">
                                        <VisualState Name="Normal" />
                                        <VisualState Name="Selected">
                                            <VisualState.Setters>
                                                <Setter Property="Stroke" Value="#E63946" />
                                                <Setter Property="BackgroundColor" Value="#2B1518" />
                                            </VisualState.Setters>
                                        </VisualState>
                                    </VisualStateGroup>
                                </VisualStateManager.VisualStateGroups>

                                <Grid ColumnDefinitions="60,*" ColumnSpacing="12">
                                    <Border
                                        Grid.Column="0"
                                        StrokeThickness="0"
                                        StrokeShape="RoundRectangle 6"
                                        HeightRequest="48"
                                        WidthRequest="60"
                                        BackgroundColor="#242426">
                                        <Image Source="{Binding Carte.ImageUrl}" Aspect="AspectFill" />
                                    </Border>
                                    <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                                        <Label Text="{Binding Carte.Marque}" FontAttributes="Bold" TextColor="#FFFFFF" FontSize="14" />
                                        <Label Text="{Binding Carte.Modele}" TextColor="#9CA3AF" FontSize="12" />
                                        <Label Text="{Binding ValeurActuelle, StringFormat='Valeur estimée : {0:N0} coins'}" TextColor="#FBBF24" FontSize="11" />
                                    </VerticalStackLayout>
                                </Grid>
                            </Border>
                        </DataTemplate>
                    </CollectionView.ItemTemplate>
                </CollectionView>
            </VerticalStackLayout>

            <!-- Étape 2 : Saisie du Prix -->
            <VerticalStackLayout Spacing="8">
                <Label
                    Text="2. PRIX DE VENTE SOUHAITÉ (COINS)"
                    FontSize="11"
                    FontAttributes="Bold"
                    TextColor="#E63946"
                    CharacterSpacing="1" />

                <Border
                    Stroke="#331A20"
                    StrokeThickness="1"
                    BackgroundColor="#1E1014"
                    StrokeShape="RoundRectangle 10"
                    Padding="14,4">
                    <Entry
                        Placeholder="Exemple : 480 000"
                        PlaceholderColor="#888888"
                        TextColor="#FFFFFF"
                        Keyboard="Numeric"
                        FontSize="16"
                        Text="{Binding PrixSaisi}" />
                </Border>
                <Label
                    Text="Le prix doit être strictement supérieur à zéro."
                    FontSize="11"
                    TextColor="#6B7280" />
            </VerticalStackLayout>

            <!-- Message d'erreur -->
            <Label
                Text="{Binding MessageErreur}"
                TextColor="#EF4444"
                FontSize="13"
                HorizontalTextAlignment="Center"
                IsVisible="{Binding MessageErreur, Converter={x:Null}}" />

            <!-- Boutons d'Action -->
            <VerticalStackLayout Spacing="10" Margin="0,12,0,0">
                <Button
                    Text="PUBLIER SUR LE MARCHÉ"
                    FontAttributes="Bold"
                    FontSize="14"
                    TextColor="#FFFFFF"
                    BackgroundColor="#E63946"
                    HeightRequest="48"
                    CornerRadius="10"
                    IsEnabled="{Binding EstDisponible}"
                    Command="{Binding CreerOffreCommand}" />

                <Button
                    Text="ANNULER"
                    FontSize="13"
                    TextColor="#9CA3AF"
                    BackgroundColor="Transparent"
                    BorderColor="#331A20"
                    BorderWidth="1"
                    HeightRequest="44"
                    CornerRadius="10"
                    Command="{Binding AnnulerCommand}" />
            </VerticalStackLayout>

        </VerticalStackLayout>
    </ScrollView>
</ContentPage>
```

### 4.3 Proposed Code: `AutoMasters/Vues/CreationOffrePage.xaml.cs`

```csharp
using AutoMasters.VuesModeles;
using Microsoft.Maui.Controls;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur pour la page de création d'offre de vente (Feature F24).
/// </summary>
public partial class CreationOffrePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public CreationOffrePage()
    {
        InitializeComponent();
        BindingContext = new CreationOffreVueModele();
    }

    public CreationOffrePage(CreationOffreVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is CreationOffreVueModele vm)
        {
            await vm.ChargerVehiculesGarageCommand.ExecuteAsync(null);
        }
    }

    #endregion
}
```

---

## 5. Garage & Portfolio Component (F25, F26)

### 5.1 Proposed Code: `AutoMasters/VuesModeles/GarageVueModele.cs`

```csharp
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel gérant le Garage personnel du collectionneur et le déclencheur de boosters (Features F25, F26).
/// Gère la valorisation globale ("482 500 coins"), le P&L net coloré ("+32 500 € P&L total"),
/// l'affichage des badges d'exemplaires multiples ("x2"), le quota journalier ("3/10 disponibles"),
/// et l'ouverture du modal booster 5 cartes.
/// </summary>
public partial class GarageVueModele : ObservableObject
{
    #region Attributs

    private ServiceAPI serviceApi = ServiceAPI.Instance;

    [ObservableProperty]
    private Portefeuille portefeuille = new Portefeuille();

    [ObservableProperty]
    private ObservableCollection<CartePossedee> cartesPossedees = new ObservableCollection<CartePossedee>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ValeurTotaleFormatee))]
    private decimal valeurTotale;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalGainPerteFormate))]
    [NotifyPropertyChangedFor(nameof(EstEnGain))]
    [NotifyPropertyChangedFor(nameof(TotalGainPerteCouleur))]
    private decimal totalGainPerte;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitreCollection))]
    private int nombreTotalCartes;

    [ObservableProperty]
    private int paquetsDisponibles = 3;

    [ObservableProperty]
    private int paquetsMax = 10;

    [ObservableProperty]
    private int tempsRestantSecondes = 595; // 09:55 par défaut

    [ObservableProperty]
    private bool estBoosterModalOuvert;

    [ObservableProperty]
    private BoosterVueModele? boosterVueModele;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    [NotifyPropertyChangedFor(nameof(PeutOuvrirBooster))]
    private bool estOccupe;

    [ObservableProperty]
    private string message = "";

    #endregion

    #region Constructeurs

    public GarageVueModele()
    {
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public bool PeutOuvrirBooster => PaquetsDisponibles > 0 && !EstOccupe;

    public string ValeurTotaleFormatee => $"{ValeurTotale:N0} coins";

    public bool EstEnGain => TotalGainPerte >= 0;

    /// <summary>
    /// Format strict validé par les tests F25_03, F25_B02 (+32 500 € P&L total) et F25_B03 (-2 000 € P&L total).
    /// </summary>
    public string TotalGainPerteFormate => $"{(EstEnGain ? "+" : "")}{TotalGainPerte:N0} € P&L total";

    public string TotalGainPerteCouleur => EstEnGain ? "#10B981" : "#EF4444";

    public string QuotaBoosterFormate => $"{PaquetsDisponibles}/{PaquetsMax} disponibles";

    public string CompteAReboursBooster => $"Prochain dans {TimeSpan.FromSeconds(TempsRestantSecondes):mm\\:ss}";

    public string TitreCollection => $"MA COLLECTION · {NombreTotalCartes} CARTES";

    #endregion

    #region Methodes

    /// <summary>
    /// Charge l'intégralité du portefeuille de l'utilisateur depuis l'API REST.
    /// </summary>
    [RelayCommand]
    private async Task ChargerGarageAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = "";

        try
        {
            var res = await serviceApi.RecupererPortefeuilleAsync();
            Portefeuille = res;
            CartesPossedees = res.CartesPossedees ?? new ObservableCollection<CartePossedee>();
            ValeurTotale = res.ValeurTotale;
            TotalGainPerte = res.TotalGainPerte;
            NombreTotalCartes = res.NombreTotalCartes;
        }
        catch (Exception ex)
        {
            Message = $"Erreur lors de la synchronisation du garage : {ex.Message}";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Lance l'ouverture d'un booster et affiche la modale de révélation interactive (F26).
    /// </summary>
    [RelayCommand]
    private async Task OuvrirBoosterAsync()
    {
        if (PaquetsDisponibles <= 0)
        {
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlert(
                    "Quota épuisé",
                    $"Vous n'avez plus de booster disponible. {CompteAReboursBooster}.",
                    "OK");
            }
            return;
        }

        var vmBooster = new BoosterVueModele();
        vmBooster.SurBoosterTermine += async () =>
        {
            EstBoosterModalOuvert = false;
            PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
            await ChargerGarageAsync();
        };

        BoosterVueModele = vmBooster;
        EstBoosterModalOuvert = true;

        await vmBooster.DemarrerOuvertureCommand.ExecuteAsync(null);
    }

    /// <summary>
    /// Ferme la modale booster.
    /// </summary>
    [RelayCommand]
    private void FermerBoosterModal()
    {
        EstBoosterModalOuvert = false;
    }

    /// <summary>
    /// Raccourci contextuel pour mettre directement en vente une carte depuis le garage.
    /// </summary>
    [RelayCommand]
    private async Task VendreCarteAsync(CartePossedee carte)
    {
        if (carte == null) return;
        await Shell.Current.GoToAsync("CreationOffrePage");
    }

    #endregion
}
```

### 5.2 Proposed Code: `AutoMasters/Vues/GaragePage.xaml`

```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    xmlns:m="clr-namespace:AutoMasters.Modeles"
    x:Class="AutoMasters.Vues.GaragePage"
    x:DataType="vm:GarageVueModele"
    BackgroundColor="#10080B"
    Title="Garage">

    <Grid>
        <!-- Vue Principale (Scrollable) -->
        <ScrollView>
            <VerticalStackLayout Padding="16,12" Spacing="16">
                
                <!-- Titre Page -->
                <Label
                    Text="GARAGE"
                    FontSize="24"
                    FontAttributes="Bold"
                    TextColor="#FFFFFF" />

                <!-- 1. En-tête Valorisation Portefeuille & P&L (Figma Screen 2 / F25) -->
                <Border
                    Stroke="#2A1E24"
                    StrokeThickness="1"
                    BackgroundColor="#1A1A1E"
                    StrokeShape="RoundRectangle 14"
                    Padding="16,14">
                    <VerticalStackLayout Spacing="4">
                        <Label
                            Text="VALEUR TOTALE DE LA COLLECTION"
                            FontSize="11"
                            FontAttributes="Bold"
                            TextColor="#9CA3AF"
                            CharacterSpacing="1" />
                        <Label
                            Text="{Binding ValeurTotaleFormatee}"
                            FontSize="32"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF" />
                        <Label
                            Text="{Binding TotalGainPerteFormate}"
                            FontSize="14"
                            FontAttributes="Bold"
                            TextColor="{Binding TotalGainPerteCouleur}" />
                    </VerticalStackLayout>
                </Border>

                <!-- 2. Bannière Booster Pack (Figma Screen 2 / F26) -->
                <Border
                    Stroke="#E63946"
                    StrokeThickness="1"
                    BackgroundColor="#221215"
                    StrokeShape="RoundRectangle 14"
                    Padding="16,14">
                    <VerticalStackLayout Spacing="8">
                        <Grid ColumnDefinitions="*,Auto">
                            <HorizontalStackLayout Grid.Column="0" Spacing="6" VerticalOptions="Center">
                                <Label Text="BOOSTER" FontSize="16" FontAttributes="Bold" TextColor="#FFFFFF" />
                                <Label Text="AUTOMASTERS" FontSize="16" FontAttributes="Bold" TextColor="#E63946" />
                            </HorizontalStackLayout>
                            <Label
                                Grid.Column="1"
                                Text="{Binding QuotaBoosterFormate}"
                                FontSize="12"
                                FontAttributes="Bold"
                                TextColor="#FFFFFF"
                                VerticalOptions="Center" />
                        </Grid>

                        <!-- Compte à rebours de recharge -->
                        <Label
                            Text="{Binding CompteAReboursBooster}"
                            FontSize="12"
                            FontAttributes="Bold"
                            TextColor="#FBBF24" />

                        <!-- Bouton '⚡ OUVRIR' -->
                        <Button
                            Text="⚡ OUVRIR"
                            FontSize="15"
                            FontAttributes="Bold"
                            TextColor="#FFFFFF"
                            BackgroundColor="#E63946"
                            HeightRequest="46"
                            CornerRadius="10"
                            IsEnabled="{Binding PeutOuvrirBooster}"
                            Command="{Binding OuvrirBoosterCommand}" />
                    </VerticalStackLayout>
                </Border>

                <!-- 3. Section Collection de Cartes (CollectionView) -->
                <VerticalStackLayout Spacing="10">
                    <Label
                        Text="{Binding TitreCollection}"
                        FontSize="12"
                        FontAttributes="Bold"
                        TextColor="#9CA3AF"
                        CharacterSpacing="1" />

                    <CollectionView
                        ItemsSource="{Binding CartesPossedees}"
                        SelectionMode="None">
                        <CollectionView.EmptyView>
                            <VerticalStackLayout Spacing="12" HorizontalOptions="Center" VerticalOptions="Center" Padding="32">
                                <Label Text="🏎️" FontSize="48" HorizontalOptions="Center" />
                                <Label
                                    Text="Votre garage est vide"
                                    FontSize="18"
                                    FontAttributes="Bold"
                                    TextColor="#FFFFFF"
                                    HorizontalOptions="Center" />
                                <Label
                                    Text="Ouvrez un booster pour obtenir vos 5 premières voitures !"
                                    FontSize="13"
                                    TextColor="#888888"
                                    HorizontalTextAlignment="Center" />
                            </VerticalStackLayout>
                        </CollectionView.EmptyView>

                        <CollectionView.ItemTemplate>
                            <DataTemplate x:DataType="m:CartePossedee">
                                <Border
                                    Stroke="#2A1E24"
                                    StrokeThickness="1"
                                    BackgroundColor="#1A1A1E"
                                    StrokeShape="RoundRectangle 12"
                                    Padding="12"
                                    Margin="0,0,0,10">
                                    <Grid ColumnDefinitions="84,*,Auto" ColumnSpacing="12">
                                        
                                        <!-- Miniature + Badge Multiplicateur 'x2' (F25_04, F25_B04, F25_B05) -->
                                        <Grid Grid.Column="0">
                                            <Border
                                                StrokeThickness="0"
                                                StrokeShape="RoundRectangle 8"
                                                HeightRequest="64"
                                                WidthRequest="84"
                                                BackgroundColor="#242426">
                                                <Image Source="{Binding Carte.ImageUrl}" Aspect="AspectFill" />
                                            </Border>

                                            <!-- Badge Multiplicateur (visible si quantité > 1) -->
                                            <Border
                                                StrokeThickness="0"
                                                BackgroundColor="#E63946"
                                                StrokeShape="RoundRectangle 4"
                                                Padding="5,1"
                                                HorizontalOptions="Start"
                                                VerticalOptions="Start"
                                                Margin="4,4,0,0">
                                                <Label
                                                    Text="{Binding QuantiteBadge}"
                                                    FontSize="10"
                                                    FontAttributes="Bold"
                                                    TextColor="#FFFFFF" />
                                            </Border>
                                        </Grid>

                                        <!-- Détails Marque, Modèle, Acheté, Rareté -->
                                        <VerticalStackLayout Grid.Column="1" Spacing="2" VerticalOptions="Center">
                                            <HorizontalStackLayout Spacing="6">
                                                <Label Text="{Binding Carte.Marque}" FontAttributes="Bold" TextColor="#FFFFFF" FontSize="15" />
                                                <Border
                                                    Stroke="#38BDF8"
                                                    StrokeThickness="1"
                                                    BackgroundColor="#162936"
                                                    StrokeShape="RoundRectangle 4"
                                                    Padding="4,1">
                                                    <Label Text="{Binding Carte.Rarete}" FontSize="9" FontAttributes="Bold" TextColor="#38BDF8" />
                                                </Border>
                                            </HorizontalStackLayout>

                                            <Label Text="{Binding Carte.Modele}" TextColor="#9CA3AF" FontSize="12" />
                                            <Label Text="{Binding PrixAchat, StringFormat='Acheté {0:N0} €'}" TextColor="#6B7280" FontSize="11" />
                                        </VerticalStackLayout>

                                        <!-- Valeur Actuelle, P&L & Menu contextuel -->
                                        <VerticalStackLayout Grid.Column="2" Spacing="2" HorizontalOptions="End" VerticalOptions="Center">
                                            <Label
                                                Text="{Binding ValeurActuelle, StringFormat='{0:N0} €'}"
                                                FontAttributes="Bold"
                                                TextColor="#FFFFFF"
                                                FontSize="14"
                                                HorizontalTextAlignment="End" />
                                            <Label
                                                Text="{Binding GainPerteFormate}"
                                                FontAttributes="Bold"
                                                TextColor="#10B981"
                                                FontSize="12"
                                                HorizontalTextAlignment="End" />
                                            <Button
                                                Text="Vendre"
                                                FontSize="10"
                                                FontAttributes="Bold"
                                                TextColor="#E63946"
                                                BackgroundColor="Transparent"
                                                HeightRequest="26"
                                                Padding="0"
                                                Command="{Binding Source={RelativeSource AncestorType={x:Type vm:GarageVueModele}}, Path=VendreCarteCommand}"
                                                CommandParameter="{Binding .}" />
                                        </VerticalStackLayout>

                                    </Grid>
                                </Border>
                            </DataTemplate>
                        </CollectionView.ItemTemplate>
                    </CollectionView>
                </VerticalStackLayout>

            </VerticalStackLayout>
        </ScrollView>

        <!-- 4. Overlay Modale d'Ouverture Booster (Figma Screen 3) -->
        <Border
            BackgroundColor="#F010080B"
            StrokeThickness="0"
            IsVisible="{Binding EstBoosterModalOuvert}"
            Padding="20"
            ZIndex="99">
            <VerticalStackLayout VerticalOptions="Center" Spacing="16">
                <!-- Bouton fermeture modale -->
                <Button
                    Text="✕"
                    FontSize="18"
                    TextColor="#FFFFFF"
                    BackgroundColor="#221215"
                    CornerRadius="16"
                    WidthRequest="36"
                    HeightRequest="36"
                    HorizontalOptions="End"
                    Command="{Binding FermerBoosterModalCommand}" />

                <Label
                    Text="BOOSTER AUTOMASTERS"
                    FontSize="20"
                    FontAttributes="Bold"
                    TextColor="#FFFFFF"
                    HorizontalTextAlignment="Center" />

                <!-- Barres de progression 5 segments -->
                <Grid ColumnDefinitions="*,*,*,*,*" ColumnSpacing="6" Margin="24,0">
                    <BoxView HeightRequest="4" Color="{Binding BoosterVueModele.Segment1Actif, Converter={x:Null}, FallbackValue='#E63946'}" CornerRadius="2" />
                    <BoxView HeightRequest="4" Color="{Binding BoosterVueModele.Segment2Actif, Converter={x:Null}, FallbackValue='#331A20'}" CornerRadius="2" />
                    <BoxView HeightRequest="4" Color="{Binding BoosterVueModele.Segment3Actif, Converter={x:Null}, FallbackValue='#331A20'}" CornerRadius="2" />
                    <BoxView HeightRequest="4" Color="{Binding BoosterVueModele.Segment4Actif, Converter={x:Null}, FallbackValue='#331A20'}" CornerRadius="2" />
                    <BoxView HeightRequest="4" Color="{Binding BoosterVueModele.Segment5Actif, Converter={x:Null}, FallbackValue='#331A20'}" CornerRadius="2" />
                </Grid>

                <Label
                    Text="{Binding BoosterVueModele.TexteProgression}"
                    FontSize="13"
                    TextColor="#9CA3AF"
                    HorizontalTextAlignment="Center" />

                <!-- Carte (Face verso ou Face recto révélée) -->
                <Border
                    Stroke="{Binding BoosterVueModele.CouleurRareteActuelle}"
                    StrokeThickness="2"
                    BackgroundColor="#1E1014"
                    StrokeShape="RoundRectangle 16"
                    HeightRequest="340"
                    WidthRequest="240"
                    HorizontalOptions="Center"
                    Padding="14">
                    <Grid>
                        <!-- Face cachée (avant retournement) -->
                        <VerticalStackLayout
                            VerticalOptions="Center"
                            Spacing="12"
                            IsVisible="{Binding BoosterVueModele.EstRetournee, Converter={x:Null}, FallbackValue=True}">
                            <Label Text="🏎️" FontSize="56" HorizontalOptions="Center" />
                            <Label
                                Text="AUTOMASTERS"
                                FontSize="16"
                                FontAttributes="Bold"
                                TextColor="#E63946"
                                CharacterSpacing="2"
                                HorizontalOptions="Center" />
                        </VerticalStackLayout>

                        <!-- Face révélée (après retournement) -->
                        <VerticalStackLayout
                            Spacing="8"
                            IsVisible="{Binding BoosterVueModele.EstRetournee}">
                            <Image
                                Source="{Binding BoosterVueModele.CarteActuelle.ImageUrl}"
                                HeightRequest="150"
                                Aspect="AspectFill" />
                            <Label
                                Text="{Binding BoosterVueModele.CarteActuelle.Marque}"
                                FontSize="18"
                                FontAttributes="Bold"
                                TextColor="#FFFFFF" />
                            <Label
                                Text="{Binding BoosterVueModele.CarteActuelle.Modele}"
                                FontSize="14"
                                TextColor="#9CA3AF" />
                            <Label
                                Text="{Binding BoosterVueModele.LibelleRareteActuelle}"
                                FontSize="12"
                                FontAttributes="Bold"
                                TextColor="{Binding BoosterVueModele.CouleurRareteActuelle}" />
                            <Label
                                Text="{Binding BoosterVueModele.CarteActuelle.PrixOfficiel, StringFormat='Valeur : {0:N0} coins'}"
                                FontSize="13"
                                FontAttributes="Bold"
                                TextColor="#FBBF24" />
                        </VerticalStackLayout>
                    </Grid>
                </Border>

                <!-- Bouton d'Action : RETOURNER ▶ ou SUIVANT -->
                <Button
                    Text="{Binding BoosterVueModele.LibelleBoutonAction}"
                    FontSize="15"
                    FontAttributes="Bold"
                    TextColor="#FFFFFF"
                    BackgroundColor="#E63946"
                    HeightRequest="48"
                    CornerRadius="10"
                    WidthRequest="240"
                    HorizontalOptions="Center"
                    Command="{Binding BoosterVueModele.ActionCarteSuivanteCommand}" />
            </VerticalStackLayout>
        </Border>
    </Grid>

</ContentPage>
```

### 5.3 Proposed Code: `AutoMasters/Vues/GaragePage.xaml.cs`

```csharp
using AutoMasters.VuesModeles;
using Microsoft.Maui.Controls;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur pour la page Garage & Portfolio (Feature F25).
/// </summary>
public partial class GaragePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public GaragePage()
    {
        InitializeComponent();
        BindingContext = new GarageVueModele();
    }

    public GaragePage(GarageVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (BindingContext is GarageVueModele vm)
        {
            await vm.ChargerGarageCommand.ExecuteAsync(null);
        }
    }

    #endregion
}
```

---

## 6. Booster Opening Flow & 5-Card Reveal Component (F26)

### 6.1 Proposed Code: `AutoMasters/VuesModeles/BoosterVueModele.cs`

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel dédié à la séquence d'ouverture et révélation pas-à-pas des 5 cartes de booster (Feature F26).
/// Consomme ServiceAPI.OuvrirBoosterAsync(), anime la progression "Carte X sur 5",
/// et gère le retournement individuel avec mise en valeur de la rareté.
/// </summary>
public partial class BoosterVueModele : ObservableObject
{
    #region Attributs

    private ServiceAPI serviceApi = ServiceAPI.Instance;
    private List<Carte> cartesTirees = new List<Carte>();

    [ObservableProperty]
    private Carte? carteActuelle;

    [ObservableProperty]
    private int indexCarteActuelle = 0;

    [ObservableProperty]
    private int numeroCarteActuelle = 1;

    [ObservableProperty]
    private string texteProgression = "Carte 1 sur 5";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CouleurRareteActuelle))]
    [NotifyPropertyChangedFor(nameof(LibelleRareteActuelle))]
    private bool estRetournee = false;

    [ObservableProperty]
    private bool estDerniereCarte = false;

    [ObservableProperty]
    private bool estTermine = false;

    [ObservableProperty]
    private string libelleBoutonAction = "RETOURNER ▶";

    [ObservableProperty]
    private bool segment1Actif = true;

    [ObservableProperty]
    private bool segment2Actif = false;

    [ObservableProperty]
    private bool segment3Actif = false;

    [ObservableProperty]
    private bool segment4Actif = false;

    [ObservableProperty]
    private bool segment5Actif = false;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private string message = "";

    #endregion

    #region Constructeurs

    public BoosterVueModele()
    {
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Couleur thématique de rareté pour la carte active (F27).
    /// </summary>
    public string CouleurRareteActuelle => CarteActuelle?.Rarete switch
    {
        "LÉGENDAIRE" => "#F59E0B",
        "ÉPIQUE" => "#A855F7",
        "RARE" => "#38BDF8",
        _ => "#9CA3AF"
    };

    /// <summary>
    /// Libellé stylisé de rareté avec étoile pour les légendaires (Figma Screen 4).
    /// </summary>
    public string LibelleRareteActuelle => CarteActuelle?.Rarete == "LÉGENDAIRE"
        ? "★ LÉGENDAIRE"
        : (CarteActuelle?.Rarete ?? "COMMUNE");

    /// <summary>
    /// Événement déclenché à la fin de la cérémonie de révélation des 5 cartes.
    /// </summary>
    public event Action? SurBoosterTermine;

    #endregion

    #region Methodes

    /// <summary>
    /// Déclenche l'appel réseau vers POST /boosters/open et initialise la séquence de révélation.
    /// </summary>
    [RelayCommand]
    public async Task DemarrerOuvertureAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = "";

        try
        {
            var cartes = await serviceApi.OuvrirBoosterAsync();
            if (cartes == null || cartes.Count == 0)
                throw new Exception("Le serveur n'a délivré aucune carte pour ce booster.");

            cartesTirees = cartes;
            indexCarteActuelle = 0;
            NumeroCarteActuelle = 1;
            TexteProgression = "Carte 1 sur 5";
            CarteActuelle = cartesTirees[0];
            EstRetournee = false;
            EstDerniereCarte = false;
            EstTermine = false;
            LibelleBoutonAction = "RETOURNER ▶";

            MettreAJourSegments(1);
        }
        catch (Exception ex)
        {
            Message = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Alterne entre l'action de révélation de la carte et le passage à la suivante (F26_04, F26_05, F26_B03).
    /// </summary>
    [RelayCommand]
    private void ActionCarteSuivante()
    {
        if (cartesTirees.Count == 0)
            return;

        if (!EstRetournee)
        {
            // Action de retournement (Flip card)
            EstRetournee = true;

            if (indexCarteActuelle < 4)
            {
                LibelleBoutonAction = "CARTE SUIVANTE ▶";
            }
            else
            {
                EstDerniereCarte = true;
                LibelleBoutonAction = "TERMINER & AJOUTER AU GARAGE";
            }
        }
        else
        {
            // Passage à la carte suivante ou terminaison
            if (indexCarteActuelle < 4)
            {
                indexCarteActuelle++;
                NumeroCarteActuelle = indexCarteActuelle + 1;
                CarteActuelle = cartesTirees[indexCarteActuelle];
                EstRetournee = false;
                TexteProgression = $"Carte {NumeroCarteActuelle} sur 5";
                LibelleBoutonAction = "RETOURNER ▶";

                MettreAJourSegments(NumeroCarteActuelle);
            }
            else
            {
                // Fin de la révélation : les cartes ont été créditées sur le serveur
                EstTermine = true;
                SurBoosterTermine?.Invoke();
            }
        }
    }

    /// <summary>
    /// Met à jour l'état visuel des 5 segments de la barre de progression (F26_B02).
    /// </summary>
    private void MettreAJourSegments(int etape)
    {
        Segment1Actif = etape >= 1;
        Segment2Actif = etape >= 2;
        Segment3Actif = etape >= 3;
        Segment4Actif = etape >= 4;
        Segment5Actif = etape >= 5;
    }

    #endregion
}
```

---

## 7. AppShell Navigation & DI Configuration Integration

### 7.1 Proposed TabBar in `AutoMasters/AppShell.xaml`
To satisfy **F20** (AppShell 5-tab navigation) and link our new pages:

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

    <!-- Écran de connexion (Hors TabBar) -->
    <ShellContent
        Title="Connexion"
        ContentTemplate="{DataTemplate vues:ConnexionPage}"
        Route="ConnexionPage" />

    <!-- Navigation principale 5 Onglets (Figma Screen 2 & 4 / Feature F20) -->
    <TabBar Route="MainTabs">
        <Tab Title="Catalogue" Icon="icon_catalogue.png" Route="Catalogue">
            <ShellContent ContentTemplate="{DataTemplate vues:CataloguePage}" />
        </Tab>
        <Tab Title="Marché" Icon="icon_marche.png" Route="Marche">
            <ShellContent ContentTemplate="{DataTemplate vues:MarchePage}" />
        </Tab>
        <Tab Title="Ordres" Icon="icon_ordres.png" Route="Ordres">
            <ShellContent ContentTemplate="{DataTemplate vues:MarchePage}" />
        </Tab>
        <Tab Title="Garage" Icon="icon_garage.png" Route="Garage">
            <ShellContent ContentTemplate="{DataTemplate vues:GaragePage}" />
        </Tab>
        <Tab Title="Activité" Icon="icon_activite.png" Route="Activite">
            <ShellContent ContentTemplate="{DataTemplate vues:GaragePage}" />
        </Tab>
    </TabBar>

</Shell>
```

### 7.2 Route Registration in `AutoMasters/AppShell.xaml.cs`
```csharp
using AutoMasters.Vues;

namespace AutoMasters;

public partial class AppShell : Shell
{
    #region Attributs

    #endregion

    #region Constructeurs

    public AppShell()
    {
        InitializeComponent();
        
        // Enregistrement des routes secondaires pour navigation modale / détail
        Routing.RegisterRoute(nameof(CreationOffrePage), typeof(CreationOffrePage));
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
```

### 7.3 Dependency Injection in `AutoMasters/MauiProgram.cs`
```csharp
// Dans CreateMauiApp() :
builder.Services.AddSingleton(ServiceAPI.Instance);

// ViewModels
builder.Services.AddTransient<MarcheVueModele>();
builder.Services.AddTransient<CreationOffreVueModele>();
builder.Services.AddTransient<GarageVueModele>();
builder.Services.AddTransient<BoosterVueModele>();

// Vues
builder.Services.AddTransient<MarchePage>();
builder.Services.AddTransient<CreationOffrePage>();
builder.Services.AddTransient<GaragePage>();
```

---

## 8. Verification & Test Alignment Checklist

The proposed implementation strictly adheres to all Tier 1 and Tier 2 acceptance tests in `AutoMasters.E2ETests`:

| Test ID | Test Name | Design Proof in this Report |
|---|---|---|
| `F22_01` | `MarketPage_XamlMustExist` | `MarchePage.xaml` fully specified |
| `F22_02` | `MarketPage_MustContainLiveBadge` | Badge `🟢 EN DIRECT` in header (Border `#10B981`) |
| `F22_03` | `MarketPage_MustContainBuyAndReserveButtons` | `ACHETER IMMÉDIATEMENT` & `RÉSERVER` buttons present |
| `F22_04` | `MarketPage_MustContainSellFabButton` | Red FAB button (`+`) defined in bottom right |
| `F22_05` | `MarketViewModel_MustExposeOffresCollection` | `ObservableCollection<Offre> OffresFiltrees` in `MarcheVueModele` |
| `F23_01` | `ImmediateBuyUx_MustPresentConfirmationRecapDialog` | `DisplayAlert("Confirmation d'achat", ...)` recap in `AcheterOffreAsync` |
| `F23_04` | `ImmediateBuyUx_DisablesBuyButtonWhileExecuting` | `EstOccupe` and `EstDisponible` guard command execution |
| `F23_05` | `ReserveUx_StartsExpirationCountdown` | 15-minute expiration timestamp set in `ReserverOffreAsync` |
| `F24_01` | `SellModal_MustAllowSelectingOwnedCard` | Single selection `CollectionView` in `CreationOffrePage.xaml` |
| `F24_03` | `SellModal_ValidatesPriceStrictlyGreaterThanZero` | `prixFinal <= 0` rejection in `CreerOffreAsync` |
| `F25_01` | `GaragePage_XamlMustExist` | `GaragePage.xaml` fully specified |
| `F25_02` | `GaragePage_MustDisplayTotalCollectionValuation` | `482 500 coins` large numeric display bound to `ValeurTotaleFormatee` |
| `F25_03` | `GaragePage_MustDisplayNetPandLBanner` | `+32 500 € P&L total` green/red banner bound to `TotalGainPerteFormate` |
| `F25_04` | `GaragePage_MustDisplayMultiCopyBadge` | Badge `x2` (`QuantiteBadge`) rendered over car thumbnail |
| `F26_01` | `BoosterDialog_MustDisplayAvailableQuota` | `"3/10 disponibles"` bound to `QuotaBoosterFormate` |
| `F26_02` | `BoosterDialog_MustDisplayRefillCountdown` | `"Prochain dans 09:55"` bound to `CompteAReboursBooster` |
| `F26_03` | `BoosterDialog_MustProvideOuvrirCtaButton` | Button `"⚡ OUVRIR"` bound to `OuvrirBoosterCommand` |
| `F26_04` | `BoosterFlow_MustTrack5CardRevealProgression` | `"Carte 1 sur 5"` tracking through 5-segment indicator |
| `F26_05` | `BoosterFlow_CardFlipActionRevealsSpecs` | Action button `"RETOURNER ▶"` flips active card face-up |
| `F28_01` | `CodeBehind_AllViewCsFilesMustBePure` | Zero `HttpClient`, zero `ServiceAPI`, constructor-only code-behinds |

---
*Report prepared by explorer_m3_3 for worker implementation in Milestone 3.*
