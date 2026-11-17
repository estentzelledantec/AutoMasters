# Rapport d'Analyse et Stratégie de Correction — Anomalie de Réentrance / Non-Idempotence du Booster

**Auteur :** `explorer_m3_fix_1` (Rôles : explorer, investigator, analyst)  
**Date :** 2026-10-09  
**Cible :** `AutoMasters/VuesModeles/BoosterVueModele.cs` (Feature F26 & F25)  
**Statut :** Investigation achevée — Stratégie chirurgicale définie  

---

## 1. Résumé Exécutif

L'audit contradictoire conduit par les challengers `challenger_m3_1` et `challenger_m3_2` a révélé un bogue critique de réentrance et de non-idempotence dans la gestion de fin de cérémonie d'ouverture des boosters (`BoosterVueModele.cs`).
Lorsqu'un utilisateur effectue un double-clic rapide ou un double-tap sur l'écran tactile sur le bouton final **"TERMINER & AJOUTER AU GARAGE"**, la méthode `ActionCarteSuivante()` s'exécute une seconde fois en l'absence de garde sur `EstTermine`.
Cette seconde exécution réinvoque immédiatement l'événement `SurBoosterTermine`. Dans `GarageVueModele.cs`, le gestionnaire de cet événement décrémente `PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);`. Par conséquent, **l'utilisateur se voit retirer 2 paquets de son quota journalier pour l'ouverture d'un seul booster de 5 cartes**, tout en déclenchant des requêtes redondantes vers le serveur (`ChargerGarageAsync()`).

Ce bogue fait échouer le test contradictoire :
`AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine` (`Expected: 1, Actual: 2`).

La remédiation requise est **chirurgicale (1 ligne modifiée)**, garantit une idempotence stricte sans aucune régression fonctionnelle, et préserve scrupuleusement la structure des 4 régions C# (`#region Attributs`, `#region Constructeurs`, `#region Getter/Setter`, `#region Methodes`) imposée par les conventions BTS SIO du projet.

---

## 2. Analyse Approfondie du Mécanisme de Défaillance

### 2.1. Trajectoire d'Exécution Nominale de la Séquence de Révélation (10 Clics)

Un paquet booster contient exactement 5 cartes. Chaque carte requiert un état initial face cachée puis un retournement interactif (F26), soit une séquence complète de 10 clics :

| Clic # | `IndexCarteActuelle` | `EstRetournee` (entrée) | Branche exécutée | `EstRetournee` (sortie) | `LibelleBoutonAction` (sortie) | `EstDerniereCarte` | `EstTermine` |
|---|:---:|:---:|---|:---:|---|:---:|:---:|
| 1 | 0 | `false` | `if (!EstRetournee)` | `true` | `"CARTE SUIVANTE ▶"` | `false` | `false` |
| 2 | 0 | `true` | `else` -> `IndexCarteActuelle++` (1) | `false` | `"RETOURNER ▶"` | `false` | `false` |
| 3 | 1 | `false` | `if (!EstRetournee)` | `true` | `"CARTE SUIVANTE ▶"` | `false` | `false` |
| 4 | 1 | `true` | `else` -> `IndexCarteActuelle++` (2) | `false` | `"RETOURNER ▶"` | `false` | `false` |
| 5 | 2 | `false` | `if (!EstRetournee)` | `true` | `"CARTE SUIVANTE ▶"` | `false` | `false` |
| 6 | 2 | `true` | `else` -> `IndexCarteActuelle++` (3) | `false` | `"RETOURNER ▶"` | `false` | `false` |
| 7 | 3 | `false` | `if (!EstRetournee)` | `true` | `"CARTE SUIVANTE ▶"` | `false` | `false` |
| 8 | 3 | `true` | `else` -> `IndexCarteActuelle++` (4) | `false` | `"RETOURNER ▶"` | `false` | `false` |
| 9 | 4 | `false` | `if (!EstRetournee)` -> dernier flip | `true` | `"TERMINER & AJOUTER AU GARAGE"` | `true` | `false` |
| 10 | 4 | `true` | `else` -> Fin de la révélation | `true` | `"TERMINER & AJOUTER AU GARAGE"` | `true` | `true` |

Au 10ème clic :
- L'état est `IndexCarteActuelle = 4` et `EstRetournee = true`.
- La condition `IndexCarteActuelle < 4 && IndexCarteActuelle < cartesTirees.Count - 1` (4 < 4) est fausse.
- La branche terminale `else` (lignes 202–207) est atteinte :
  ```csharp
  // Fin de la révélation
  EstTermine = true;
  SurBoosterTermine?.Invoke();
  ```
- `EstTermine` passe à `true`.
- `SurBoosterTermine?.Invoke()` est exécuté (1ère invocation).

### 2.2. Comportement Pathologique lors d'un Double-Clic / Double-Tap (11ème Clic)

Examinons le code actuel de `ActionCarteSuivante()` dans `AutoMasters/VuesModeles/BoosterVueModele.cs` (lignes 168–209) :

```csharp
[RelayCommand]
public void ActionCarteSuivante()
{
    if (cartesTirees.Count == 0)
        return;

    if (!EstRetournee)
    {
        // Action de retournement (Flip card)
        ...
    }
    else
    {
        // Passage à la carte suivante ou terminaison
        if (IndexCarteActuelle < 4 && IndexCarteActuelle < cartesTirees.Count - 1)
        {
            ...
        }
        else
        {
            // Fin de la révélation
            EstTermine = true;
            SurBoosterTermine?.Invoke();
        }
    }
}
```

Lorsqu'un 11ème clic intervient (double-tap tactile, double-clic de souris, ou tap avant que l'animation de fermeture de la modale ne soit achevée) :
1. `cartesTirees.Count == 0` vaut `false` (il y a toujours 5 cartes en mémoire).
2. **AUCUN CONTRÔLE** n'est effectué sur `EstTermine`.
3. `!EstRetournee` vaut `false` car la 5ème carte est déjà retournée.
4. Le contrôle `IndexCarteActuelle < 4` vaut `false` (index = 4).
5. Le flux tombe **inévitablement** dans la branche `else` finale (ligne 202).
6. `SurBoosterTermine?.Invoke();` est exécuté **une deuxième fois** !

### 2.3. Impact Sévère dans `GarageVueModele.cs` (Rayon d'Impact)

Dans `AutoMasters/VuesModeles/GarageVueModele.cs` (lignes 171–177) :

```csharp
var vmBooster = new BoosterVueModele(serviceApi);
vmBooster.SurBoosterTermine += async () =>
{
    EstBoosterModalOuvert = false;
    PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
    await ChargerGarageAsync();
};
```

À chaque appel de `SurBoosterTermine?.Invoke()` :
1. `PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);` est réexécuté.
   - Si le joueur avait 3 paquets :
     - 1er clic (10ème étape) : `PaquetsDisponibles` passe de 3 à 2.
     - 2ème clic (11ème étape) : `PaquetsDisponibles` passe de 2 à 1 !
   - Le joueur perd un paquet supplémentaire sans contrepartie de cartes.
2. `ChargerGarageAsync()` est invoqué et exécuté en tâche de fond une seconde fois en parallèle, générant du trafic HTTP redondant (`GET /portfolio`).

### 2.4. Preuve par le Test Unitaire Contradictoire

Dans `AutoMasters.Tests/ViewModelEmpiricalChallengeTests.cs` (lignes 553–590) :

```csharp
[Fact]
public async Task Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine()
{
    ...
    int eventFiredCount = 0;
    boosterVm.SurBoosterTermine += () => eventFiredCount++;

    for (int i = 0; i < 9; i++)
    {
        boosterVm.ActionCarteSuivante();
    }

    Assert.True(boosterVm.EstDerniereCarte);
    Assert.Equal("TERMINER & AJOUTER AU GARAGE", boosterVm.LibelleBoutonAction);

    // 10ème clic : terminaison normale
    boosterVm.ActionCarteSuivante();
    Assert.True(boosterVm.EstTermine);
    Assert.Equal(1, eventFiredCount);

    // 11ème clic : double-clic intempestif ou tap répété sur l'écran tactile
    boosterVm.ActionCarteSuivante();

    // Invariant contradictoire : la finalisation d'un booster DOIT être idempotente
    Assert.Equal(1, eventFiredCount);
}
```

Résultat d'exécution en environnement de test :
```
[FAIL] AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine
Assert.Equal() Failure: Values differ
Expected: 1
Actual:   2
   at AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine() in ViewModelEmpiricalChallengeTests.cs:line 589
```

---

## 3. Stratégie de Correction Chirurgicale Recommandée

### 3.1. Modification dans `AutoMasters/VuesModeles/BoosterVueModele.cs`

Le garde de complétion doit être placé immédiatement en en-tête de `ActionCarteSuivante()`.

#### Code Avant Modification (lignes 168–172) :
```csharp
    [RelayCommand]
    public void ActionCarteSuivante()
    {
        if (cartesTirees.Count == 0)
            return;
```

#### Code Après Modification (lignes 168–172) :
```csharp
    [RelayCommand]
    public void ActionCarteSuivante()
    {
        if (cartesTirees.Count == 0 || EstTermine)
            return;
```

### 3.2. Analyse Détaillée de la Correction

1. **Garantie d'Idempotence :**
   - Dès que `EstTermine` est positionné à `true` (ligne 205), tout appel ultérieur à `ActionCarteSuivante()` est intercepté dès la première ligne par `if (cartesTirees.Count == 0 || EstTermine) return;`.
   - La méthode sort immédiatement sans altérer aucune propriété ni réinvoquer `SurBoosterTermine`.
2. **Atomicité et Ordre d'Affectation :**
   - Dans la branche terminale :
     ```csharp
     EstTermine = true;
     SurBoosterTermine?.Invoke();
     ```
   - `EstTermine` est positionné à `true` **avant** l'invocation de `SurBoosterTermine`. Même si un observateur ou un thread concurrent rappelait `ActionCarteSuivante()`, la condition de sortie serait déjà active.
3. **Réinitialisation pour les Boosters Suivants :**
   - Dans `DemarrerOuvertureAsync()` (ligne 149), `EstTermine = false;` réinitialise explicitement le cycle pour tout nouveau booster ouvert sur la même instance.
   - De surcroît, dans `GarageVueModele.OuvrirBoosterAsync()`, une nouvelle instance `new BoosterVueModele(serviceApi)` est instanciée à chaque ouverture, garantissant un état initial propre (`estTermine = false`).

### 3.3. Conformité aux Régions Françaises BTS SIO

Le fichier `BoosterVueModele.cs` est structuré selon les 4 régions canoniques requises par le référentiel BTS SIO et `PROJECT.md` :
1. Ligne 18 : `#region Attributs` -> Contient `private bool estTermine = false;` (ligne 44)
2. Ligne 73 : `#region Constructeurs`
3. Ligne 93 : `#region Getter/Setter` -> Contient `public event Action? SurBoosterTermine;` (ligne 118)
4. Ligne 122 : `#region Methodes` -> Contient `DemarrerOuvertureAsync()`, `ActionCarteSuivante()`, `MettreAJourSegments(int)`

La modification proposée s'effectue **strictement à l'intérieur de `#region Methodes`** sur la méthode existante `ActionCarteSuivante()`. Aucune région n'est altérée, renommée, ni déplacée.

---

## 4. Matrice de Non-Régression

| Propriété / Méthode | Comportement avant le 10ème clic | Comportement au 10ème clic | Comportement aux clics 11+ (après correctif) | Régression possible ? |
|---|---|---|---|:---:|
| `CarteActuelle` | Affiche carte 1 à 5 séquentiellement | Reste sur Carte 5 | Inchangé (Carte 5 conservée) | **NON** |
| `IndexCarteActuelle` | Incrémenté de 0 à 4 | Vaut 4 | Inchangé (reste à 4) | **NON** |
| `NumeroCarteActuelle` | Incrémenté de 1 à 5 | Vaut 5 | Inchangé (reste à 5) | **NON** |
| `TexteProgression` | `"Carte 1 sur 5"` ... `"Carte 5 sur 5"` | `"Carte 5 sur 5"` | Inchangé (`"Carte 5 sur 5"`) | **NON** |
| `EstRetournee` | Alterne `false` -> `true` -> `false` ... | Reste `true` | Inchangé (reste `true`) | **NON** |
| `EstDerniereCarte` | `false` pour cartes 1 à 4, `true` sur carte 5 | Reste `true` | Inchangé (reste `true`) | **NON** |
| `EstTermine` | `false` | Devient `true` | Reste `true` | **NON** |
| `LibelleBoutonAction` | Alterne `"RETOURNER ▶"` / `"CARTE SUIVANTE ▶"` | `"TERMINER & AJOUTER AU GARAGE"` | Inchangé (`"TERMINER & AJOUTER AU GARAGE"`) | **NON** |
| `CouleurRareteActuelle` | Dérivée de `CarteActuelle.Rarete` | Couleur de la 5ème carte | Inchangée | **NON** |
| `LibelleRareteActuelle` | Dérivé de `CarteActuelle.Rarete` | Rareté de la 5ème carte | Inchangée | **NON** |
| `Segments 1 à 5` | S'activent de 1 à 5 | Tous actifs (`true`) | Tous actifs (`true`) | **NON** |
| `SurBoosterTermine` | Non déclenché | Invoqué 1 fois | **NON INVOQUÉ** (bloqué par `EstTermine`) | **CORRECTION CIBLÉE** |
| `PaquetsDisponibles` (Garage) | Inchangé | Décrémenté de 1 | **INCHANGÉ** (décrément unique protégé) | **CORRECTION CIBLÉE** |

---

## 5. Recommandation Additionnelle (Défense en Profondeur dans `GarageVueModele.cs`)

Bien que la responsabilité de l'idempotence incombe au composant émetteur (`BoosterVueModele`), il est également possible et recommandé, à titre de bonne pratique de défense en profondeur, de sécuriser l'abonné dans `GarageVueModele.cs` (lignes 172–177) en désabonnant le délégué ou en utilisant un verrou local :

```csharp
Action? onBoosterTermine = null;
onBoosterTermine = async () =>
{
    vmBooster.SurBoosterTermine -= onBoosterTermine;
    EstBoosterModalOuvert = false;
    PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
    await ChargerGarageAsync();
};
vmBooster.SurBoosterTermine += onBoosterTermine;
```

Cependant, le test unitaire contradictoire `Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine` instanciant directement `BoosterVueModele` de façon autonome, la correction dans `BoosterVueModele.cs` est la condition *sine qua non* pour satisfaire les critères de qualité et faire passer le test.

---

## 6. Protocole de Vérification Indépendante

Après application de la modification chirurgicale par le worker :

1. **Exécution du test unitaire ciblé :**
   ```powershell
   dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine"
   ```
   * Résultat attendu : `Réussi! - échec : 0, réussite : 1`.

2. **Exécution de l'ensemble des tests de la suite Booster et Garage :**
   ```powershell
   dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~Booster|FullyQualifiedName~Garage"
   ```
   * Résultat attendu : 100% de réussite sur tous les tests liés aux flux Booster et Garage.

3. **Exécution de la suite E2E :**
   ```powershell
   dotnet test AutoMasters.E2ETests/AutoMasters.E2ETests.csproj
   ```
   * Résultat attendu : 300/300 tests réussis (0 échec).

---

## 7. Conclusion

La cause racine du bogue de double décompte de boosters réside exclusivement dans l'absence de garde `EstTermine` dans la commande `ActionCarteSuivante()`. La solution est une modification d'une seule ligne :
`if (cartesTirees.Count == 0 || EstTermine) return;`.
Cette correction garantit l'intégrité absolue des données du portefeuille utilisateur, l'idempotence des événements ViewModel, et le respect total des exigences architecturales du projet.
