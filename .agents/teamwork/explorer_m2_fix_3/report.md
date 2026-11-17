# Rapport de Stratégie d'Extension des Tests Unitaires — AutoMasters.Tests/ServiceApiTests.cs

**Agent :** `explorer_m2_fix_3`  
**Rôle :** Explorateur / Analyste Spécialiste Tests & Vérification  
**Cible :** `AutoMasters.Tests/ServiceApiTests.cs` (Fonctionnalité F17 / Milestone M2)  
**Date :** 2026-10-07T18:15:00Z  
**Statut :** Recommandation Prête pour Implémentation  

---

## 1. Résumé Exécutif

Cette stratégie étend la suite de tests unitaires `AutoMasters.Tests` de 27 à 33 tests (+22% de couverture) en spécifiant le code C# exact pour valider la révocation immédiate de session sur HTTP 401, la désérialisation de tableaux JSON bruts (`[ { ... } ]`) sur les 4 méthodes de collection, et la capture propre des flux JSON corrompus sur code HTTP 200.
Toutes les spécifications sont 100% compatibles avec `MockHttpMessageHandler`, déterministes, exécutables hors-ligne et exemptes d'assertions tautologiques.

---

## 2. Matrice d'Audit et Justification des Nouveaux Cas de Test

| # | Nom du Test Proposé | Endpoint & Méthode | Scénario & Payload d'Entrée | État & Assertions Validés | Défaut Résolu / Justification |
|---|---------------------|--------------------|-----------------------------|---------------------------|--------------------------------|
| 1 | `Test_401_RevoqueSessionEtAuthentification` | GET `/cards` via `RecupererCatalogueAsync` | Session active préalable puis réponse HTTP 401 Unauthorized (`{ "code": "UNAUTHORIZED" }`) | `EstAuthentifie == false`, `DefaultRequestHeaders.Authorization == null`, `Jeton == null`, `ex.Message` contient "Connexion refusée ou expirée" | Élimine la faille critique de rétention de jeton identifiée par `challenger_m2_1`. |
| 2 | `RecupererCatalogueAsync_TableauBrutDirect_DeserialiseListeCartes` | GET `/cards` via `RecupererCatalogueAsync` | Tableau brut `[ { "id": "card_alpine_01", ... }, { ... } ]` sans enveloppe `"items"` | Retourne `List<Carte>` avec 2 éléments, vérifie Marque, Modèle, Rareté, Prix, Puissance | Couvre la branche `token is JArray` non testée (identifiée par `challenger_m2_2`). |
| 3 | `RecupererPortefeuilleAsync_TableauBrutDirect_CreePortefeuilleEtCalculeValorisation` | GET `/portfolio` via `RecupererPortefeuilleAsync` | Tableau brut `[ { "id": "copy_alpine", ... } ]` sans enveloppe `"positions"` | `Portefeuille.Positions.Count == 2`, calcul dynamique de `ValeurTotale`, `TotalGainPerte`, `EstEnGain` | Couvre la branche `token is JArray` et valide le calcul dynamique des métriques du garage. |
| 4 | `RecupererOffresAsync_TableauBrutDirect_DeserialiseOffres` | GET `/offers` via `RecupererOffresAsync` | Tableau brut `[ { "id": "offer_01", ... }, { ... } ]` sans enveloppe `"items"` | `List<Offre>` avec 2 éléments, vérifie `Identifiant`, `Vendeur`, `Prix`, `Statut`, `Carte.Marque` | Couvre la branche `token is JArray` pour la place de marché. |
| 5 | `OuvrirBoosterAsync_TableauBrutDirect_Retourne5Cartes` | POST `/boosters/open` via `OuvrirBoosterAsync` | Tableau brut `[ { "id": "c1", ... }, ... ]` de 5 cartes sans enveloppe `"cards"` | `List<Carte>` avec exactement 5 cartes distinctes, vérifie marques et modèles | Couvre la branche `token is JArray` pour l'ouverture de boosters. |
| 6 | `RecupererCatalogueAsync_Reponse200JsonCorrompu_LeveExceptionClaireSansCrash` | GET `/cards` via `RecupererCatalogueAsync` | HTTP 200 OK avec chaîne JSON invalide `"{ invalid json }"` | Lève `Exception` avec message non vide et explicite ("format", "invalide", ou "JSON"), sans crash non géré | Couvre le cas limite de corruption de flux réseau / portail captif identifié par `challenger_m2_2`. |

---

## 3. Spécification Détaillée du Code des Cas de Test

Les 6 méthodes de test suivantes sont rédigées en C# 12 / .NET 10, conformes aux conventions du projet BTS SIO (régions explicites, nommage clair, typage fort, `MockHttpMessageHandler`).

```csharp
    // =========================================================================
    // 1. Révocation de session sur HTTP 401 Unauthorized
    // =========================================================================

    [Fact]
    public async Task Test_401_RevoqueSessionEtAuthentification()
    {
        // Disposition : établissement préalable d'une session valide
        string jetonTest = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.token_actif_avant_401";
        _mockHandler.QuandPost("/auth/login", HttpStatusCode.OK, new
        {
            accessToken = jetonTest,
            refreshToken = "refresh_token_test",
            expiresAt = DateTime.UtcNow.AddHours(2)
        });

        await _service.SeConnecterAsync("etudiant@bts.sio", "MotDePasse123!");

        // Vérification de l'état initial : utilisateur authentifié avec Bearer token
        Assert.True(_service.EstAuthentifie, "L'utilisateur doit être authentifié initialement.");
        Assert.NotNull(_service.ClientHttp.DefaultRequestHeaders.Authorization);
        Assert.Equal("Bearer", _service.ClientHttp.DefaultRequestHeaders.Authorization.Scheme);
        Assert.Equal(jetonTest, _service.ClientHttp.DefaultRequestHeaders.Authorization.Parameter);
        Assert.Equal(jetonTest, _service.Jeton);

        // Simulation de l'expiration/révocation : l'API renvoie 401 sur un endpoint protégé (GET /cards)
        _mockHandler.QuandGet("/cards", HttpStatusCode.Unauthorized, new
        {
            code = "UNAUTHORIZED",
            message = "Jeton de session expiré ou révoqué."
        });

        // Action : appel protégé levant l'exception
        var ex = await Assert.ThrowsAsync<Exception>(() => _service.RecupererCatalogueAsync());
        Assert.Contains("Connexion refusée ou expirée", ex.Message);

        // Assertions critiques : révocation immédiate de la session en mémoire et dans les en-têtes
        Assert.False(_service.EstAuthentifie, "EstAuthentifie doit impérativement basculer à false après réception d'un code 401.");
        Assert.Null(_service.ClientHttp.DefaultRequestHeaders.Authorization);
        Assert.Null(_service.Jeton);
    }

    // =========================================================================
    // 2. Désérialisation de Tableaux JSON Bruts [ { ... } ]
    // =========================================================================

    [Fact]
    public async Task RecupererCatalogueAsync_TableauBrutDirect_DeserialiseListeCartes()
    {
        // Disposition : le serveur retourne un tableau brut JSON direct sans enveloppe { items: [...] }
        string jsonTableauBrut = @"[
            {
                ""id"": ""card_alpine_01"",
                ""brand"": ""Alpine"",
                ""model"": ""A110 R"",
                ""variant"": ""Turini"",
                ""year"": 2023,
                ""powerHp"": 300,
                ""rarity"": ""ÉPIQUE"",
                ""officialPrice"": 105000.0,
                ""circulationCount"": 500,
                ""imageUrl"": ""https://images.unsplash.com/alpine.jpg"",
                ""description"": ""Version sportive allégée.""
            },
            {
                ""id"": ""card_bugatti_01"",
                ""brand"": ""Bugatti"",
                ""model"": ""Chiron Super Sport"",
                ""variant"": ""300+"",
                ""year"": 2021,
                ""powerHp"": 1600,
                ""rarity"": ""LÉGENDAIRE"",
                ""officialPrice"": 3500000.0,
                ""circulationCount"": 30,
                ""imageUrl"": ""https://images.unsplash.com/bugatti.jpg"",
                ""description"": ""Hypercar franchissant les 490 km/h.""
            }
        ]";

        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        List<Carte> cartes = await _service.RecupererCatalogueAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Equal(2, cartes.Count);

        var c1 = cartes[0];
        Assert.Equal("card_alpine_01", c1.Identifiant);
        Assert.Equal("Alpine", c1.Marque);
        Assert.Equal("A110 R", c1.Modele);
        Assert.Equal("Turini", c1.Variante);
        Assert.Equal(2023, c1.Annee);
        Assert.Equal(300, c1.Puissance);
        Assert.Equal("ÉPIQUE", c1.Rarete);
        Assert.Equal(105000m, c1.PrixOfficiel);

        var c2 = cartes[1];
        Assert.Equal("card_bugatti_01", c2.Identifiant);
        Assert.Equal("Bugatti", c2.Marque);
        Assert.Equal("Chiron Super Sport", c2.Modele);
        Assert.Equal(1600, c2.Puissance);
        Assert.Equal("LÉGENDAIRE", c2.Rarete);
        Assert.Equal(3500000m, c2.PrixOfficiel);
    }

    [Fact]
    public async Task RecupererPortefeuilleAsync_TableauBrutDirect_CreePortefeuilleEtCalculeValorisation()
    {
        // Disposition : le serveur retourne un tableau brut JSON de positions sans enveloppe { positions: [...] }
        string jsonTableauBrut = @"[
            {
                ""id"": ""copy_alpine_garage"",
                ""card"": {
                    ""id"": ""card_alpine_01"",
                    ""brand"": ""Alpine"",
                    ""model"": ""A110 R"",
                    ""officialPrice"": 105000.0
                },
                ""currentValue"": 115000.0,
                ""quantity"": 2,
                ""purchasePrice"": 100000.0
            },
            {
                ""id"": ""copy_porsche_garage"",
                ""card"": {
                    ""id"": ""card_porsche_01"",
                    ""brand"": ""Porsche"",
                    ""model"": ""911 GT3 RS"",
                    ""officialPrice"": 223000.0
                },
                ""currentValue"": 240000.0,
                ""quantity"": 1,
                ""purchasePrice"": 210000.0
            }
        ]";

        _mockHandler.QuandGet("/portfolio", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        Portefeuille portefeuille = await _service.RecupererPortefeuilleAsync();

        // Assertions
        Assert.NotNull(portefeuille);
        Assert.Equal(2, portefeuille.Positions.Count);

        // Position 1 (Alpine - Quantité 2)
        var pos1 = portefeuille.Positions[0];
        Assert.Equal("copy_alpine_garage", pos1.Identifiant);
        Assert.Equal("Alpine", pos1.Carte.Marque);
        Assert.Equal(2, pos1.Quantite);
        Assert.Equal(115000m, pos1.ValeurActuelle);
        Assert.Equal(100000m, pos1.PrixAchat);
        // Gain attendu : 2 * (115 000 - 100 000) = +30 000
        Assert.Equal(30000m, pos1.GainPerte);

        // Position 2 (Porsche - Quantité 1)
        var pos2 = portefeuille.Positions[1];
        Assert.Equal("copy_porsche_garage", pos2.Identifiant);
        Assert.Equal("Porsche", pos2.Carte.Marque);
        Assert.Equal(1, pos2.Quantite);
        Assert.Equal(240000m, pos2.ValeurActuelle);
        Assert.Equal(210000m, pos2.PrixAchat);
        // Gain attendu : 1 * (240 000 - 210 000) = +30 000
        Assert.Equal(30000m, pos2.GainPerte);

        // Calculs agrégés calculés dynamiquement par le constructeur Portefeuille(positions)
        // ValeurTotale : (115 000 * 2) + (240 000 * 1) = 230 000 + 240 000 = 470 000
        Assert.Equal(470000m, portefeuille.ValeurTotale);
        // TotalGainPerte : 30 000 + 30 000 = +60 000
        Assert.Equal(60000m, portefeuille.TotalGainPerte);
        Assert.Equal(3, portefeuille.NombreTotalCartes);
        Assert.Equal(2, portefeuille.NombreModelesDistincts);
        Assert.True(portefeuille.EstEnGain);
    }

    [Fact]
    public async Task RecupererOffresAsync_TableauBrutDirect_DeserialiseOffres()
    {
        // Disposition : le serveur retourne un tableau brut JSON direct d'offres sans enveloppe { items: [...] }
        string jsonTableauBrut = @"[
            {
                ""id"": ""offer_direct_01"",
                ""copyId"": ""copy_ferrari_direct"",
                ""card"": {
                    ""id"": ""card_ferrari_101"",
                    ""brand"": ""Ferrari"",
                    ""model"": ""LaFerrari Aperta""
                },
                ""seller"": ""ScuderiaBTS"",
                ""price"": 450000.0,
                ""status"": ""active""
            },
            {
                ""id"": ""offer_direct_02"",
                ""copyId"": ""copy_toyota_direct"",
                ""card"": {
                    ""id"": ""card_toyota_107"",
                    ""brand"": ""Toyota"",
                    ""model"": ""GR Yaris""
                },
                ""seller"": ""RallyFan"",
                ""price"": 42000.0,
                ""status"": ""active""
            }
        ]";

        _mockHandler.QuandGet("/offers", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        List<Offre> offres = await _service.RecupererOffresAsync();

        // Assertions
        Assert.NotNull(offres);
        Assert.Equal(2, offres.Count);

        var off1 = offres[0];
        Assert.Equal("offer_direct_01", off1.Identifiant);
        Assert.Equal("copy_ferrari_direct", off1.IdentifiantExemplaire);
        Assert.Equal("ScuderiaBTS", off1.Vendeur);
        Assert.Equal(450000m, off1.Prix);
        Assert.Equal("active", off1.Statut);
        Assert.False(off1.EstReservee);
        Assert.NotNull(off1.Carte);
        Assert.Equal("Ferrari", off1.Carte.Marque);

        var off2 = offres[1];
        Assert.Equal("offer_direct_02", off2.Identifiant);
        Assert.Equal("RallyFan", off2.Vendeur);
        Assert.Equal(42000m, off2.Prix);
        Assert.NotNull(off2.Carte);
        Assert.Equal("Toyota", off2.Carte.Marque);
    }

    [Fact]
    public async Task OuvrirBoosterAsync_TableauBrutDirect_Retourne5Cartes()
    {
        // Disposition : le serveur retourne un tableau brut JSON direct de 5 cartes sans enveloppe { cards: [...] }
        string jsonTableauBrut = @"[
            { ""id"": ""card_b1"", ""brand"": ""Ferrari"", ""model"": ""F40"", ""rarity"": ""LÉGENDAIRE"" },
            { ""id"": ""card_b2"", ""brand"": ""Porsche"", ""model"": ""911 GT3 RS"", ""rarity"": ""ÉPIQUE"" },
            { ""id"": ""card_b3"", ""brand"": ""Renault"", ""model"": ""Clio V6"", ""rarity"": ""RARE"" },
            { ""id"": ""card_b4"", ""brand"": ""Peugeot"", ""model"": ""205 Turbo 16"", ""rarity"": ""RARE"" },
            { ""id"": ""card_b5"", ""brand"": ""Alpine"", ""model"": ""A110 R"", ""rarity"": ""ÉPIQUE"" }
        ]";

        _mockHandler.QuandPost("/boosters/open", HttpStatusCode.OK, jsonTableauBrut);

        // Action
        List<Carte> cartes = await _service.OuvrirBoosterAsync();

        // Assertions
        Assert.NotNull(cartes);
        Assert.Equal(5, cartes.Count);

        Assert.Equal("card_b1", cartes[0].Identifiant);
        Assert.Equal("Ferrari", cartes[0].Marque);
        Assert.Equal("F40", cartes[0].Modele);
        Assert.Equal("LÉGENDAIRE", cartes[0].Rarete);

        Assert.Equal("card_b2", cartes[1].Identifiant);
        Assert.Equal("Porsche", cartes[1].Marque);

        Assert.Equal("card_b3", cartes[2].Identifiant);
        Assert.Equal("Renault", cartes[2].Marque);

        Assert.Equal("card_b4", cartes[3].Identifiant);
        Assert.Equal("Peugeot", cartes[3].Marque);

        Assert.Equal("card_b5", cartes[4].Identifiant);
        Assert.Equal("Alpine", cartes[4].Marque);
    }

    // =========================================================================
    // 3. Gestion Propre du JSON Corrompu sur Réponse HTTP 200 OK
    // =========================================================================

    [Fact]
    public async Task RecupererCatalogueAsync_Reponse200JsonCorrompu_LeveExceptionClaireSansCrash()
    {
        // Disposition : le serveur retourne HTTP 200 OK avec un corps textuel JSON malformé / corrompu
        string jsonInvalide = "{ invalid json }";
        _mockHandler.QuandGet("/cards", HttpStatusCode.OK, jsonInvalide);

        // Action & Assertion : l'appel doit lever une Exception gérée proprement
        var ex = await Assert.ThrowsAsync<Exception>(() =>
            _service.RecupererCatalogueAsync());

        // Assertions : le message doit être informatif et non vide (pas de chaîne vide silencieuse)
        Assert.NotNull(ex);
        Assert.False(string.IsNullOrWhiteSpace(ex.Message), "Le message d'erreur ne doit jamais être vide lors d'une désérialisation échouée.");
        Assert.True(
            ex.Message.Contains("format", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("invalide", StringComparison.OrdinalIgnoreCase) ||
            ex.Message.Contains("JSON", StringComparison.OrdinalIgnoreCase),
            $"Le message d'erreur doit expliciter une anomalie de format ou de données invalides. Message obtenu: '{ex.Message}'");
    }
```

---

## 4. Guide d'Emplacement Précis dans `AutoMasters.Tests/ServiceApiTests.cs`

Deux options d'intégration équivalentes s'offrent à l'agent d'implémentation (`worker_m2_fix`) :

### Option A (Recommandée — Placement par Domaine Fonctionnel)
Cette option insère chaque test dans sa région thématique respective :
1. **`Test_401_RevoqueSessionEtAuthentification`** : Dans `#region Tests Authentification (F07)`, immédiatement après le test de panne réseau (ligne 129).
2. **`RecupererCatalogueAsync_TableauBrutDirect_DeserialiseListeCartes`** : Dans `#region Tests Catalogue (F08, F09)`, après le test de désérialisation standard (ligne 200).
3. **`RecupererPortefeuilleAsync_TableauBrutDirect_CreePortefeuilleEtCalculeValorisation`** : Dans `#region Tests Portefeuille & Garage (F10)`, après le test de calcul de valorisation (ligne 344).
4. **`RecupererOffresAsync_TableauBrutDirect_DeserialiseOffres`** : Dans `#region Tests Marché & Transactions (F11, F12, F13, F14)`, après le test de désérialisation des offres (ligne 430).
5. **`OuvrirBoosterAsync_TableauBrutDirect_Retourne5Cartes`** : Dans `#region Tests Boosters (F15)`, après le test de quota disponible (ligne 608).
6. **`RecupererCatalogueAsync_Reponse200JsonCorrompu_LeveExceptionClaireSansCrash`** : Dans `#region Tests Robustesse, DI & Cas Limites`, après le test `ServiceAPI_ReponseHtml502_GereErreurServeurSansCrashJson` (ligne 644).

### Option B (Bloc Contigu — Région Dédiée)
Ajouter un bloc contigu en fin de classe avant la fermeture de la classe (ligne 698) :
```csharp
    #region Tests Correctifs M2 (Révocation 401, Tableaux Bruts & JSON Corrompu)
    // [Insérer les 6 tests ici]
    #endregion
```
*Note :* Mettre à jour le commentaire XML d'en-tête de la classe `ServiceApiTests` (ligne 15) pour refléter :  
`/// Suite de 33 tests unitaires exhaustive pour ServiceAPI (Fonctionnalité F17).`

---

## 5. Dépendances et Synchronisation avec les Correctifs

1. **Dépendance avec `explorer_m2_fix_1` :**  
   Le test `Test_401_RevoqueSessionEtAuthentification` requiert l'implémentation de la réinitialisation de `client.DefaultRequestHeaders.Authorization = null;`, `jetonSession = null;` et `SecureStorage.Default.Remove(CleStockageJeton)` dans `LireReponseAsync`.
2. **Dépendance avec `explorer_m2_fix_2` :**  
   Le test `RecupererCatalogueAsync_Reponse200JsonCorrompu_LeveExceptionClaireSansCrash` valide que l'interception de `JsonException` sur les réponses 200 OK produit un message non vide et compréhensible pour l'utilisateur.
3. **Indépendance des tests de tableaux bruts :**  
   Les tests de tableaux bruts (`RecupererCatalogueAsync`, `RecupererPortefeuilleAsync`, `RecupererOffresAsync`, `OuvrirBoosterAsync`) s'appuient sur le code déjà existant (`token is JArray`) dans `ServiceAPI.cs` et passeront immédiatement dès leur ajout.

---

## 6. Protocole de Vérification Indépendante

Après application des modifications dans `AutoMasters.Tests/ServiceApiTests.cs` :
```powershell
dotnet test AutoMasters.Tests/AutoMasters.Tests.csproj --logger "console;verbosity=detailed"
```
**Critères de succès :**
- 33 tests passés avec succès (0 échec, 0 ignoré).
- Temps d'exécution total < 2 secondes (mocks in-memory).
- Aucune régression sur les 27 tests existants.
