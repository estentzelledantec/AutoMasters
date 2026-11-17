# Rapport de Défi Contradictoire et Stress-Testing Empirique — Milestone 3

**Agent :** challenger_m3_2 (EMPIRICAL CHALLENGER / critic, specialist)  
**Date :** 2026-10-09  
**Cible :** Milestone 3 — Couche de Présentation MVVM, Logique Marché / Portefeuille & Flux Booster  
**Verdict Explicite :** **REQUEST_CHANGES** ❌  
**Évaluation Globale des Risques :** **HIGH** ⚠️

---

## 1. Résumé Exécutif & Verdict

Le Milestone 3 a fait l'objet d'un banc d'essai contradictoire approfondi (*Adversarial Stress Harness*) instanciant et testant directement les ViewModels réels (`MarcheVueModele`, `CreationOffreVueModele`, `GarageVueModele`, `BoosterVueModele`, `ConnexionVueModele`).

Bien que les comportements nominaux de base soient bien implémentés (achat immédiat avec en-tête et corps Idempotency-Key UUIDv4, validation stricte du prix > 0, calcul de valorisation, formatage coloré du P&L, séquence séquentielle des 5 cartes de booster), **deux failles empiriques critiques ont été découvertes et reproduites par tests automatisés** :

1. **[CRITIQUE] Non-idempotence de fin de booster (`BoosterVueModele.cs`) :**  
   L'action finale de complétion d'un booster ("TERMINER & AJOUTER AU GARAGE") n'est pas protégée contre la réentrance ou les clics multiples (`EstTermine`). Un double-clic rapide ou un double-tap sur l'écran tactile redéclenche l'événement `SurBoosterTermine`, ce qui décrémente **une seconde fois le quota journalier** du joueur dans `GarageVueModele` (`PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1)`).
2. **[HAUTE] Masquage trompeur de l'erreur 401 Unauthorized (`ConnexionVueModele.cs`) :**  
   En cas de mauvais mot de passe ou d'identifiant invalide, `ServiceAPI` lève une exception contenant `"Connexion refusée ou expirée"`. Dans le bloc `catch` de `ConnexionVueModele.cs`, la condition teste la présence du mot `"connexion"` avant d'identifier le refus, ce qui affiche à tort à l'utilisateur : `"Impossible de joindre le serveur. Vérifiez votre connexion Internet."` au lieu de `"Identifiants invalides"`.

En vertu du principe d'intégrité empirique, ces deux défauts justifient un verdict d'arrêt immédiat : **REQUEST_CHANGES**.

---

## 2. Défis et Vulnérabilités Identifiés

### Défi 1 [CRITIQUE] : Réentrance et Déduction Multiple de Boosters sur Double-Clic

- **Composant cible :** `AutoMasters/VuesModeles/BoosterVueModele.cs`, lignes 168-208 et `GarageVueModele.cs`, lignes 172-177.
- **Hypothèse mise en échec :** L'utilisateur clique une seule fois sur le bouton de fin, ou l'interface bloque tout déclenchement ultérieur une fois la révélation achevée.
- **Scénario d'attaque :**
  L'utilisateur termine la révélation de la 5ème carte. Le libellé du bouton devient `"TERMINER & AJOUTER AU GARAGE"`. L'utilisateur effectue un double-clic rapide (ou appui tactile répété).
  Dans `BoosterVueModele.ActionCarteSuivante()` :
  ```csharp
  public void ActionCarteSuivante()
  {
      if (cartesTirees.Count == 0) // <-- ABSENCE DE CONTRÔLE SUR EstTermine !
          return;
      ...
      else
      {
          if (IndexCarteActuelle < 4 && IndexCarteActuelle < cartesTirees.Count - 1) { ... }
          else
          {
              EstTermine = true;
              SurBoosterTermine?.Invoke(); // <-- Invoqué à CHAQUE clic supplémentaire !
          }
      }
  }
  ```
  Dans `GarageVueModele.cs` :
  ```csharp
  vmBooster.SurBoosterTermine += async () =>
  {
      EstBoosterModalOuvert = false;
      PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
      await ChargerGarageAsync();
  };
  ```
- **Rayon d'impact (Blast Radius) :**  
  Perte injustifiée de paquets boosters pour l'utilisateur (le quota passe par exemple de 3 à 1 en une seule session au lieu de 2), saturation du serveur par des requêtes répétées `GET /portfolio`.
- **Preuve Empirique Verbatim :**  
  Test `ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine` :
  ```
  Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   2
     at AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine()
  ```
- **Atténuation préconisée :**
  Dans `BoosterVueModele.cs`, ligne 170 :
  ```csharp
  if (cartesTirees.Count == 0 || EstTermine)
      return;
  ```
  Et dans `GarageVueModele.cs` : détacher l'événement ou le verrouiller pour n'autoriser qu'une seule exécution.

---

### Défi 2 [HAUTE] : Masquage de l'Erreur d'Authentification 401 en Panne Réseau Fictive

- **Composant cible :** `AutoMasters/VuesModeles/ConnexionVueModele.cs`, lignes 163-171 et `AutoMasters/Services/ServiceAPI.cs`, lignes 111-115.
- **Hypothèse mise en échec :** Une erreur d'identifiant erroné (HTTP 401) est communiquée de manière transparente à l'utilisateur ("Identifiants invalides").
- **Scénario d'attaque :**
  Lorsqu'une requête `POST /auth/login` reçoit un statut 401 Unauthorized, `ServiceAPI.LireReponseAsync` exécute :
  ```csharp
  if (reponse.StatusCode == HttpStatusCode.Unauthorized)
  {
      ReinitialiserSession();
      throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
  }
  ```
  Cette exception est capturée par `ConnexionVueModele.cs` :
  ```csharp
  catch (Exception ex)
  {
      MotDePasse = string.Empty;
      string details = ex.Message ?? "";
      if (details.Contains("401") || details.Contains("Unauthorized", ...) || details.Contains("invalide", ...))
      {
          Message = "Identifiants invalides. Vérifiez votre adresse email et votre mot de passe.";
      }
      else if (details.Contains("connexion", StringComparison.OrdinalIgnoreCase) ...)
      {
          Message = "Impossible de joindre le serveur. Vérifiez votre connexion Internet.";
      }
  ```
  Comme `ex.Message` vaut `"Connexion refusée ou expirée. Reconnectez-vous."`, il contient le sous-terme `"connexion"`, mais aucun des trois mots-clés du premier `if`. Par conséquent, le second `else if` s'active systématiquement !
- **Rayon d'impact :**  
  Violation directe des critères UX Figma et de la spécification F19_B01/B02. Un utilisateur se trompant de mot de passe croit que l'application a un bogue réseau ou que le serveur BTS SIO est en panne, ce qui empêche tout diagnostic.
- **Preuve Empirique Verbatim :**  
  Test `AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner` :
  ```
  Assert.Contains() Failure: Sub-string not found
  String:    "Impossible de joindre le serveur. Vérifiez votre connexion Internet."
  Not found: "Identifiants invalides"
  ```
- **Atténuation préconisée :**
  Dans `ConnexionVueModele.cs`, ajouter les termes `"refusée"` et `"expirée"` au premier prédicat, ou tester le code de statut d'authentification directement :
  ```csharp
  if (details.Contains("401") || details.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) || details.Contains("invalide", StringComparison.OrdinalIgnoreCase) || details.Contains("refusée", StringComparison.OrdinalIgnoreCase))
  ```

---

### Défi 3 [MOYENNE] : Fenêtre de Concurrence sur `AcheterOffreAsync` avant `DisplayAlertAsync`

- **Composant cible :** `AutoMasters/VuesModeles/MarcheVueModele.cs`, lignes 181-220.
- **Analyse :**  
  Le drapeau `EstOccupe = true;` n'est activé qu'à la ligne 217, *après* le dialogue modal `DisplayAlertAsync`. Pendant l'affichage du dialogue de confirmation, `EstOccupe` est faux. De plus, `ServiceAPI.AcheterOffreAsync` régénère un nouveau `Guid.NewGuid()` à chaque appel. Si deux appels franchissent la modale, deux clés d'idempotence distinctes seront émises, contournant la protection d'idempotence backend (RM006).
- **Atténuation préconisée :**  
  Positionner `EstOccupe = true;` en amont de `DisplayAlertAsync` et le réinitialiser en cas d'annulation de l'alerte.

---

## 3. Résultats des Tests de Stress Empiriques

Un banc d'essai complet de 17 tests contradictoires a été exécuté dans `AutoMasters.Tests/ViewModelEmpiricalChallengeTests.cs`.

| Domaine / Feature | Test Empirique | Résultat | Observation / Données Vérifiées |
|---|---|---|---|
| **MarcheVueModele** | `Marche_AcheterOffreAsync_EmitsIdempotencyKeyInHeaderAndBody` | **PASS** ✅ | En-tête `Idempotency-Key` (GUID) et corps JSON conformes. Solde décrémenté de 250 000 coins. |
| **MarcheVueModele** | `Marche_AcheterOffreAsync_InsufficientBalance_AbortsWithoutApiCall` | **PASS** ✅ | Solde insuffisant (50 000 < 300 000) bloque l'appel réseau localement et affiche "Fonds insuffisants". |
| **MarcheVueModele** | `Marche_AcheterOffreAsync_NullOffer_IsSafeNoOp` | **PASS** ✅ | Offre nulle gérée sans exception. |
| **MarcheVueModele** | `Marche_AcheterOffreAsync_ApiError_DoesNotDeductBalance` | **PASS** ✅ | En cas d'erreur API 400, le solde n'est pas débité et le message est propagé. |
| **MarcheVueModele** | `Marche_ReserverOffreAsync_SetsReservedStatusAnd15MinCountdown` | **PASS** ✅ | Verrouillage POST `offers/{id}/reservations`, statut `reserved`, `DateExpiration` à UtcNow + 15 min. |
| **MarcheVueModele** | `Marche_ReserverOffreAsync_AlreadyReserved_RejectsWithoutNetworkCall` | **PASS** ✅ | Offre déjà réservée rejetée localement sans appel réseau. |
| **CreationOffreVueModele** | `CreationOffre_PrixInferieurOuEgalAZero_EstRejete` (0, 0.00, -10, -250000) | **PASS** ✅ | Prix <= 0 rejetés avec le message exact "Le prix de vente doit être strictement supérieur à zéro." |
| **CreationOffreVueModele** | `CreationOffre_FormatPrixInvalide_EstRejete` ("", "abc", "!@#$") | **PASS** ✅ | Formats invalides rejetés avec "Veuillez saisir un montant numérique valide." |
| **CreationOffreVueModele** | `CreationOffre_AucunVehiculeSelectionne_EstRejete` | **PASS** ✅ | Véhicule non sélectionné rejeté avec "Veuillez sélectionner un véhicule de votre garage." |
| **CreationOffreVueModele** | `CreationOffre_PrixValide_EnvoieRequetePostOffers` ("250 000", "250000,50") | **PASS** ✅ | Parsing des espaces et virgules réussi, POST `offers` avec copyId et price. |
| **GarageVueModele** | `Garage_GainPositif_AfficheVertEtSignePlus` | **PASS** ✅ | Valorisation 482 500 coins, P&L "+32 500 € P&L total", couleur `#10B981` (vert). |
| **GarageVueModele** | `Garage_PerteNegative_AfficheRougeEtSigneMoins` | **PASS** ✅ | P&L "-2 000 € P&L total", absence de double signe négatif, couleur `#EF4444` (rouge). |
| **GarageVueModele** | `CartePossedee_QuantiteBadge_AfficheMultiplicateurUniquementSiSuperieurAUn` | **PASS** ✅ | Multiplicateur masqué pour qte=1, `"x2"` pour qte=2, `"x3"` pour qte=3. |
| **BoosterVueModele** | `Booster_SequenceComplete5Cartes_ProgressionEtRetournementInteractif` | **PASS** ✅ | Séquence "Carte 1 sur 5" -> "Carte 5 sur 5", segments 1 à 5, alternance "RETOURNER ▶" / "CARTE SUIVANTE ▶", tokens rareté (#F59E0B, #A855F7, #38BDF8, #9CA3AF). |
| **BoosterVueModele** | `Garage_CoordinationBooster_DecrementeQuotaEtRechargeCollection` | **PASS** ✅ | Quota 3 -> 2 après ouverture nominale complète et rechargement de collection. |
| **BoosterVueModele** | `Garage_QuotaEpuise_NePeutPasOuvrirBooster` | **PASS** ✅ | Quota à 0 désactive `PeutOuvrirBooster` et bloque l'ouverture. |
| **BoosterVueModele** | `Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine` | **FAIL** ❌ | **Bogue prouvé : SurBoosterTermine déclenché 2 fois sur double-clic (déduction double de quota).** |
| **ConnexionVueModele** | `Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner` | **FAIL** ❌ | **Bogue prouvé : Erreur 401 affiche "Impossible de joindre le serveur" au lieu d'"Identifiants invalides".** |

---

## 4. Instructions Précises de Remédiation pour `worker_m3`

Pour que le Milestone 3 obtienne l'approbation, `worker_m3` doit appliquer les corrections suivantes :

1. **Dans `AutoMasters/VuesModeles/BoosterVueModele.cs` :**
   À la méthode `ActionCarteSuivante()`, ajouter la protection contre la réentrance dès la première ligne :
   ```csharp
   if (cartesTirees.Count == 0 || EstTermine)
       return;
   ```
2. **Dans `AutoMasters/VuesModeles/ConnexionVueModele.cs` :**
   Dans le bloc `catch (Exception ex)` de `SeConnecterAsync()`, mettre à jour la détection d'erreur :
   ```csharp
   if (details.Contains("401") ||
       details.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
       details.Contains("invalide", StringComparison.OrdinalIgnoreCase) ||
       details.Contains("refusée", StringComparison.OrdinalIgnoreCase))
   {
       Message = "Identifiants invalides. Vérifiez votre adresse email et votre mot de passe.";
   }
   ```
3. **Dans `AutoMasters/VuesModeles/MarcheVueModele.cs` :**
   Placer `EstOccupe = true;` avant `DisplayAlertAsync` dans `AcheterOffreAsync` pour éviter tout clic concurrent durant le dialogue modal récapitulatif.

---

## 5. Conclusion

Le travail réalisé par `worker_m3` présente une excellente fidélité visuelle et structurelle, mais contient des failles de logique d'état et de routage d'erreurs révélées sous conditions adverses.
Le verdict est **REQUEST_CHANGES**. Les corrections sont légères, ciblées et immédiatement vérifiables.
