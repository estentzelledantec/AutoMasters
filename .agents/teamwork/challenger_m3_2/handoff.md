# Handoff Report — Milestone 3 Adversarial Challenge

## 1. Observation

- **Test unitaire contradictoire de réentrance Booster (`AutoMasters.Tests/ViewModelEmpiricalChallengeTests.cs`) :**
  Commande : `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine"`
  Sortie verbatim :
  ```
  [xUnit.net 00:00:00.44]     AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine [FAIL]
    Échoué AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine [5 ms]
    Message d'erreur :
     Assert.Equal() Failure: Values differ
  Expected: 1
  Actual:   2
    Arborescence des appels de procédure :
       at AutoMasters.Tests.ViewModelEmpiricalChallengeTests.Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ViewModelEmpiricalChallengeTests.cs:line 589
  ```

- **Test unitaire contradictoire de capture d'erreur 401 (`AutoMasters.Tests/ChallengerM3VerificationTests.cs`) :**
  Commande : `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner"`
  Sortie verbatim :
  ```
  [xUnit.net 00:00:00.43]     AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner [FAIL]
    Échoué AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner [5 ms]
    Message d'erreur :
     Assert.Contains() Failure: Sub-string not found
  String:    "Impossible de joindre le serveur. Vérifiez votre connexion Internet."
  Not found: "Identifiants invalides"
    Arborescence des appels de procédure :
       at AutoMasters.Tests.ChallengerM3VerificationTests.Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner() in C:\Users\stent\source\repos\AutoMasters\AutoMasters.Tests\ChallengerM3VerificationTests.cs:line 178
  ```

- **Examen de code dans `AutoMasters/VuesModeles/BoosterVueModele.cs` :**
  Lignes 168 à 171 :
  ```csharp
  [RelayCommand]
  public void ActionCarteSuivante()
  {
      if (cartesTirees.Count == 0)
          return;
  ```
  Lignes 204 à 208 :
  ```csharp
      else
      {
          // Fin de la révélation
          EstTermine = true;
          SurBoosterTermine?.Invoke();
      }
  ```
  Constat : `ActionCarteSuivante()` ne vérifie pas `EstTermine` en tête de méthode. Tout appel subséquent après la fin réinvoque `SurBoosterTermine?.Invoke()`.

- **Examen de code dans `AutoMasters/VuesModeles/GarageVueModele.cs` :**
  Lignes 172 à 177 :
  ```csharp
  vmBooster.SurBoosterTermine += async () =>
  {
      EstBoosterModalOuvert = false;
      PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
      await ChargerGarageAsync();
  };
  ```
  Constat : Chaque déclenchement de `SurBoosterTermine` décrémente `PaquetsDisponibles`.

- **Examen de code dans `AutoMasters/Services/ServiceAPI.cs` et `AutoMasters/VuesModeles/ConnexionVueModele.cs` :**
  `ServiceAPI.cs`, lignes 111 à 115 :
  ```csharp
  if (reponse.StatusCode == HttpStatusCode.Unauthorized)
  {
      ReinitialiserSession();
      throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
  }
  ```
  `ConnexionVueModele.cs`, lignes 163 à 171 :
  ```csharp
  string details = ex.Message ?? "";
  if (details.Contains("401") || details.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) || details.Contains("invalide", StringComparison.OrdinalIgnoreCase))
  {
      Message = "Identifiants invalides. Vérifiez votre adresse email et votre mot de passe.";
  }
  else if (details.Contains("connexion", StringComparison.OrdinalIgnoreCase) || details.Contains("serveur", StringComparison.OrdinalIgnoreCase) || details.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase))
  {
      Message = "Impossible de joindre le serveur. Vérifiez votre connexion Internet.";
  }
  ```
  Constat : Le message d'exception contient le mot `"connexion"` ("Connexion refusée..."). Le premier test échoue car ni "401", ni "Unauthorized", ni "invalide" n'apparaissent dans `ex.Message`. Le second `else if` s'exécute et affecte `"Impossible de joindre le serveur"`.

---

## 2. Logic Chain

1. **Déduction sur la réentrance du booster :**
   - Observation : `BoosterVueModele.ActionCarteSuivante()` n'a pas de condition de sortie si `EstTermine == true`.
   - Observation : `SurBoosterTermine?.Invoke()` est invoqué à chaque exécution de la branche finale.
   - Observation : Dans `GarageVueModele`, `SurBoosterTermine` décrémente `PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1)`.
   - Observation : Le test empirique `Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine` démontre que deux clics sur le bouton de fin entraînent deux invocations de l'événement (`eventFiredCount == 2`).
   - Conclusion : Un double-clic ou double-tap utilisateur sur "TERMINER & AJOUTER AU GARAGE" consomme deux boosters au lieu d'un seul.

2. **Déduction sur le masquage d'erreur 401 :**
   - Observation : L'API ou son mock renvoie 401 Unauthorized pour des identifiants incorrects.
   - Observation : `ServiceAPI` lève `new Exception("Connexion refusée ou expirée. Reconnectez-vous.")`.
   - Observation : `ConnexionVueModele` teste d'abord si la chaîne contient "401", "Unauthorized" ou "invalide". C'est faux.
   - Observation : `ConnexionVueModele` teste ensuite si la chaîne contient "connexion". C'est vrai car `ex.Message` commence par "Connexion".
   - Conclusion : En cas d'identifiants incorrects, l'application informe faussement l'utilisateur d'un problème de connexion réseau au serveur, masquant l'erreur réelle d'authentification.

3. **Déduction sur la conformité des autres composants :**
   - Les tests empiriques sur l'achat immédiat avec clé d'idempotence, la réservation 15 minutes, la validation des prix <= 0, le calcul de valorisation, le format P&L coloré et le badge "x2" passent tous avec succès.

4. **Synthèse logique :**
   - Les deux défaillances constatées impactent directement l'expérience utilisateur et violent les critères de robustesse et d'exactitude UX. Elles nécessitent une remédiation immédiate.

---

## 3. Caveats

- Les tests ont été exécutés en environnement hors-ligne déterministe avec `MockHttpMessageHandler` sans backend réseau distant temps réel (ce qui est conforme aux spécifications M2/M3).
- L'interface graphique MAUI réelle n'a pas été exécutée avec affichage écran interactif WinUI desktop (exécution des ViewModels en mode headless unitaire).
- Aucun autre caveat.

---

## 4. Conclusion

**Verdict final : REQUEST_CHANGES ❌**

Deux corrections précises sont requises de la part de `worker_m3` :
1. Ajouter `if (cartesTirees.Count == 0 || EstTermine) return;` dans `AutoMasters/VuesModeles/BoosterVueModele.cs` (`ActionCarteSuivante`).
2. Ajouter la détection de `"refusée"` ou `"expirée"` dans le premier `if` de traitement d'erreur de `AutoMasters/VuesModeles/ConnexionVueModele.cs`.

---

## 5. Verification Method

Pour reproduire de manière autonome et indépendante les défaillances :
1. Exécuter le test de réentrance du booster :
   `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~Booster_Adversarial_RapidDoubleTapOnFinish_ShouldNotDoubleInvokeSurBoosterTermine"`
   -> Échoue avec `Expected: 1, Actual: 2`.
2. Exécuter le test de masquage de l'erreur 401 :
   `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --filter "FullyQualifiedName~Challenger_Login_InvalidCredentials_401_PreservesEmail_ClearsPassword_ShowsErrorBanner"`
   -> Échoue avec `String: "Impossible de joindre le serveur...", Not found: "Identifiants invalides"`.
3. Conditions d'invalidation du rejet :
   Après correction par `worker_m3`, exécuter :
   `dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj`
   Tous les 77 tests de la suite unitaire doivent réussir (0 échec).
