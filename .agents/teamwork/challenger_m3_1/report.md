# Rapport d'Évaluation Contradictoire — Milestone 3
## Pureté du Code-Behind & Flux de Navigation (Features F18–F28)

**Auteur :** `challenger_m3_1` (Rôles : critic, specialist)  
**Date :** 2026-10-09  
**Verdict :** **REQUEST_CHANGES**  
**Niveau de risque global :** **HIGH**  

---

## 1. Résumé Exécutif

L'audit contradictoire empirique du Milestone 3 a permis de vérifier de manière indépendante les deux piliers architecturaux exigés :
1. **Pureté absolue du Code-Behind (F28)** : **CONFORME & APPROUVÉ**.
   - Analyse statique et textuelle de l'ensemble des 9 fichiers `.xaml.cs` du projet (`AutoMasters/`).
   - 0 instance de `HttpClient`.
   - 0 appel à `ServiceAPI.Instance` ou constructeur de service.
   - 0 méthode asynchrone, 0 `Task.Run`, 0 gestionnaire d'événement UI (`Clicked`).
   - Seuls `InitializeComponent()` et l'affectation du `BindingContext` sont présents dans les constructeurs des vues.
   - Les tests E2E de pureté (`F28_01` à `F28_05` et `F28_B01` à `F28_B05`) passent à 100%.

2. **Flux de navigation Shell & Gestion de l'Authentification (F18, F19, F20)** : **DÉFAUTS DÉTECTÉS (REQUEST_CHANGES)**.
   - La hiérarchie Shell (`AppShell.xaml`) est bien structurée : racine non-authentifiée sur `ConnexionPage`, `TabBar` (`MainTabs`) à 5 onglets avec `Catalogue` en premier.
   - Lors d'une connexion réussie, la redirection s'opère bien vers `//MainTabs/Catalogue`, le mot de passe est effacé et l'email est conservé.
   - **Cependant, deux anomalies empiriques sévères ont été mises en évidence par tests contradictoires :**
     * **Anomalie 1 (Medium - Erreur 401 dévoyée en panne réseau)** : Lorsque l'API renvoie HTTP 401 Unauthorized, `ServiceAPI` lève l'exception `"Connexion refusée ou expirée. Reconnectez-vous."`. `ConnexionVueModele` teste `details.Contains("connexion")` dans sa branche de gestion des pannes réseau, affichant à tort : `"Impossible de joindre le serveur. Vérifiez votre connexion Internet."` au lieu d'informer l'utilisateur que ses identifiants sont invalides.
     * **Anomalie 2 (High - Perte de booster sur double-tap)** : Lors de la séquence d'ouverture de booster (`BoosterVueModele`), un tap répété ou double-clic sur le bouton final `"TERMINER & AJOUTER AU GARAGE"` déclenche l'événement `SurBoosterTermine` plusieurs fois en l'absence de garde `EstTermine`. Dans `GarageVueModele`, le quota de boosters (`PaquetsDisponibles`) est décrémenté 2 fois pour un seul paquet ouvert ! Ce bug fait échouer le test contradictoire `ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine` (`Expected: 1, Actual: 2`).

En conséquence, la conformité fonctionnelle et l'intégrité des données utilisateur exigent une remédiation immédiate avant validation de M3.

---

## 2. Défis Contradictoires (Challenges)

### [High] Challenge 1 : Non-idempotence du bouton de fin de booster provoquant la perte de quota utilisateur
- **Composant affecté :** `AutoMasters/VuesModeles/BoosterVueModele.cs` (méthode `ActionCarteSuivante()`, lignes 168–209) et `GarageVueModele.cs` (lignes 172–177).
- **Hypothèse mise en échec :** L'action de terminaison d'un booster était supposée être finale et n'invoquer la clôture qu'une seule fois.
- **Scénario d'attaque :** Sur un écran tactile de smartphone ou par double-clic rapide sur desktop, l'utilisateur tape deux fois de suite sur le bouton `"TERMINER & AJOUTER AU GARAGE"`.
- **Preuve empirique verbatim :**
  ```
  [FAIL] AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   2
     at AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine() in ViewModelEmpiricalChallengeTests.cs:line 589
  ```
- **Rayon d'impact (Blast Radius) :** Le gestionnaire d'événement de `GarageVueModele` exécute `PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);` deux fois de suite. L'utilisateur se voit retirer 2 paquets de son quota journalier pour l'ouverture d'un seul booster de 5 cartes.
- **Atténuation recommandée (Mitigation) :**
  Ajouter un garde au début de `ActionCarteSuivante()` dans `BoosterVueModele.cs` :
  ```csharp
  if (cartesTirees.Count == 0 || EstTermine)
      return;
  ```

---

### [Medium] Challenge 2 : Fausse alerte de panne réseau sur identifiants invalides (HTTP 401)
- **Composant affecté :** `AutoMasters/VuesModeles/ConnexionVueModele.cs` (lignes 163–176) et `AutoMasters/Services/ServiceAPI.cs` (ligne 114).
- **Hypothèse mise en échec :** En cas d'identifiants incorrects (HTTP 401 Unauthorized), l'écran de connexion devait afficher un message d'erreur clair indiquant que les identifiants sont invalides, tout en préservant l'adresse email.
- **Scénario d'attaque :** L'utilisateur saisit une adresse email correcte et un mot de passe erroné. L'API retourne HTTP 401.
- **Preuve empirique verbatim :**
  ```
  [FAIL] AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner
  Assert.Contains() Failure: Sub-string not found
  String:    "Impossible de joindre le serveur. Vérifiez votre connexion Internet."
  Not found: "Identifiants invalides"
     at AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner() in ChallengerM3VerificationTests.cs:line 178
  ```
- **Mécanisme du bug :**
  1. `ServiceAPI.LireReponseAsync` intercepte HTTP 401 et lève : `throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");`.
  2. `ConnexionVueModele.SeConnecterAsync` intercepte l'exception.
  3. L'évaluation de `details.Contains("401") || details.Contains("Unauthorized") || details.Contains("invalide")` échoue car le texte contient `"Connexion refusée..."`.
  4. La branche suivante `else if (details.Contains("connexion") || ...)` évalue `details.Contains("connexion")` à `true` (sensible à la casse insensible sur le mot initial "Connexion").
  5. Le message affiché devient alors `"Impossible de joindre le serveur. Vérifiez votre connexion Internet."`.
- **Rayon d'impact (Blast Radius) :** L'utilisateur croit à un problème d'infrastructure réseau ou de serveur et redémarre son application ou sa connexion au lieu de corriger sa saisie d'identifiants.
- **Atténuation recommandée (Mitigation) :**
  Dans `ConnexionVueModele.cs` (ligne 164), intégrer les termes spécifiques de rejet d'authentification :
  ```csharp
  if (details.Contains("401") ||
      details.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
      details.Contains("invalide", StringComparison.OrdinalIgnoreCase) ||
      details.Contains("refusée", StringComparison.OrdinalIgnoreCase) ||
      details.Contains("expirée", StringComparison.OrdinalIgnoreCase))
  {
      Message = "Identifiants invalides. Vérifiez votre adresse email et votre mot de passe.";
  }
  ```

---

## 3. Résultats Détaillés des Tests Contradictoires

| Test / Scénario | Comportement Attendu | Comportement Observé | Statut |
|---|---|---|:---:|
| **Pureté Code-Behind (9 fichiers `.xaml.cs`)** | 0 HttpClient, 0 ServiceAPI, 0 Task/async, constructeur seul | Strictement aucun code métier, uniquement `InitializeComponent()` et DI | **PASS** |
| **Purity Engine AST E2E (`F28_01` à `F28_05`)** | 100% de conformité de l'arborescence Vues | Validation complète de tous les fichiers de vues | **PASS** |
| **Navigation racine Shell (`AppShell.xaml`)** | Page de démarrage non-authentifiée = `ConnexionPage` | Balise `<ShellContent Route="ConnexionPage" ... />` en première position | **PASS** |
| **Structure 5 onglets (`AppShell.xaml`)** | TabBar `MainTabs` avec 5 onglets, `Catalogue` en tête | TabBar configurée avec Catalogue, Marché, Ordres, Garage, Activité | **PASS** |
| **Connexion valide -> Redirection** | Navigation vers `//MainTabs/Catalogue`, effacement MdP | Navigation exécutée, mot de passe vidé, email préservé | **PASS** |
| **Champs vides / format email invalide** | Blocage local sans requête HTTP, message d'erreur | Aucun appel réseau émis, messages explicites affichés | **PASS** |
| **Connexion invalide (HTTP 401)** | Message d'identifiants invalides, email préservé | Email préservé, mais message = "Impossible de joindre le serveur" | **FAIL** |
| **Finalisation Booster (Double-tap)** | Déclenchement unique de `SurBoosterTermine` (idempotence) | Événement déclenché 2 fois, décrémentation double du quota | **FAIL** |
| **Suite de tests E2E globale (`AutoMasters.E2ETests`)** | 300 tests passants | 300/300 tests réussis (340 ms) | **PASS** |

---

## 4. Zones Non Contestées (Unchallenged Areas)

- **Gestion des événements système matériels mobiles (Bouton retour physique Android)** : Non contesté car le projet cible l'environnement desktop dépackagé Windows (`net10.0-windows10.0.19041.0`) selon les critères BTS SIO du sujet.
- **Routage des onglets secondaires "Ordres" et "Activité"** : Ces onglets réutilisent provisoirement `MarchePage` et `GaragePage` conformément aux spécifications du cahier des charges MVP.

---

## 5. Recommandations Actionnables

1. **Appliquer le correctif d'idempotence dans `BoosterVueModele.cs` :**
   Insérer `if (cartesTirees.Count == 0 || EstTermine) return;` au début de `ActionCarteSuivante()`.
2. **Appliquer le correctif de classification 401 dans `ConnexionVueModele.cs` :**
   Ajouter `details.Contains("refusée", StringComparison.OrdinalIgnoreCase) || details.Contains("expirée", StringComparison.OrdinalIgnoreCase)` dans la première condition du bloc `catch (Exception ex)`.
3. **Re-lancer les tests unitaires :**
   `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj` devra retourner 100% de réussite (77/77 tests passants).
