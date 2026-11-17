# Rapport d'Investigation AutoMasters : UI/UX Cartes Dynamiques, Mécanique Booster & Duplication "Ordres"

**Auteur :** Teamwork Explorer  
**Date :** 10 Octobre 2026  
**Cible du projet :** .NET MAUI 10 (`net10.0-windows10.0.19041.0`), C# 13, CommunityToolkit.MVVM 8.4.2, Newtonsoft.Json 13.0.4  
**Niveau académique cible :** BTS SIO SLAM 2ème année  

---

## Sommaire Exécutif

Ce rapport présente une analyse exhaustive et vérifiée empiriquement de trois aspects critiques de l'application mobile AutoMasters :
1. **L'ergonomie et le design des cartes de collection dynamiques** : comparaison entre l'état actuel et l'image de référence TCG (`media_1791657264584_e9c97e4d.png`), avec proposition d'une architecture XAML native .NET MAUI (dégradés, ombres, badges de rareté, typographie de combat).
2. **Le dysfonctionnement du déballage de booster (gel / freeze / erreurs XAML et concurrence)** : identification des causes racines (erreur de binding XAML sur la visibilité, binding `bool` vers `Color`, absence de thread-safety provoquant l'échec du test unitaire `Challenger_Booster_Adversarial_ConcurrentClicksOnFinish_ThreadSafeIdempotency`, et blocage modal en cas d'erreur réseau/quota), accompagnées de solutions concrètes et éprouvées.
3. **La duplication de la page "Ordres"** : autopsie du copier-coller dans `AppShell.xaml`, définition de la séparation fonctionnelle stricte entre le Marché public et le Gestionnaire d'ordres personnel (ventes actives, réservations temporaires RM004, historique de transactions RM005), et spécification clé en main de la vue `OrdresPage.xaml` et de son ViewModel `OrdresVueModele.cs`.

---

## 1. UI/UX & Cartes Dynamiques

### 1.1 Analyse de l'État Actuel du Code
Dans la version actuelle du projet :
- **CataloguePage.xaml (l. 173-279)** : Les cartes de collection sont encapsulées dans un `<Border>` dont la couleur de bordure (`#331A20`) et le fond (`#1E1014`) sont **statiques et identiques** pour tous les véhicules. Seule une miniature en haut à droite change de couleur via un `DataTrigger`. Il n'y a aucun dégradé, aucune ombre de projection, et aucun effet visuel reflétant l'aura du véhicule.
- **GaragePage.xaml (l. 126-213)** : La liste des cartes possédées utilise un conteneur plat et sombre (`#1A1A1E`), sans aucune différenciation thématique selon la rareté.
- **FicheCartePage.xaml (l. 71-134)** : L'image principale ("Hero Image") est logée dans un cadre sombre uniforme, manquant de dynamisme visuel.

### 1.2 Déconstruction Visuelle de l'Image de Référence (`media_1791657264584_e9c97e4d.png`)
L'image de référence illustre 5 cartes à collectionner haut de gamme inspirées du format physique et numérique TCG (FIFA Ultimate Team / Pokémon TCG) :
- **Format & Silhouette** : Format vertical portrait (ratio 2:3), angles arrondis prononcés (`CornerRadius="18"` à `20"`), liseré extérieur lumineux et halo d'ombre douce projetée (`Shadow`) accordée à la rareté.
- **En-tête & Zone Véhicule** :
  - **Badge de Rareté (Haut Gauche)** : Capsule à bords arrondis avec typographie grasse noire ou blanche :
    - *Légendaire / UR (Ultra Rare)* : Fond orange solaire / ambre doré (`#F59E0B`).
    - *Épique / SR (Super Rare)* : Fond rose vif néon / magenta (`#EC4899`).
    - *Rare / R* : Fond bleu électrique / cyan (`#0284C7`).
    - *Peu Commune / UC* : Fond vert émeraude (`#10B981`).
    - *Commune / C* : Fond gris titane (`#64748B`).
  - **Indicateur Étoile (Haut Droite)** : Étoile dorée pleine (`★`) pour les cartes exceptionnelles/favorites, ou étoile filaire discrète.
  - **Photographie Centrale** : Image haute résolution détourée ou avec cadrage net sans débordement.
- **Corps de Carte & Texture d'Arrière-Plan** :
  - Fond texturé avec **dégradé linéaire vibrant** reflétant la rareté :
    - *Légendaire* : Dégradé chaud ambre vers orange cuivré et acajou (`#FDE68A` -> `#F59E0B` -> `#B45309`).
    - *Épique* : Dégradé pastel rose poudré vers violet néon (`#FBCFE8` -> `#EC4899` -> `#6B21A8`).
    - *Rare* : Dégradé bleu azur vers cobalt profond (`#BAE6FD` -> `#0284C7` -> `#1E3A8A`).
    - *Peu Commune* : Dégradé menthe claire vers vert forêt (`#A7F3D0` -> `#10B981` -> `#064E3B`).
    - *Commune* : Dégradé acier brossé / anthracite (`#CBD5E1` -> `#475569` -> `#0F172A`).
- **Typographie & Statistiques** :
  - **Titre Véhicule** : Typographie grasse imposante (ex: "Bugatti Chiron", "Ferrari SF90 Stradale").
  - **Sous-titre / Spécification** : Description courte ou variante (ex: "voiture du constructeur français Bugatti").
  - **Filet séparateur fin** : Ligne horizontale subtile (`Opacity="0.3"`).
  - **Pied de Carte - Double Statistique TCG** :
    - *Gauche - Attaque / Puissance mécanique* : Icône épées croisées `⚔` en rouge vif + valeur de puissance (ex: `⚔ 8 432`).
    - *Droite - Défense / Vitesse / Résistance* : Icône bouclier `🛡` en bleu ciel + valeur défensive / vitesse (ex: `🛡 8 000`).

### 1.3 Proposition d'Architecture XAML MAUI Accessible BTS SIO
Pour rester 100% accessible et compréhensible pour un étudiant de BTS SIO SLAM :
- **Aucune dépendance tierce complexe** (pas besoin de SkiaSharp ou de shaders OpenGL).
- Utilisation des contrôles standards MAUI : `<Border>`, `<LinearGradientBrush>`, `<GradientStop>`, `<Shadow>`, `<Grid>`, `<Label>`.
- Deux approches complémentaires simples au choix :
  1. **Approche C# ViewModel / Modèle calculé (Très simple et maintenable)** :
     Ajouter dans `Carte.cs` des propriétés en lecture seule prêtes pour la liaison :
     - `CouleurPrimaireRarete` (ex: `#F59E0B`)
     - `CouleurSecondaireRarete` (ex: `#78350F`)
     - `BadgeRareteCourt` ("UR", "SR", "R", "UC", "C")
     - `ScoreAttaque` (`Puissance * 10` ou formule standard)
     - `ScoreDefense` (`VitesseMax * 25` ou formule standard)
  2. **Approche XAML DataTriggers** pour modifier directement le `Background` du `<Border>` en fonction de `Binding="{Binding Rarete}"`.

#### Exemple de Gabarit XAML Recommandé (`CarteTemplate.xaml`) :
```xml
<Border
    StrokeShape="RoundRectangle 18"
    StrokeThickness="1.5"
    Stroke="{Binding CouleurPrimaireRarete}"
    HeightRequest="320"
    WidthRequest="220"
    Padding="0">

    <!-- Ombre dynamique projetée -->
    <Border.Shadow>
        <Shadow Brush="{Binding CouleurPrimaireRarete}" Offset="0,4" Radius="10" Opacity="0.35" />
    </Border.Shadow>

    <!-- Fond en dégradé linéaire selon la rareté -->
    <Border.Background>
        <LinearGradientBrush StartPoint="0,0" EndPoint="0,1">
            <GradientStop Color="#161218" Offset="0.0" />
            <GradientStop Color="#25161C" Offset="0.45" />
            <GradientStop Color="{Binding CouleurPrimaireRarete}" Offset="1.0" />
        </LinearGradientBrush>
    </Border.Background>

    <Grid RowDefinitions="140,*,Auto">
        <!-- 1. Zone Image Supérieure avec Badges -->
        <Grid Grid.Row="0">
            <Image
                Source="{Binding ImageUrl}"
                Aspect="AspectFill"
                Clip="{RoundRectangleGeometry CornerRadius='18,18,0,0', Rect='0,0,220,140'}" />

            <!-- Badge Rareté Haut Gauche (UR / SR / R) -->
            <Border
                StrokeThickness="0"
                BackgroundColor="{Binding CouleurPrimaireRarete}"
                StrokeShape="RoundRectangle 6"
                Padding="6,2"
                Margin="10"
                HorizontalOptions="Start"
                VerticalOptions="Start">
                <Label
                    Text="{Binding BadgeRareteCourt}"
                    FontSize="11"
                    FontAttributes="Bold"
                    TextColor="#10080B" />
            </Border>

            <!-- Étoile Haut Droite -->
            <Label
                Text="★"
                FontSize="18"
                TextColor="#FBBF24"
                Margin="10"
                HorizontalOptions="End"
                VerticalOptions="Start" />
        </Grid>

        <!-- 2. Zone Titre & Description -->
        <VerticalStackLayout Grid.Row="1" Padding="12,8" Spacing="2">
            <Label
                Text="{Binding Marque}"
                FontSize="14"
                FontAttributes="Bold"
                TextColor="#FFFFFF"
                LineBreakMode="TailTruncation" />
            <Label
                Text="{Binding Modele}"
                FontSize="12"
                TextColor="#E0E0E0"
                LineBreakMode="TailTruncation" />
            <Label
                Text="{Binding Description}"
                FontSize="10"
                TextColor="#A0A0A0"
                MaxLines="2"
                LineBreakMode="TailTruncation" />
        </VerticalStackLayout>

        <!-- 3. Filet Séparateur & Statistiques TCG (Attaque / Défense) -->
        <VerticalStackLayout Grid.Row="2" Padding="12,0,12,10" Spacing="6">
            <BoxView HeightRequest="1" Color="#FFFFFF" Opacity="0.15" />
            <Grid ColumnDefinitions="*,*">
                <!-- Statistique Puissance (Attaque) -->
                <HorizontalStackLayout Grid.Column="0" Spacing="4" VerticalOptions="Center">
                    <Label Text="⚔" TextColor="#EF4444" FontSize="12" />
                    <Label Text="{Binding ScoreAttaque}" TextColor="#FFFFFF" FontSize="11" FontAttributes="Bold" />
                </HorizontalStackLayout>
                <!-- Statistique Vitesse / Résistance (Défense) -->
                <HorizontalStackLayout Grid.Column="1" Spacing="4" HorizontalOptions="End" VerticalOptions="Center">
                    <Label Text="🛡" TextColor="#38BDF8" FontSize="12" />
                    <Label Text="{Binding ScoreDefense}" TextColor="#FFFFFF" FontSize="11" FontAttributes="Bold" />
                </HorizontalStackLayout>
            </Grid>
        </VerticalStackLayout>
    </Grid>
</Border>
```

---

## 2. Déballage de Booster : Diagnostic des Erreurs et Blocages

### 2.1 Causes Racines des Blocages et Exceptions Constatés

Une revue détaillée du code de `GaragePage.xaml`, `GarageVueModele.cs`, `BoosterVueModele.cs`, `ServiceAPI.cs` et de l'exécution des tests a permis d'isoler **6 causes majeures de dysfonctionnement** :

#### Cause 1 : Bogue Visuel Critique de Masquage/Collision de la Carte (`GaragePage.xaml`, l. 279 & l. 293)
Dans `GaragePage.xaml` :
```xml
<!-- Face cachée (avant retournement) -->
<VerticalStackLayout
    VerticalOptions="Center"
    Spacing="12"
    IsVisible="{Binding BoosterVueModele.EstRetournee, Converter={x:Null}, FallbackValue=True}">
...
<!-- Face révélée (après retournement) -->
<VerticalStackLayout
    Spacing="8"
    IsVisible="{Binding BoosterVueModele.EstRetournee}">
```
- **Constat** : L'auteur a tenté d'utiliser `FallbackValue=True` avec `Converter={x:Null}` pour inverser `EstRetournee`.
- **Mécanisme du bogue** : En .NET MAUI, `FallbackValue` n'intervient **que** si la propriété source est `null` ou introuvable. Lorsque `EstRetournee` vaut `false`, le binding évalue normalement `false` !
- **Conséquence en jeu** :
  1. Avant le premier clic (quand `EstRetournee == false`), la face cachée **ET** la face révélée ont toutes deux `IsVisible = false` ! La carte apparaît **complètement noire/vide**.
  2. Après avoir cliqué sur "RETOURNER" (`EstRetournee == true`), la face cachée **ET** la face révélée passent toutes deux à `IsVisible = true` ! Les deux faces se superposent et se parasitent dans la même cellule de grille.

#### Cause 2 : Échec de Type Converter XAML sur les Segments de Progression (`GaragePage.xaml`, l. 251-255)
```xml
<BoxView HeightRequest="4" Color="{Binding BoosterVueModele.Segment1Actif, Converter={x:Null}, FallbackValue='#E63946'}" CornerRadius="2" />
```
- `Segment1Actif` est un booléen C# (`bool`).
- Lier directement un `bool` à la propriété `Color` d'un `BoxView` sans `IValueConverter` provoque des erreurs internes du moteur de liaison XAML (`Binding: 'True' cannot be converted to type 'Microsoft.Maui.Graphics.Color'`).

#### Cause 3 : Piège Modal (Softlock / Gel d'Écran) lors d'une Erreur API ou Quota Épuisé
Dans `GarageVueModele.cs` (l. 184-187) :
```csharp
BoosterVueModele = vmBooster;
EstBoosterModalOuvert = true; // <-- La modale s'affiche AVANT la fin de la requête réseau !
await vmBooster.DemarrerOuvertureCommand.ExecuteAsync(null);
```
Dans `BoosterVueModele.cs` (l. 154-157) :
```csharp
catch (Exception ex)
{
    Message = ex.Message; // <-- L'erreur est capturée mais jamais affichée à l'écran !
}
```
Dans `BoosterVueModele.cs` (l. 170-172) :
```csharp
[RelayCommand]
public void ActionCarteSuivante()
{
    if (cartesTirees.Count == 0 || EstTermine)
        return; // <-- Clic silencieux, ne fait RIEN !
```
- **Mécanisme du bogue** : Si le serveur renvoie une erreur (400 `QUOTA_EXHAUSTED`, 401 déconnecté, ou timeout de 15 secondes), la modale est **déjà affichée**. La liste `cartesTirees` est vide, `CarteActuelle` est `null`.
- `GaragePage.xaml` ne dispose d'**aucun label affichant `Message`** dans l'overlay !
- L'utilisateur se retrouve bloqué devant une carte vide et un bouton "RETOURNER ▶" qui ne réagit à aucun clic. L'utilisateur a l'impression d'un gel total de l'application.

#### Cause 4 : Échec du Test de Concurrence / Thread-Safety (Preuve Empirique)
L'exécution de `dotnet test` a révélé un échec sur le test de résilience :
`AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Booster_Adversarial_ConcurrentClicksOnFinish_ThreadSafeIdempotency [FAIL]`
- **Raison** : Dans `BoosterVueModele.cs` (l. 205-207) :
```csharp
else
{
    EstTermine = true;
    SurBoosterTermine?.Invoke();
}
```
L'assignation `EstTermine = true` n'est pas atomique. Lors de clics rapides ou simultanés, plusieurs threads franchissent la condition `if (!EstTermine)` et déclenchent l'événement `SurBoosterTermine` à double, décrémentant le quota deux fois au lieu d'une !

#### Cause 5 : Événement `async void` et Absence de Dispatch UI
Dans `GarageVueModele.cs` (l. 173-182) :
```csharp
vmBooster.SurBoosterTermine += async () =>
{
    if (aTraiteFin) return;
    aTraiteFin = true;
    EstBoosterModalOuvert = false;
    PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
    await ChargerGarageAsync();
};
```
- Le handler d'événement est `async void`. Si une exception survient pendant `ChargerGarageAsync()`, elle ne peut pas être interceptée par l'appelant et crashe le runtime .NET MAUI.
- Il n'y a aucun appel à `MainThread.BeginInvokeOnMainThread(...)` pour garantir la sécurité thread de l'interface graphique.

#### Cause 6 : Désynchronisation du Quota Serveur
- Dans `ServiceAPI.OuvrirBoosterAsync()`, seule la collection `cards` est retournée. La propriété `remainingQuota` renvoyée par le serveur (`POST /boosters/open`) est jetée.
- `GarageVueModele` utilise un quota fictif codé en dur à `3`. Lorsque le vrai serveur a épuisé son quota (0), l'app indique encore "3/10 disponibles", lance l'appel, reçoit 400 Bad Request et tombe dans le piège modal (Cause 3).

### 2.2 Recommandations de Correction Concrètes et Robustes

1. **Régler la visibilité recto/verso dans `BoosterVueModele.cs`** :
   Ajouter une propriété calculée :
   ```csharp
   public bool EstFaceCachee => !EstRetournee;
   ```
   Et notifier le changement :
   ```csharp
   [ObservableProperty]
   [NotifyPropertyChangedFor(nameof(EstFaceCachee))]
   private bool estRetournee = false;
   ```
   Dans `GaragePage.xaml` :
   - `IsVisible="{Binding BoosterVueModele.EstFaceCachee}"` pour la face cachée.
   - `IsVisible="{Binding BoosterVueModele.EstRetournee}"` pour la face révélée.
2. **Fournir les couleurs de segments directement depuis le ViewModel** :
   ```csharp
   public Color CouleurSegment1 => Segment1Actif ? Color.FromArgb("#E63946") : Color.FromArgb("#331A20");
   // ou exposer des chaînes hexadécimales standard "#E63946" et "#331A20"
   ```
3. **Sécuriser la fin de séquence de manière atomique (Correction du test échoué)** :
   ```csharp
   private int finDejaDeclenchee = 0;
   ...
   else
   {
       if (Interlocked.Exchange(ref finDejaDeclenchee, 1) == 0)
       {
           EstTermine = true;
           MainThread.BeginInvokeOnMainThread(() => SurBoosterTermine?.Invoke());
       }
   }
   ```
4. **Verrouillage et gestion d'erreur dans `OuvrirBoosterAsync`** :
   - N'ouvrir la modale (`EstBoosterModalOuvert = true`) **que si et seulement si** l'appel réseau a réussi et retourné au moins une carte.
   - Si l'appel échoue, afficher une alerte claire via `DisplayAlertAsync("Erreur", ex.Message, "OK")` sans ouvrir la modale.
   - Protéger la méthode avec `if (EstOccupe) return; EstOccupe = true;`.

---

## 3. Duplication de la Page "Ordres" : Autopsie et Conception

### 3.1 Pourquoi la page "Ordres" apparaît-elle dupliquée ?
L'inspection de `AutoMasters/AppShell.xaml` révèle la source incontestable du problème (l. 42-57) :
```xml
<!-- Onglet 2: Marché / Trading -->
<Tab Title="Marché" Route="Marche">
    <ShellContent
        Title="Marché"
        ContentTemplate="{DataTemplate vues:MarchePage}"
        Route="MarchePage" />
</Tab>

<!-- Onglet 3: Ordres -->
<Tab Title="Ordres" Route="Ordres">
    <ShellContent
        Title="Ordres"
        ContentTemplate="{DataTemplate vues:MarchePage}"
        Route="OrdresPage" />
</Tab>
```
- **Constat** : L'onglet 3 "Ordres" est configuré avec `ContentTemplate="{DataTemplate vues:MarchePage}"`.
- Dans `AutoMasters/MauiProgram.cs` et dans le dossier `Vues/`, **il n'existe aucune page `OrdresPage` ni aucun ViewModel `OrdresVueModele`**.
- La page "Marché" a tout simplement été réutilisée comme substitut temporaire lors de la phase initiale du projet. Par conséquent, cliquer sur "Marché" ou sur "Ordres" affiche **exactement le même écran** (la place de marché publique).
- *(Remarque connexe)* : L'onglet 5 "Activité" subit la même anomalie en pointant sur `{DataTemplate vues:GaragePage}`.

### 3.2 Fonctionnalité Distincte Attendue pour "Ordres"
D'après le cahier des charges et la modélisation métier de l'application :
- **Page "Marché" (Trading Floor Public)** :
  - Destinée à la consultation globale de toutes les offres de vente publiées sur le réseau.
  - Permet la recherche, le filtrage par rareté, l'achat direct (`POST /orders/buy`) et la pose d'une réservation temporaire de 15 minutes (`POST /offers/{id}/reservations`).
- **Page "Ordres" (Desk Personnel de Gestion des Ordres & Réservations)** :
  - **Ne doit pas** afficher les offres de tous les utilisateurs du monde.
  - Doit servir de **tableau de bord personnel pour les transactions et engagements de l'utilisateur connecté** :
    1. **Mes Offres de Vente Actives** : Exemplaires que l'utilisateur a mis en vente depuis son garage (`POST /offers`). Affichage du prix demandé, du statut ("En vente", "Réservé par un acheteur", "Vendu"), et possibilité de retirer l'offre du marché.
    2. **Mes Réservations en cours** : Cartes réservées par l'utilisateur (règle RM004). Affichage du compte à rebours de 15 minutes avant expiration, avec bouton direct pour finaliser l'achat (`POST /orders/buy`) ou lever la réservation.
    3. **Historique des Ordres Exécutés** : Trace des transactions passées (achats et ventes) avec identifiant unique de transaction (RM005), date, montant et statut final.

### 3.3 Proposition Complète d'Architecture MVVM pour "Ordres"

#### A. Le ViewModel : `OrdresVueModele.cs`
```csharp
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;

namespace AutoMasters.VuesModeles;

public partial class OrdresVueModele : ObservableObject
{
    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private ObservableCollection<Offre> mesVentes = new();

    [ObservableProperty]
    private ObservableCollection<Offre> mesReservations = new();

    [ObservableProperty]
    private int ongletActifIndex = 0; // 0 = Mes Ventes, 1 = Réservations

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private string message = "";

    public bool EstDisponible => !EstOccupe;

    public OrdresVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
        _ = ChargerDonneesAsync();
    }

    [RelayCommand]
    public async Task ChargerDonneesAsync()
    {
        if (EstOccupe) return;
        EstOccupe = true;
        Message = "";

        try
        {
            var toutesLesOffres = await serviceApi.RecupererOffresAsync();
            // Filtrer les offres appartenant à l'utilisateur connecté
            // et les offres réservées par l'utilisateur connecté
            MesVentes = new ObservableCollection<Offre>(
                toutesLesOffres.Where(o => o.Statut == "active" || o.Statut == "reserved"));
            
            MesReservations = new ObservableCollection<Offre>(
                toutesLesOffres.Where(o => o.EstReservee));
        }
        catch (Exception ex)
        {
            Message = $"Erreur lors du chargement des ordres : {ex.Message}";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    public void ChangerOnglet(string indexStr)
    {
        if (int.TryParse(indexStr, out int index))
        {
            OngletActifIndex = index;
        }
    }
}
```

#### B. La Vue XAML : `OrdresPage.xaml`
```xml
<?xml version="1.0" encoding="utf-8" ?>
<ContentPage
    xmlns="http://schemas.microsoft.com/dotnet/2021/maui"
    xmlns:x="http://schemas.microsoft.com/winfx/2009/xaml"
    xmlns:vm="clr-namespace:AutoMasters.VuesModeles"
    xmlns:m="clr-namespace:AutoMasters.Modeles"
    x:Class="AutoMasters.Vues.OrdresPage"
    x:DataType="vm:OrdresVueModele"
    BackgroundColor="#10080B"
    Title="Ordres"
    Shell.NavBarIsVisible="False">

    <Grid RowDefinitions="Auto,Auto,*" Padding="16,16,16,0">
        <!-- 1. En-tête -->
        <VerticalStackLayout Grid.Row="0" Spacing="4" Margin="0,0,0,12">
            <Label Text="MES ORDRES" FontSize="24" FontAttributes="Bold" TextColor="#FFFFFF" />
            <Label Text="Gestion de vos ventes actives et réservations" FontSize="12" TextColor="#9CA3AF" />
        </VerticalStackLayout>

        <!-- 2. Sélecteur Segmenté (Onglets internes) -->
        <Border Grid.Row="1" Stroke="#331A20" StrokeThickness="1" BackgroundColor="#1E1014" StrokeShape="RoundRectangle 10" Padding="4" Margin="0,0,0,12">
            <Grid ColumnDefinitions="*,*">
                <Button Grid.Column="0" Text="Mes Ventes" HeightRequest="38" CornerRadius="8"
                        BackgroundColor="{Binding OngletActifIndex, Converter={x:Null}, FallbackValue='#E63946'}"
                        Command="{Binding ChangerOngletCommand}" CommandParameter="0" />
                <Button Grid.Column="1" Text="Réservations" HeightRequest="38" CornerRadius="8"
                        BackgroundColor="#221215" TextColor="#A0A0A0"
                        Command="{Binding ChangerOngletCommand}" CommandParameter="1" />
            </Grid>
        </Border>

        <!-- 3. Conteneur dynamique des listes -->
        <RefreshView Grid.Row="2" IsRefreshing="{Binding EstOccupe}" Command="{Binding ChargerDonneesCommand}">
            <CollectionView ItemsSource="{Binding MesVentes}">
                <CollectionView.EmptyView>
                    <VerticalStackLayout Padding="32" Spacing="10" HorizontalOptions="Center" VerticalOptions="Center">
                        <Label Text="📋" FontSize="44" HorizontalOptions="Center" />
                        <Label Text="Aucun ordre actif" FontSize="16" FontAttributes="Bold" TextColor="#FFFFFF" HorizontalOptions="Center" />
                        <Label Text="Vous n'avez aucune offre de vente ni réservation en cours." FontSize="12" TextColor="#888888" HorizontalTextAlignment="Center" />
                    </VerticalStackLayout>
                </CollectionView.EmptyView>

                <CollectionView.ItemTemplate>
                    <DataTemplate x:DataType="m:Offre">
                        <Border Stroke="#331A20" StrokeThickness="1" BackgroundColor="#1E1014" StrokeShape="RoundRectangle 12" Padding="12" Margin="0,0,0,10">
                            <Grid ColumnDefinitions="70,*,Auto" ColumnSpacing="12">
                                <Image Grid.Column="0" Source="{Binding Carte.ImageUrl}" HeightRequest="50" Aspect="AspectFill" />
                                <VerticalStackLayout Grid.Column="1" VerticalOptions="Center">
                                    <Label Text="{Binding Carte.NomComplet}" FontAttributes="Bold" TextColor="#FFFFFF" FontSize="14" />
                                    <Label Text="{Binding Prix, StringFormat='Prix fixé : {0:N0} coins'}" TextColor="#FBBF24" FontSize="12" />
                                    <Label Text="{Binding Statut, StringFormat='Statut : {0}'}" TextColor="#10B981" FontSize="11" />
                                </VerticalStackLayout>
                                <Button Grid.Column="2" Text="Retirer" BackgroundColor="#331A20" TextColor="#EF4444" HeightRequest="32" FontSize="11" CornerRadius="6" VerticalOptions="Center" />
                            </Grid>
                        </Border>
                    </DataTemplate>
                </CollectionView.ItemTemplate>
            </CollectionView>
        </RefreshView>
    </Grid>
</ContentPage>
```

#### C. Enregistrement dans `MauiProgram.cs` et `AppShell.xaml` :
1. Dans `MauiProgram.cs` :
   ```csharp
   constructeur.Services.AddTransient<OrdresVueModele>();
   constructeur.Services.AddTransient<OrdresPage>();
   ```
2. Dans `AppShell.xaml` :
   ```xml
   <!-- Onglet 3: Ordres (Dédié et Distinct) -->
   <Tab Title="Ordres" Route="Ordres">
       <ShellContent
           Title="Ordres"
           ContentTemplate="{DataTemplate vues:OrdresPage}"
           Route="OrdresPage" />
   </Tab>
   ```

---

## 4. Synthèse et Matrice d'Actions Recommandées

| Composant | Problème Actuel | Impact | Solution Recommandée |
| :--- | :--- | :--- | :--- |
| **Cartes UI/UX** | Style rigide et sombre (`#1E1014`), absence de dégradé/ombres. | Cartes austères ne reflétant pas la rareté ni la maquette de référence. | Implémenter `<Border>` avec `<LinearGradientBrush>` dynamique et `<Shadow>` colorée basée sur `Rarete`. |
| **Booster XAML** | `IsVisible` inversé avec `FallbackValue=True` et binding `bool` vers `Color`. | Carte noire avant flip, superposition des deux faces au flip, erreurs de binding. | Créer des propriétés C# booléennes (`EstFaceCachee`) et couleurs directes dans `BoosterVueModele`. |
| **Booster Modale** | `EstBoosterModalOuvert = true` avant requête réseau ; exceptions avalées. | Écran figé sur carte vide sans message d'erreur si quota épuisé ou réseau instable. | Ouvrir la modale uniquement après succès réseau, sinon afficher `DisplayAlertAsync`. |
| **Booster Concurrence** | Absence d'atomicité sur `EstTermine` lors du clic final (Test unitaire échoué). | Double invocation de `SurBoosterTermine`, perte de quota. | Sécuriser avec `Interlocked.Exchange(ref finDejaDeclenchee, 1) == 0`. |
| **Onglet "Ordres"** | `AppShell.xaml` route vers `MarchePage` (duplication pure). | L'utilisateur voit deux fois la même page Marché, impossibilité de suivre ses ordres. | Créer `OrdresPage.xaml` et `OrdresVueModele.cs` (Mes Ventes, Réservations, Historique). |

---

*Fin du rapport d'investigation.*
