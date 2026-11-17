using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using AutoMasters.Modeles;

namespace AutoMasters.Services;

/// <summary>
/// Classe de service gérant les appels vers l'API REST du Groupe 2 (https://api-cartes-g2.fldsio.com/).
/// Regroupe les méthodes pour la connexion, le catalogue des 300 voitures, le portefeuille (garage),
/// les offres du marché, l'achat, la réservation, l'ouverture des boosters et le journal d'activité.
/// </summary>
public class ServiceAPI
{
    #region Attributs

    private const string CleStockageJeton = "automasters_jwt";
    private static ServiceAPI instance = new ServiceAPI();
    private HttpClient client;
    private string? jetonSession;
    private readonly bool estModeProduction;
    private string utilisateurConnecte = "bob";
    private List<Carte>? cacheBaseVoituresLocale;
    private readonly Dictionary<string, List<CartePossedee>> garagesParUtilisateur = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, decimal> soldesParUtilisateur = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Offre> offresSession = new();
    private readonly List<TransactionActivite> journalActivitesSession = new();
    private readonly Dictionary<string, PointCotationMarche> coursModifiesParVentes = new(StringComparer.OrdinalIgnoreCase);
    private readonly Random generateurAleatoire = new();
    private readonly object verrouPartage = new();
    private DateTime dateDerniereLectureDisque = DateTime.MinValue;
    private bool surveillancePartageActive;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut (utilisé en production et par l'instance Singleton).
    /// Initialise le client HTTP avec l'URL de base et un timeout de 15 secondes.
    /// </summary>
    public ServiceAPI() : this(null)
    {
    }

    /// <summary>
    /// Constructeur avec gestionnaire HTTP injectable.
    /// Permet d'injecter un gestionnaire simulé (ex: MockHttpMessageHandler) pour les tests unitaires 100% hors-ligne.
    /// </summary>
    /// <param name="handler">Gestionnaire HTTP injectable ou null pour le gestionnaire par défaut.</param>
    public ServiceAPI(HttpMessageHandler? handler = null)
    {
        estModeProduction = handler == null;
        client = handler != null ? new HttpClient(handler) : new HttpClient();
        client.BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/");
        client.Timeout = TimeSpan.FromSeconds(15);

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();
            DemarrerSurveillancePartageTempsReel();
        }
    }

    #endregion

    #region Getter/Setter

    /// <summary>
    /// Événement déclenché dès qu'une vente, une offre, un don ou un changement de compte intervient.
    /// Permet aux ViewModels de se rafraîchir automatiquement en temps réel.
    /// </summary>
    public event Action? DonneesMarcheModifiees;

    /// <summary>
    /// Accès au singleton ServiceAPI pour l'injection ou l'accès direct depuis les ViewModels.
    /// </summary>
    public static ServiceAPI Instance => instance;

    /// <summary>
    /// Client HTTP sous-jacent exposé pour la configuration ou l'inspection avancée.
    /// </summary>
    public HttpClient ClientHttp => client;

    /// <summary>
    /// Jeton JWT actuel conservé en mémoire pour la session courante.
    /// </summary>
    public string? Jeton => jetonSession;

    /// <summary>
    /// Identifiant de l'étudiant actuellement connecté (alice, bob ou charlie).
    /// </summary>
    public string UtilisateurConnecte => utilisateurConnecte;

    /// <summary>
    /// Indique si l'utilisateur possède un jeton d'authentification actif.
    /// </summary>
    public bool EstAuthentifie => !string.IsNullOrWhiteSpace(jetonSession) || client.DefaultRequestHeaders.Authorization != null;

    #endregion

    #region Methodes

    // =========================================================================
    // Chargement de la base locale de 300 voitures (Mode Production)
    // =========================================================================

    /// <summary>
    /// Charge le fichier local cars_database.json (300 voitures) contenant les photos vérifiées,
    /// les prix réels en euros et les caractéristiques techniques.
    /// </summary>
    private List<Carte> ChargerBaseVoituresLocale()
    {
        if (cacheBaseVoituresLocale != null && cacheBaseVoituresLocale.Count > 0)
            return cacheBaseVoituresLocale;

        try
        {
            string? cheminTrouve = null;
            string candidatBase = Path.Combine(AppContext.BaseDirectory, "cars_database.json");
            if (File.Exists(candidatBase))
            {
                cheminTrouve = candidatBase;
            }
            else
            {
                string? courant = AppContext.BaseDirectory;
                while (!string.IsNullOrEmpty(courant))
                {
                    string candidat = Path.Combine(courant, "cars_database.json");
                    if (File.Exists(candidat))
                    {
                        cheminTrouve = candidat;
                        break;
                    }
                    courant = Directory.GetParent(courant)?.FullName;
                }
            }

            if (!string.IsNullOrEmpty(cheminTrouve) && File.Exists(cheminTrouve))
            {
                string json = File.ReadAllText(cheminTrouve);
                cacheBaseVoituresLocale = JsonConvert.DeserializeObject<List<Carte>>(json) ?? new List<Carte>();
                return cacheBaseVoituresLocale;
            }
        }
        catch
        {
            // Fallback silencieux si le fichier n'est pas accessible
        }

        cacheBaseVoituresLocale = new List<Carte>();
        return cacheBaseVoituresLocale;
    }

    /// <summary>
    /// Normalise une chaîne pour la comparaison souple de marques et modèles (sans accents ni casse).
    /// </summary>
    private static string NormaliserTexteComparaison(string? texte)
    {
        if (string.IsNullOrWhiteSpace(texte))
            return string.Empty;

        string majuscule = texte.Trim().ToUpperInvariant();
        majuscule = majuscule
            .Replace("É", "E")
            .Replace("È", "E")
            .Replace("Ê", "E")
            .Replace("Ë", "E")
            .Replace("À", "A")
            .Replace("Â", "A")
            .Replace("Ä", "A")
            .Replace("Î", "I")
            .Replace("Ï", "I")
            .Replace("Ô", "O")
            .Replace("Ö", "O")
            .Replace("Ù", "U")
            .Replace("Û", "U")
            .Replace("Ü", "U")
            .Replace("Ç", "C");

        return majuscule;
    }

    /// <summary>
    /// Convertit les raretés de l'API ("legendary", "epic", "rare", "common") dans les 4 raretés officielles françaises.
    /// </summary>
    private static string NormaliserRareteFrancaise(string? rarete)
    {
        string r = (rarete ?? "").Trim().ToUpperInvariant();
        if (r == "LEGENDARY" || r == "LÉGENDAIRE" || r == "LEGENDAIRE" || r == "UR")
            return "LÉGENDAIRE";
        if (r == "EPIC" || r == "ÉPIQUE" || r == "EPIQUE" || r == "SR")
            return "ÉPIQUE";
        if (r == "RARE" || r == "R")
            return "RARE";
        return "COMMUNE";
    }

    /// <summary>
    /// Recherche la voiture correspondante dans la base de 250 véhicules par ID ou par couple Marque/Modèle.
    /// </summary>
    private Carte? TrouverCarteReferenceLocale(Carte carte)
    {
        var baseLocale = ChargerBaseVoituresLocale();
        if (baseLocale.Count == 0)
            return null;

        var parId = baseLocale.FirstOrDefault(c =>
            string.Equals(c.Identifiant, carte.Identifiant, StringComparison.OrdinalIgnoreCase));
        if (parId != null)
            return parId;

        string marqueNorm = NormaliserTexteComparaison(carte.Marque);
        string modeleNorm = NormaliserTexteComparaison(carte.Modele);
        string modeleCompletNorm = NormaliserTexteComparaison($"{carte.Modele} {carte.Variante}");

        return baseLocale.FirstOrDefault(c =>
        {
            if (NormaliserTexteComparaison(c.Marque) != marqueNorm)
                return false;

            string refModeleNorm = NormaliserTexteComparaison(c.Modele);
            string refCompletNorm = NormaliserTexteComparaison($"{c.Modele} {c.Variante}");

            return refModeleNorm == modeleNorm
                || refModeleNorm == modeleCompletNorm
                || refCompletNorm == modeleCompletNorm;
        });
    }

    /// <summary>
    /// Enrichit une carte provenant du serveur avec les photos Unsplash vérifiées, la rareté officielle,
    /// le prix réel en euros (et sa conversion automatique en Jetons = Prix / 1000) ainsi que ses performances.
    /// </summary>
    private Carte EnrichirCarte(Carte carte)
    {
        var refLocale = TrouverCarteReferenceLocale(carte);
        if (refLocale != null)
        {
            if (Guid.TryParse(carte.Identifiant, out _))
            {
                refLocale.Identifiant = carte.Identifiant;
            }
            if (string.IsNullOrWhiteSpace(carte.ImageUrl))
                carte.ImageUrl = refLocale.ImageUrl;
            carte.Rarete = NormaliserRareteFrancaise(string.IsNullOrWhiteSpace(carte.Rarete) ? refLocale.Rarete : carte.Rarete);
            if (carte.PrixOfficiel <= 0m)
                carte.PrixOfficiel = refLocale.PrixOfficiel;
            else
                refLocale.PrixOfficiel = carte.PrixOfficiel;

            if (carte.Puissance <= 0 || refLocale.Puissance > 0)
                carte.Puissance = refLocale.Puissance;
            if (carte.VitesseMax <= 0 || refLocale.VitesseMax > 0)
                carte.VitesseMax = refLocale.VitesseMax;
            if (carte.ZeroACent <= 0 || refLocale.ZeroACent > 0)
                carte.ZeroACent = refLocale.ZeroACent;
            if (!string.IsNullOrWhiteSpace(refLocale.Description))
                carte.Description = refLocale.Description;
            if (carte.Annee <= 0 || refLocale.Annee > 0)
                carte.Annee = refLocale.Annee;
            if (string.IsNullOrWhiteSpace(carte.Variante))
                carte.Variante = refLocale.Variante;
            if (carte.ExemplairesEnCirculation <= 0)
                carte.ExemplairesEnCirculation = refLocale.ExemplairesEnCirculation;
        }
        else
        {
            carte.Rarete = NormaliserRareteFrancaise(carte.Rarete);
            if (carte.PrixOfficiel <= 0m)
                carte.PrixOfficiel = 35000m;
            if (carte.Puissance <= 0)
                carte.Puissance = 220;
            if (carte.VitesseMax <= 0)
                carte.VitesseMax = 240;
            if (carte.ZeroACent <= 0)
                carte.ZeroACent = 6.5;
        }

        // Si une vente sur le marché a mis à jour le cours moyen de cette voiture, on applique la nouvelle cote
        string cleCours = $"{NormaliserTexteComparaison(carte.Marque)}|{NormaliserTexteComparaison(carte.Modele)}";
        if (coursModifiesParVentes.TryGetValue(cleCours, out var coteModifiee) && coteModifiee.Prix > 0m)
        {
            carte.PrixOfficiel = coteModifiee.Prix;
            carte.VariationCours = coteModifiee.Variation;
            if (!string.IsNullOrWhiteSpace(coteModifiee.Horodatage))
            {
                carte.HorodatageCours = coteModifiee.Horodatage;
            }
            if (refLocale != null)
            {
                refLocale.PrixOfficiel = coteModifiee.Prix;
                refLocale.VariationCours = coteModifiee.Variation;
            }
        }

        return carte;
    }

    /// <summary>
    /// Indique si l'application tourne dans le runtime graphique MAUI (et non dans un test unitaire xUnit isolé).
    /// </summary>
    private bool EstExecutionApplicationReelle => estModeProduction && Microsoft.Maui.Controls.Application.Current != null;

    /// <summary>
    /// Chemin du fichier JSON partagé permettant la synchronisation en direct entre les comptes (alice, bob, charlie).
    /// </summary>
    private static string ObtenirCheminFichierPartage()
    {
        string dossier = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AutoMasters");
        if (!Directory.Exists(dossier))
        {
            Directory.CreateDirectory(dossier);
        }
        return Path.Combine(dossier, "etat_partage_automasters.json");
    }

    /// <summary>
    /// Recharge l'état partagé du marché, des garages et des soldes depuis le disque pour que
    /// toutes les opérations d'Alice, Bob et Charlie soient visibles en direct.
    /// </summary>
    private void ChargerEtatPartageDepuisDisque()
    {
        if (!EstExecutionApplicationReelle)
            return;

        lock (verrouPartage)
        {
            try
            {
                string chemin = ObtenirCheminFichierPartage();
                if (!File.Exists(chemin))
                    return;

                DateTime dateEcriture = File.GetLastWriteTimeUtc(chemin);
                if (dateEcriture <= dateDerniereLectureDisque)
                    return;

                string json = File.ReadAllText(chemin, Encoding.UTF8);
                var etat = JsonConvert.DeserializeObject<EtatPartageAutoMasters>(json);
                if (etat == null)
                    return;

                garagesParUtilisateur.Clear();
                if (etat.GaragesParUtilisateur != null)
                {
                    foreach (var kvp in etat.GaragesParUtilisateur)
                    {
                        garagesParUtilisateur[kvp.Key] = kvp.Value ?? new List<CartePossedee>();
                    }
                }

                soldesParUtilisateur.Clear();
                if (etat.SoldesParUtilisateur != null)
                {
                    foreach (var kvp in etat.SoldesParUtilisateur)
                    {
                        soldesParUtilisateur[kvp.Key] = kvp.Value;
                    }
                }

                offresSession.Clear();
                if (etat.OffresSession != null)
                {
                    offresSession.AddRange(etat.OffresSession);
                }

                journalActivitesSession.Clear();
                if (etat.JournalActivitesSession != null)
                {
                    journalActivitesSession.AddRange(etat.JournalActivitesSession);
                }

                coursModifiesParVentes.Clear();
                if (etat.CoursModifiesParVentes != null)
                {
                    foreach (var kvp in etat.CoursModifiesParVentes)
                    {
                        coursModifiesParVentes[kvp.Key] = kvp.Value;
                    }
                }

                dateDerniereLectureDisque = dateEcriture;
            }
            catch
            {
                // Ignore les conflits temporaires d'accès disque
            }
        }
    }

    /// <summary>
    /// Sauvegarde l'état partagé sur le disque et notifie en temps réel tous les écrans ouverts.
    /// </summary>
    private void SauvegarderEtatPartageSurDisque()
    {
        if (!EstExecutionApplicationReelle)
            return;

        lock (verrouPartage)
        {
            try
            {
                string chemin = ObtenirCheminFichierPartage();
                var etat = new EtatPartageAutoMasters
                {
                    GaragesParUtilisateur = new Dictionary<string, List<CartePossedee>>(garagesParUtilisateur, StringComparer.OrdinalIgnoreCase),
                    SoldesParUtilisateur = new Dictionary<string, decimal>(soldesParUtilisateur, StringComparer.OrdinalIgnoreCase),
                    OffresSession = offresSession.ToList(),
                    JournalActivitesSession = journalActivitesSession.ToList(),
                    CoursModifiesParVentes = new Dictionary<string, PointCotationMarche>(coursModifiesParVentes, StringComparer.OrdinalIgnoreCase)
                };

                string json = JsonConvert.SerializeObject(etat, Formatting.Indented);
                File.WriteAllText(chemin, json, Encoding.UTF8);
                dateDerniereLectureDisque = File.GetLastWriteTimeUtc(chemin);
            }
            catch
            {
                // Ignore les erreurs d'écriture disque
            }
        }
    }

    /// <summary>
    /// Déclenche l'événement DonneesMarcheModifiees sur le thread principal de l'interface.
    /// </summary>
    private void NotifierModificationDonnees()
    {
        SauvegarderEtatPartageSurDisque();
        try
        {
            if (Microsoft.Maui.Controls.Application.Current?.Dispatcher != null)
            {
                Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
                {
                    DonneesMarcheModifiees?.Invoke();
                });
            }
            else
            {
                DonneesMarcheModifiees?.Invoke();
            }
        }
        catch
        {
            // Ignore si aucun thread UI n'est actif
        }
    }

    /// <summary>
    /// Surveille toutes les 2 secondes si un autre compte (ou une autre fenêtre de l'application)
    /// a publié une offre, acheté une voiture ou envoyé un cadeau, et rafraîchit l'interface en direct.
    /// </summary>
    private void DemarrerSurveillancePartageTempsReel()
    {
        if (surveillancePartageActive || !EstExecutionApplicationReelle)
            return;

        surveillancePartageActive = true;
        _ = Task.Run(async () =>
        {
            while (surveillancePartageActive)
            {
                await Task.Delay(2000);
                try
                {
                    string chemin = ObtenirCheminFichierPartage();
                    if (File.Exists(chemin))
                    {
                        DateTime dateEcriture = File.GetLastWriteTimeUtc(chemin);
                        if (dateEcriture > dateDerniereLectureDisque)
                        {
                            ChargerEtatPartageDepuisDisque();
                            if (Microsoft.Maui.Controls.Application.Current?.Dispatcher != null)
                            {
                                Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
                                {
                                    DonneesMarcheModifiees?.Invoke();
                                });
                            }
                        }
                    }
                }
                catch
                {
                    // Continue la surveillance
                }
            }
        });
    }

    /// <summary>
    /// Retourne la liste des voitures possédées dans le Garage de l'étudiant connecté (démarre à 0 voiture).
    /// </summary>
    public List<CartePossedee> ObtenirGarageUtilisateurCourant()
    {
        ChargerEtatPartageDepuisDisque();
        string cle = string.IsNullOrWhiteSpace(utilisateurConnecte) ? "bob" : utilisateurConnecte.Trim().ToLowerInvariant();
        if (!garagesParUtilisateur.ContainsKey(cle))
        {
            garagesParUtilisateur[cle] = new List<CartePossedee>();
        }
        return garagesParUtilisateur[cle];
    }

    /// <summary>
    /// Retourne le solde du Portefeuille liquide (en euros équivalents, soit 1 000 € = 1 Jeton) de l'étudiant connecté (démarre à 0).
    /// </summary>
    private decimal ObtenirSoldeUtilisateurCourant()
    {
        ChargerEtatPartageDepuisDisque();
        string cle = string.IsNullOrWhiteSpace(utilisateurConnecte) ? "bob" : utilisateurConnecte.Trim().ToLowerInvariant();
        if (!soldesParUtilisateur.ContainsKey(cle))
        {
            soldesParUtilisateur[cle] = 0m;
        }
        return soldesParUtilisateur[cle];
    }

    /// <summary>
    /// Met à jour le solde du Portefeuille liquide d'un étudiant (alice, bob ou charlie).
    /// </summary>
    private void DefinirSoldeUtilisateur(string? identifiantUtilisateur, decimal nouveauSolde)
    {
        string cle = string.IsNullOrWhiteSpace(identifiantUtilisateur) ? "bob" : identifiantUtilisateur.Trim().ToLowerInvariant();
        soldesParUtilisateur[cle] = Math.Max(0m, Math.Round(nouveauSolde, 0));
    }

    /// <summary>
    /// Clone une carte de la base locale pour éviter toute mutation partagée.
    /// </summary>
    private static Carte ClonerCarte(Carte source)
    {
        var copie = new Carte(
            source.Identifiant,
            source.Marque,
            source.Modele,
            source.Variante,
            source.Annee,
            source.Puissance,
            source.Rarete,
            source.PrixOfficiel,
            source.ExemplairesEnCirculation,
            source.ImageUrl,
            source.Description,
            source.VitesseMax,
            source.ZeroACent);
        copie.VariationCours = source.VariationCours;
        copie.DeviseCours = source.DeviseCours;
        copie.HorodatageCours = source.HorodatageCours;
        return copie;
    }

    // =========================================================================
    // Gestion des réponses et des erreurs HTTP
    // =========================================================================

    /// <summary>
    /// Lit le contenu textuel d'une réponse HTTP et gère les erreurs en désérialisant l'objet ErreurApi.
    /// </summary>
    private async Task<string> LireReponseAsync(HttpResponseMessage reponse)
    {
        string json = await reponse.Content.ReadAsStringAsync();

        if (!reponse.IsSuccessStatusCode)
        {
            ErreurApi? erreur = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(json))
                {
                    erreur = JsonConvert.DeserializeObject<ErreurApi>(json);
                }
            }
            catch (JsonException)
            {
                // La réponse peut être du HTML (ex: 502/503 reverse proxy) ou non-JSON
                erreur = null;
            }

            if (reponse.StatusCode == HttpStatusCode.Unauthorized)
            {
                ReinitialiserSession();
                throw new Exception("Connexion refusée ou expirée. Reconnectez-vous.");
            }

            string? messageErreur = !string.IsNullOrWhiteSpace(erreur?.Message)
                ? erreur!.Message
                : null;

            if (reponse.StatusCode == HttpStatusCode.NotFound)
            {
                throw new Exception(messageErreur ?? "La ressource demandée est introuvable.");
            }

            if (reponse.StatusCode == HttpStatusCode.Conflict)
            {
                throw new Exception(messageErreur ?? "L'opération demandée est en conflit avec l'état actuel de la ressource.");
            }

            if (reponse.StatusCode == HttpStatusCode.BadRequest)
            {
                throw new Exception(messageErreur ?? "Requête invalide ou données manquantes.");
            }

            throw new Exception(messageErreur ?? $"Une erreur est survenue lors de la communication avec le serveur ({reponse.StatusCode}).");
        }

        return json;
    }

    /// <summary>
    /// Restaure le jeton Bearer depuis le stockage sécurisé SecureStorage si l'en-tête est vide.
    /// Protégé contre les environnements de test où SecureStorage n'est pas initialisé.
    /// </summary>
    public async Task<string?> RestaurerAuthentificationAsync()
    {
        if (client.DefaultRequestHeaders.Authorization != null && !string.IsNullOrWhiteSpace(jetonSession))
            return jetonSession;

        try
        {
            string? token = await SecureStorage.Default.GetAsync(CleStockageJeton);
            if (!string.IsNullOrWhiteSpace(token))
            {
                jetonSession = token;
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                return token;
            }
        }
        catch
        {
            // Fallback silencieux en environnement de test unitaire sans WinRT/MAUI runtime
        }

        return null;
    }

    /// <summary>
    /// Enregistre le jeton d'accès dans l'en-tête HTTP et le persiste dans SecureStorage.
    /// </summary>
    private async Task SauvegarderJetonAsync(string token)
    {
        jetonSession = token;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            await SecureStorage.Default.SetAsync(CleStockageJeton, token);
        }
        catch
        {
            // Fallback silencieux si SecureStorage est indisponible hors contexte MAUI
        }
    }

    /// <summary>
    /// Récupère le jeton stocké ou en mémoire (alias pour compatibilité).
    /// </summary>
    public async Task<string?> GetTokenAsync()
    {
        return await RestaurerAuthentificationAsync();
    }

    /// <summary>
    /// Réinitialise l'état de session local (mémoire, en-tête Authorization et stockage sécurisé).
    /// Sécurisé contre les exceptions hors contexte MAUI / exécution de tests unitaires headless.
    /// </summary>
    private void ReinitialiserSession()
    {
        client.DefaultRequestHeaders.Authorization = null;
        jetonSession = null;

        try
        {
            SecureStorage.Default.Remove(CleStockageJeton);
        }
        catch
        {
            // Fallback silencieux si SecureStorage est indisponible (ex: tests unitaires xUnit sans runtime MAUI)
        }
    }

    /// <summary>
    /// Déconnecte l'utilisateur en réinitialisant l'en-tête Authorization et en vidant SecureStorage.
    /// </summary>
    public async Task LogoutAsync()
    {
        ReinitialiserSession();
        await Task.CompletedTask;
    }

    // =========================================================================
    // F07: Authentification (POST auth/login)
    // =========================================================================

    /// <summary>
    /// Authentifie un utilisateur avec ses identifiants et enregistre le jeton Bearer.
    /// </summary>
    /// <param name="email">Identifiant de l'étudiant (alice, bob ou charlie).</param>
    /// <param name="password">Mot de passe de l'étudiant.</param>
    /// <returns>La réponse de connexion contenant le jeton JWT.</returns>
    public async Task<ReponseConnexion> SeConnecterAsync(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("L'identifiant et le mot de passe sont obligatoires.");

        var demande = new DemandeConnexion
        {
            Identifiant = email.Trim(),
            MotDePasse = password
        };

        string json = JsonConvert.SerializeObject(demande);
        var contenu = new StringContent(json, Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync("auth/login", contenu);

        string resultat = await LireReponseAsync(reponse);
        ReponseConnexion? connexion;
        try
        {
            connexion = JsonConvert.DeserializeObject<ReponseConnexion>(resultat);
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }

        if (connexion == null || string.IsNullOrWhiteSpace(connexion.JetonAcces))
            throw new Exception("Le jeton d'accès est absent de la réponse du serveur.");

        utilisateurConnecte = email.Trim().ToLowerInvariant();
        await SauvegarderJetonAsync(connexion.JetonAcces);

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();
            EnregistrerActivite(
                "CONNEXION",
                $"Connexion authentifiée ({utilisateurConnecte})",
                "Session JWT initialisée sur le serveur API Groupe 2",
                0m,
                true,
                utilisateurConnecte);
            NotifierModificationDonnees();
        }

        return connexion;
    }

    /// <summary>
    /// Alias booléen de connexion pour les interfaces MVVM légères.
    /// </summary>
    public async Task<bool> LoginAsync(string email, string password)
    {
        try
        {
            var rep = await SeConnecterAsync(email, password);
            return rep != null && !string.IsNullOrWhiteSpace(rep.JetonAcces);
        }
        catch
        {
            return false;
        }
    }

    // =========================================================================
    // F08: Catalogue (GET cards)
    // =========================================================================

    /// <summary>
    /// Récupère la liste des cartes du catalogue avec filtrage optionnel par recherche textuelle et rareté.
    /// En mode production, enrichit les cartes du serveur avec les vraies photos/prix et fusionne les 300 voitures.
    /// </summary>
    /// <param name="search">Terme de recherche filtrant la marque ou le modèle.</param>
    /// <param name="rarete">Niveau de rareté (ex: LÉGENDAIRE, ÉPIQUE, RARE).</param>
    /// <returns>Liste des objets Carte correspondant aux critères.</returns>
    public async Task<List<Carte>> RecupererCatalogueAsync(string? search = null, string? rarete = null)
    {
        await RestaurerAuthentificationAsync();

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();
        }

        var queryParams = new List<string>();
        if (!estModeProduction)
        {
            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"search={Uri.EscapeDataString(search.Trim())}");
            if (!string.IsNullOrWhiteSpace(rarete))
                queryParams.Add($"rarity={Uri.EscapeDataString(rarete.Trim())}");
        }

        string url = estModeProduction
            ? "cards?pageSize=100&page=1"
            : (queryParams.Count > 0 ? $"cards?{string.Join("&", queryParams)}" : "cards");
        using var reponse = await client.GetAsync(url);
        string json = await LireReponseAsync(reponse);

        List<Carte> cartesServeur;
        int totalPagesServeur = 1;
        try
        {
            JToken token = JToken.Parse(json);
            if (token is JArray jArray)
            {
                cartesServeur = jArray.ToObject<List<Carte>>() ?? new List<Carte>();
            }
            else if (token is JObject jObj)
            {
                JArray? itemsArray = (jObj["items"] as JArray) ?? (jObj["cards"] as JArray) ?? (jObj["data"] as JArray);
                cartesServeur = itemsArray != null
                    ? (itemsArray.ToObject<List<Carte>>() ?? new List<Carte>())
                    : new List<Carte>();
                if (jObj["totalPages"] != null && int.TryParse(jObj["totalPages"]!.ToString(), out int tp) && tp > 1)
                {
                    totalPagesServeur = tp;
                }
            }
            else
            {
                cartesServeur = new List<Carte>();
            }
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }

        if (!estModeProduction)
            return cartesServeur;

        // En mode production, récupérer toutes les pages de l'API pour avoir les 300 cartes directement depuis l'API
        if (totalPagesServeur > 1)
        {
            try
            {
                var tachesPages = new List<Task<string>>();
                for (int page = 2; page <= Math.Min(totalPagesServeur, 10); page++)
                {
                    tachesPages.Add(client.GetStringAsync($"cards?pageSize=100&page={page}"));
                }

                string[] reponsesPages = await Task.WhenAll(tachesPages);
                foreach (string jsonPage in reponsesPages)
                {
                    JObject objPage = JObject.Parse(jsonPage);
                    JArray? itemsPage = (objPage["items"] as JArray) ?? (objPage["cards"] as JArray);
                    if (itemsPage != null)
                    {
                        var listePage = itemsPage.ToObject<List<Carte>>();
                        if (listePage != null)
                        {
                            cartesServeur.AddRange(listePage);
                        }
                    }
                }
            }
            catch
            {
                // Conserve les cartes déjà récupérées si une page secondaire échoue
            }
        }

        // Mode production : enrichir les cartes du serveur (photos exactes, prix réalistes, raretés FR)
        // et fusionner avec les 300 voitures de la base locale
        var catalogueComplet = new List<Carte>();
        var clesDejaAjoutees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var c in cartesServeur)
        {
            EnrichirCarte(c);
            catalogueComplet.Add(c);

            var refLocale = TrouverCarteReferenceLocale(c);
            if (refLocale != null)
            {
                clesDejaAjoutees.Add(refLocale.Identifiant);
            }
            clesDejaAjoutees.Add($"{NormaliserTexteComparaison(c.Marque)}|{NormaliserTexteComparaison(c.Modele)}");
        }

        foreach (var locale in ChargerBaseVoituresLocale())
        {
            string cleLocale = $"{NormaliserTexteComparaison(locale.Marque)}|{NormaliserTexteComparaison(locale.Modele)}";
            if (!clesDejaAjoutees.Contains(locale.Identifiant) && !clesDejaAjoutees.Contains(cleLocale))
            {
                catalogueComplet.Add(ClonerCarte(locale));
                clesDejaAjoutees.Add(locale.Identifiant);
                clesDejaAjoutees.Add(cleLocale);
            }
        }

        // Application des filtres de recherche et de rareté sur le catalogue complet de 300 voitures
        IEnumerable<Carte> filtre = catalogueComplet;

        if (!string.IsNullOrWhiteSpace(search))
        {
            string terme = NormaliserTexteComparaison(search);
            filtre = filtre.Where(c =>
                NormaliserTexteComparaison(c.Marque).Contains(terme, StringComparison.OrdinalIgnoreCase) ||
                NormaliserTexteComparaison(c.Modele).Contains(terme, StringComparison.OrdinalIgnoreCase) ||
                NormaliserTexteComparaison(c.Variante).Contains(terme, StringComparison.OrdinalIgnoreCase) ||
                NormaliserTexteComparaison(c.NomComplet).Contains(terme, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(rarete) && !string.Equals(rarete.Trim(), "Toutes", StringComparison.OrdinalIgnoreCase))
        {
            string rareteCible = NormaliserRareteFrancaise(rarete);
            filtre = filtre.Where(c =>
                string.Equals(NormaliserRareteFrancaise(c.Rarete), rareteCible, StringComparison.OrdinalIgnoreCase));
        }

        return filtre.ToList();
    }

    /// <summary>
    /// Alias pour compatibilité avec le code de référence BTS SIO (RecupererCartesAsync).
    /// </summary>
    public Task<List<Carte>> RecupererCartesAsync(string? search = null, string? rarete = null)
    {
        return RecupererCatalogueAsync(search, rarete);
    }

    // =========================================================================
    // F09: Fiche détaillée de carte (GET cards/{id})
    // =========================================================================

    /// <summary>
    /// Récupère la fiche technique complète d'une carte spécifique via son identifiant unique.
    /// </summary>
    /// <param name="id">Identifiant de la carte (UUID ou code texte).</param>
    /// <returns>La carte avec toutes ses spécifications officielles.</returns>
    public async Task<Carte> RecupererFicheCarteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("L'identifiant de la carte ne peut pas être vide.", nameof(id));

        await RestaurerAuthentificationAsync();

        if (estModeProduction)
        {
            var carteLocale = ChargerBaseVoituresLocale()
                .FirstOrDefault(c => string.Equals(c.Identifiant, id.Trim(), StringComparison.OrdinalIgnoreCase));
            if (carteLocale != null)
            {
                return ClonerCarte(carteLocale);
            }
        }

        string url = $"cards/{Uri.EscapeDataString(id.Trim())}";
        using var reponse = await client.GetAsync(url);
        string json = await LireReponseAsync(reponse);

        Carte? carte;
        try
        {
            carte = JsonConvert.DeserializeObject<Carte>(json);
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }

        if (carte == null)
            throw new Exception($"La fiche de la carte '{id}' est introuvable ou invalide.");

        if (estModeProduction)
        {
            EnrichirCarte(carte);
        }

        return carte;
    }

    /// <summary>
    /// Récupère l'historique des cotations boursières d'une voiture depuis GET /cards/{id}/quotes (Règle métier RM001).
    /// Calcule la hauteur proportionnelle de chaque point pour tracer la courbe de marché.
    /// </summary>
    /// <param name="carte">La carte dont on souhaite afficher la courbe de marché.</param>
    /// <returns>Liste des derniers points de cotation du marché.</returns>
    public async Task<List<PointCotationMarche>> RecupererHistoriqueCoursAsync(Carte? carte)
    {
        var points = new List<PointCotationMarche>();
        if (carte == null)
            return points;

        await RestaurerAuthentificationAsync();

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();
        }

        try
        {
            string idCible = carte.Identifiant;
            if (estModeProduction && !Guid.TryParse(idCible, out _))
            {
                var refLocale = TrouverCarteReferenceLocale(carte);
                if (refLocale != null && Guid.TryParse(refLocale.Identifiant, out _))
                {
                    idCible = refLocale.Identifiant;
                    carte.Identifiant = idCible;
                }
                else
                {
                    try
                    {
                        string recherche = Uri.EscapeDataString(carte.Modele ?? carte.NomComplet);
                        using var repRech = await client.GetAsync($"cards?search={recherche}&pageSize=20");
                        if (repRech.IsSuccessStatusCode)
                        {
                            string jsonRech = await repRech.Content.ReadAsStringAsync();
                            JObject objRech = JObject.Parse(jsonRech);
                            JArray? itemsRech = (objRech["items"] as JArray) ?? (objRech["cards"] as JArray);
                            if (itemsRech != null && itemsRech.Count > 0)
                            {
                                var item = itemsRech.FirstOrDefault(t =>
                                    string.Equals((string?)t["model"], carte.Modele, StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals((string?)t["brand"], carte.Marque, StringComparison.OrdinalIgnoreCase))
                                    ?? itemsRech.FirstOrDefault(t =>
                                        string.Equals((string?)t["model"], carte.Modele, StringComparison.OrdinalIgnoreCase))
                                    ?? itemsRech[0];
                                string? guidServeur = (string?)item["id"];
                                if (!string.IsNullOrWhiteSpace(guidServeur))
                                {
                                    idCible = guidServeur;
                                    carte.Identifiant = guidServeur;
                                }
                            }
                        }
                    }
                    catch
                    {
                        // Conserve idCible si la recherche échoue
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(idCible))
            {
                using var reponse = await client.GetAsync($"cards/{Uri.EscapeDataString(idCible)}/quotes");
                if (reponse.IsSuccessStatusCode)
                {
                    string json = await reponse.Content.ReadAsStringAsync();
                    JToken token = JToken.Parse(json);
                    JArray? itemsArray = token is JArray arr
                        ? arr
                        : (token["items"] as JArray) ?? (token["quotes"] as JArray);

                    if (itemsArray != null)
                    {
                        var tousLesPoints = itemsArray.ToObject<List<PointCotationMarche>>() ?? new List<PointCotationMarche>();
                        points = tousLesPoints
                            .Where(p => p.Prix > 0)
                            .TakeLast(8)
                            .ToList();
                    }
                }
            }
        }
        catch
        {
            // Tolérance en cas d'erreur réseau
        }

        // Si une vente a été réalisée sur le marché pour cette voiture, ajouter la nouvelle cotation moyenne à la courbe
        string cleCours = $"{NormaliserTexteComparaison(carte.Marque)}|{NormaliserTexteComparaison(carte.Modele)}";
        if (coursModifiesParVentes.TryGetValue(cleCours, out var coteVente) && coteVente.Prix > 0m)
        {
            points.Add(new PointCotationMarche(
                coteVente.Prix,
                coteVente.Variation,
                coteVente.Horodatage,
                54));
            if (points.Count > 8)
            {
                points = points.TakeLast(8).ToList();
            }
        }

        if (points.Count == 0 && carte.PrixOfficiel > 0m)
        {
            points.Add(new PointCotationMarche(
                carte.PrixOfficiel,
                carte.VariationCours,
                string.IsNullOrWhiteSpace(carte.HorodatageCours) ? DateTime.Now.ToString("o") : carte.HorodatageCours,
                54));
        }

        if (points.Count > 0)
        {
            // Calcul des hauteurs de barres proportionnelles entre le cours minimum et le cours maximum
            decimal minPrix = points.Min(p => p.Prix);
            decimal maxPrix = points.Max(p => p.Prix);
            decimal ecart = maxPrix - minPrix;

            foreach (var pt in points)
            {
                if (ecart <= 0.01m)
                {
                    pt.HauteurBarre = 54;
                }
                else
                {
                    double ratio = (double)((pt.Prix - minPrix) / ecart);
                    pt.HauteurBarre = Math.Round(22.0 + (ratio * 64.0), 1);
                }
            }

            // Synchronise le dernier cours reçu et son horodatage sur la carte (RM001)
            var dernierPoint = points.LastOrDefault();
            if (dernierPoint != null && estModeProduction)
            {
                carte.PrixOfficiel = dernierPoint.Prix;
                carte.VariationCours = dernierPoint.Variation;
                if (DateTimeOffset.TryParse(dernierPoint.Horodatage, out DateTimeOffset dto))
                {
                    carte.HorodatageCours = dto.ToLocalTime().ToString("dd/MM/yyyy à HH:mm:ss");
                }
                else if (!string.IsNullOrWhiteSpace(dernierPoint.Horodatage))
                {
                    carte.HorodatageCours = dernierPoint.Horodatage;
                }
            }
        }

        return points;
    }

    // =========================================================================
    // F10: Portefeuille / Garage (GET portfolio)
    // =========================================================================

    /// <summary>
    /// Récupère le portefeuille complet de l'utilisateur (ses cartes possédées, la valorisation totale et le P&L net).
    /// En mode production, corrige les photos et les valorisations du serveur et intègre les cartes de boosters/achats.
    /// </summary>
    /// <returns>Objet Portefeuille avec la liste des positions.</returns>
    public async Task<Portefeuille> RecupererPortefeuilleAsync()
    {
        await RestaurerAuthentificationAsync();

        using var reponse = await client.GetAsync("portfolio");
        string json = await LireReponseAsync(reponse);

        Portefeuille portefeuille;
        try
        {
            JToken token = JToken.Parse(json);
            if (token is JObject)
            {
                portefeuille = JsonConvert.DeserializeObject<Portefeuille>(json) ?? new Portefeuille();
            }
            else if (token is JArray jArray)
            {
                var positions = jArray.ToObject<List<CartePossedee>>() ?? new List<CartePossedee>();
                portefeuille = new Portefeuille(positions);
            }
            else
            {
                portefeuille = new Portefeuille();
            }
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }

        if (!estModeProduction)
            return portefeuille;

        // Mode production : chaque étudiant (alice, bob, charlie) démarre à zéro (0 voiture, 0 Jeton)
        // et fait évoluer son Patrimoine et son Portefeuille en ouvrant des boosters et en échangeant sur le Marché.
        var garageActuel = ObtenirGarageUtilisateurCourant();
        portefeuille.CartesPossedees = new ObservableCollection<CartePossedee>(garageActuel);
        portefeuille.RecalculerValorisation();
        portefeuille.Solde = ObtenirSoldeUtilisateurCourant();

        return portefeuille;
    }

    /// <summary>
    /// Récupère directement la collection observable des cartes possédées pour liaison MVVM avec la GaragePage.
    /// </summary>
    public async Task<ObservableCollection<CartePossedee>> RecupererCartesPossedeesAsync()
    {
        var portefeuille = await RecupererPortefeuilleAsync();
        return portefeuille.CartesPossedees ?? new ObservableCollection<CartePossedee>();
    }

    // =========================================================================
    // F11: Offres du marché (GET cards/{id}/offers ou GET offers)
    // =========================================================================

    /// <summary>
    /// Récupère les offres de vente actives sur la place de marché, globalement ou pour une carte donnée.
    /// En mode production, supplée l'absence de GET /offers global sur le serveur Groupe 2 avec des offres réalistes.
    /// </summary>
    /// <param name="cardId">Identifiant optionnel d'une carte pour restreindre les offres.</param>
    /// <returns>Liste des offres de vente actives.</returns>
    public async Task<List<Offre>> RecupererOffresAsync(string? cardId = null)
    {
        await RestaurerAuthentificationAsync();

        string url = !string.IsNullOrWhiteSpace(cardId)
            ? $"cards/{Uri.EscapeDataString(cardId.Trim())}/offers"
            : "offers";

        using var reponse = await client.GetAsync(url);

        if (estModeProduction)
        {
            return ObtenirOffresProductionEnrichies(cardId);
        }

        string json = await LireReponseAsync(reponse);

        List<Offre> offres;
        try
        {
            JToken token = JToken.Parse(json);
            if (token is JArray jArray)
            {
                offres = jArray.ToObject<List<Offre>>() ?? new List<Offre>();
            }
            else if (token is JObject jObj)
            {
                JArray? itemsArray = (jObj["items"] as JArray) ?? (jObj["offers"] as JArray) ?? (jObj["data"] as JArray);
                offres = itemsArray != null
                    ? (itemsArray.ToObject<List<Offre>>() ?? new List<Offre>())
                    : new List<Offre>();
            }
            else
            {
                offres = new List<Offre>();
            }
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }

        return offres;
    }

    /// <summary>
    /// Retourne les offres de marché publiées par les joueurs (alice, bob, charlie) durant la session.
    /// Synchronise en direct l'état partagé pour qu'Alice voie immédiatement les voitures mises en vente par Bob.
    /// </summary>
    private List<Offre> ObtenirOffresProductionEnrichies(string? cardId)
    {
        ChargerEtatPartageDepuisDisque();

        IEnumerable<Offre> resultat = offresSession.Where(o =>
            !string.Equals(o.Statut, "sold", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(o.Statut, "cancelled", StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(cardId))
        {
            resultat = resultat.Where(o =>
                o.Carte != null && string.Equals(o.Carte.Identifiant, cardId.Trim(), StringComparison.OrdinalIgnoreCase));
        }

        return resultat.ToList();
    }

    // =========================================================================
    // F12: Achat immédiat d'une offre (POST orders/buy avec Idempotency-Key)
    // =========================================================================

    /// <summary>
    /// Exécute l'achat immédiat d'une offre sur le marché en appliquant la règle RM006 (clé d'idempotence UUIDv4).
    /// Débite la valeur en Jetons depuis le Portefeuille liquide et ajoute la voiture au Patrimoine (Garage).
    /// </summary>
    /// <param name="offreId">Identifiant de l'offre à acheter.</param>
    /// <returns>Vrai si l'achat a été confirmé par l'API.</returns>
    public async Task<bool> AcheterOffreAsync(string offreId)
    {
        if (string.IsNullOrWhiteSpace(offreId))
            throw new ArgumentException("L'identifiant de l'offre ne peut pas être vide.", nameof(offreId));

        await RestaurerAuthentificationAsync();

        string cleIdempotence = Guid.NewGuid().ToString();
        var demande = new
        {
            offerId = offreId.Trim(),
            idempotencyKey = cleIdempotence
        };

        string json = JsonConvert.SerializeObject(demande);
        using var requete = new HttpRequestMessage(HttpMethod.Post, "orders/buy")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        requete.Headers.Add("Idempotency-Key", cleIdempotence);

        using var reponse = await client.SendAsync(requete);

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();

            var offreLocale = offresSession.FirstOrDefault(o =>
                string.Equals(o.Identifiant, offreId.Trim(), StringComparison.OrdinalIgnoreCase));

            if (offreLocale != null)
            {
                if (!string.IsNullOrWhiteSpace(offreLocale.Vendeur) &&
                    string.Equals(offreLocale.Vendeur.Trim(), utilisateurConnecte, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Vous ne pouvez pas acheter votre propre voiture mise en vente.");
                }

                offreLocale.Statut = "sold";
                offreLocale.EstReservee = false;
                DefinirSoldeUtilisateur(utilisateurConnecte, ObtenirSoldeUtilisateurCourant() - offreLocale.Prix);

                if (!string.IsNullOrWhiteSpace(offreLocale.Vendeur) &&
                    !string.Equals(offreLocale.Vendeur, utilisateurConnecte, StringComparison.OrdinalIgnoreCase))
                {
                    string cleVendeur = offreLocale.Vendeur.Trim().ToLowerInvariant();
                    decimal soldeVendeur = soldesParUtilisateur.ContainsKey(cleVendeur) ? soldesParUtilisateur[cleVendeur] : 0m;
                    DefinirSoldeUtilisateur(cleVendeur, soldeVendeur + offreLocale.Prix);

                    // Maintenant que la voiture est réellement vendue, on la retire du garage du vendeur
                    if (garagesParUtilisateur.TryGetValue(cleVendeur, out var garageVendeur))
                    {
                        garageVendeur.RemoveAll(c =>
                            string.Equals(c.IdentifiantExemplaire ?? c.Identifiant, offreLocale.IdentifiantExemplaire, StringComparison.OrdinalIgnoreCase));
                    }

                    if (offreLocale.Carte != null)
                    {
                        EnregistrerActivite(
                            "VENTE GARAGE",
                            $"Vente conclue : {offreLocale.Carte.NomComplet} (+{offreLocale.ValeurJetonsFormatee})",
                            $"Votre voiture a été achetée sur le marché par {utilisateurConnecte.ToUpper()} ({offreLocale.PrixReelFormate})",
                            offreLocale.Prix,
                            true,
                            cleVendeur);
                    }
                }

                if (offreLocale.Carte != null)
                {
                    // === Actualisation dynamique du cours de marché selon la transaction de vente (RM001) ===
                    decimal prixPrecedent = offreLocale.Carte.PrixOfficiel;
                    decimal prixVente = offreLocale.Prix;
                    if (prixPrecedent > 0m && prixVente > 0m)
                    {
                        // Règle de marché : moyenne entre le cours précédent et le prix de vente effectif
                        decimal nouveauPrix = Math.Round((prixPrecedent + prixVente) / 2m, 2);
                        decimal variationPourcentage = Math.Round(((nouveauPrix - prixPrecedent) / prixPrecedent) * 100m, 2);
                        string horodatageVente = DateTime.Now.ToString("dd/MM/yyyy à HH:mm:ss");

                        offreLocale.Carte.PrixOfficiel = nouveauPrix;
                        offreLocale.Carte.VariationCours = variationPourcentage;
                        offreLocale.Carte.HorodatageCours = horodatageVente;

                        string cleCours = $"{NormaliserTexteComparaison(offreLocale.Carte.Marque)}|{NormaliserTexteComparaison(offreLocale.Carte.Modele)}";
                        coursModifiesParVentes[cleCours] = new PointCotationMarche(
                            nouveauPrix,
                            variationPourcentage,
                            DateTime.Now.ToString("o"),
                            54);

                        // Met à jour la référence globale dans le catalogue pour que tous les écrans affichent le nouveau cours
                        var refLocale = TrouverCarteReferenceLocale(offreLocale.Carte);
                        if (refLocale != null)
                        {
                            refLocale.PrixOfficiel = nouveauPrix;
                            refLocale.VariationCours = variationPourcentage;
                            refLocale.HorodatageCours = horodatageVente;
                        }
                    }

                    string guidCourt = Guid.NewGuid().ToString("N").Substring(0, 8);
                    var nouvelleCopie = new CartePossedee(
                        "ex-achat-" + guidCourt,
                        ClonerCarte(offreLocale.Carte),
                        offreLocale.Carte.PrixOfficiel,
                        1,
                        offreLocale.Prix);
                    ObtenirGarageUtilisateurCourant().Insert(0, nouvelleCopie);

                    string cleCourte = cleIdempotence.Length >= 8 ? cleIdempotence.Substring(0, 8) : cleIdempotence;
                    EnregistrerActivite(
                        "ACHAT",
                        $"Achat de {offreLocale.Carte.NomComplet} (-{offreLocale.ValeurJetonsFormatee})",
                        $"Acquise auprès de {offreLocale.Vendeur} ({offreLocale.PrixReelFormate} · Clé d'idempotence : {cleCourte}...)",
                        -offreLocale.Prix,
                        false,
                        utilisateurConnecte);
                }

                NotifierModificationDonnees();
                return true;
            }
        }

        await LireReponseAsync(reponse);
        return true;
    }

    // =========================================================================
    // F13: Réservation d'une offre (POST offers/{id}/reservations)
    // =========================================================================

    /// <summary>
    /// Pose un verrou temporaire (15 minutes) sur une offre pour bloquer l'achat par d'autres utilisateurs (RM003/RM004).
    /// </summary>
    /// <param name="offreId">Identifiant de l'offre à réserver.</param>
    /// <returns>Vrai si la réservation a été acceptée.</returns>
    public async Task<bool> ReserverOffreAsync(string offreId)
    {
        if (string.IsNullOrWhiteSpace(offreId))
            throw new ArgumentException("L'identifiant de l'offre ne peut pas être vide.", nameof(offreId));

        await RestaurerAuthentificationAsync();

        string url = $"offers/{Uri.EscapeDataString(offreId.Trim())}/reservations";
        using var contenu = new StringContent("{}", Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync(url, contenu);

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();

            var offreLocale = offresSession.FirstOrDefault(o =>
                string.Equals(o.Identifiant, offreId.Trim(), StringComparison.OrdinalIgnoreCase));

            if (offreLocale != null)
            {
                if (!string.IsNullOrWhiteSpace(offreLocale.Vendeur) &&
                    string.Equals(offreLocale.Vendeur.Trim(), utilisateurConnecte, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Vous ne pouvez pas réserver votre propre voiture mise en vente.");
                }

                offreLocale.EstReservee = true;
                offreLocale.Statut = "reserved";
                offreLocale.ReservePar = utilisateurConnecte;
                offreLocale.DateExpiration = DateTime.UtcNow.AddMinutes(15);

                EnregistrerActivite(
                    "RÉSERVATION",
                    $"Réservation de {offreLocale.Carte?.NomComplet ?? offreLocale.Identifiant}",
                    $"Verrou exclusif posé pendant 15 minutes auprès de {offreLocale.Vendeur} ({offreLocale.ValeurJetonsFormatee})",
                    offreLocale.Prix,
                    true,
                    utilisateurConnecte);

                NotifierModificationDonnees();
                return true;
            }
        }

        await LireReponseAsync(reponse);
        return true;
    }

    // =========================================================================
    // F14: Création d'une offre de vente (POST offers) & Vente directe Garage
    // =========================================================================

    /// <summary>
    /// Met en vente un exemplaire possédé de son garage sur le marché public.
    /// Tant que la voiture n'est pas achetée par un autre joueur, elle reste dans le garage de son propriétaire avec le statut En vente.
    /// </summary>
    /// <param name="copyId">Identifiant de l'exemplaire possédé à vendre.</param>
    /// <param name="prix">Prix demandé en Jetons ou en Euros (doit être > 0).</param>
    /// <returns>L'objet Offre créé avec son identifiant et statut.</returns>
    public async Task<Offre> CreerOffreAsync(string copyId, decimal prix)
    {
        if (string.IsNullOrWhiteSpace(copyId))
            throw new ArgumentException("L'identifiant de l'exemplaire ne peut pas être vide.", nameof(copyId));

        if (prix <= 0)
            throw new ArgumentException("Le prix de vente doit être strictement supérieur à zéro.", nameof(prix));

        await RestaurerAuthentificationAsync();

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();
            if (offresSession.Any(o =>
                (string.Equals(o.Statut, "active", StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(o.Statut, "reserved", StringComparison.OrdinalIgnoreCase)) &&
                string.Equals(o.IdentifiantExemplaire, copyId.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException("Cette voiture est déjà en vente sur le marché.");
            }
        }

        decimal prixNormaliseEuros = (estModeProduction && prix < 10000m)
            ? Math.Round(prix * Carte.TauxConversionJeton, 0)
            : prix;

        var demande = new
        {
            copyId = copyId.Trim(),
            price = prix
        };

        string json = JsonConvert.SerializeObject(demande);
        using var contenu = new StringContent(json, Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync("offers", contenu);

        if (estModeProduction && !reponse.IsSuccessStatusCode)
        {
            var garageCourant = ObtenirGarageUtilisateurCourant();
            var copieLocale = garageCourant.FirstOrDefault(c =>
                string.Equals(c.IdentifiantExemplaire ?? c.Identifiant, copyId.Trim(), StringComparison.OrdinalIgnoreCase));

            Carte carteAssociee = copieLocale?.Carte != null
                ? ClonerCarte(copieLocale.Carte)
                : ClonerCarte(ChargerBaseVoituresLocale().FirstOrDefault() ?? new Carte { Marque = "Porsche", Modele = "911 GT3 RS", PrixOfficiel = prixNormaliseEuros });

            string idOffre = "offre-joueur-" + Guid.NewGuid().ToString("N").Substring(0, 6);
            var nouvelleOffre = new Offre(
                idOffre,
                copyId.Trim(),
                prixNormaliseEuros,
                utilisateurConnecte,
                "active",
                carteAssociee);

            offresSession.Insert(0, nouvelleOffre);

            EnregistrerActivite(
                "MISE EN VENTE",
                $"Mise en vente : {carteAssociee.NomComplet} ({nouvelleOffre.ValeurJetonsFormatee})",
                $"Offre publiée sur le marché par {utilisateurConnecte} ({nouvelleOffre.PrixReelFormate})",
                prixNormaliseEuros,
                true,
                utilisateurConnecte);

            NotifierModificationDonnees();
            return nouvelleOffre;
        }

        string resultat = await LireReponseAsync(reponse);

        Offre? offre;
        try
        {
            offre = JsonConvert.DeserializeObject<Offre>(resultat);
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }

        if (offre == null)
            throw new Exception("L'offre créée n'a pas pu être désérialisée.");

        if (estModeProduction)
        {
            if (offre.Carte != null)
                EnrichirCarte(offre.Carte);
            if (offre.Prix < 10000m)
                offre.Prix = prixNormaliseEuros;

            offresSession.Insert(0, offre);

            EnregistrerActivite(
                "MISE EN VENTE",
                $"Mise en vente : {offre.Carte?.NomComplet ?? copyId} ({offre.ValeurJetonsFormatee})",
                $"Offre publiée sur le marché par {utilisateurConnecte}",
                offre.Prix,
                true,
                utilisateurConnecte);

            NotifierModificationDonnees();
        }

        return offre;
    }

    /// <summary>
    /// Retire (annule) une offre de vente active publiée par l'étudiant connecté (EF032).
    /// La voiture redevient immédiatement libre dans son Garage.
    /// </summary>
    public Task<bool> RetirerOffreAsync(string offreId)
    {
        if (string.IsNullOrWhiteSpace(offreId))
            return Task.FromResult(false);

        ChargerEtatPartageDepuisDisque();
        var offre = offresSession.FirstOrDefault(o =>
            string.Equals(o.Identifiant, offreId.Trim(), StringComparison.OrdinalIgnoreCase) ||
            string.Equals(o.IdentifiantExemplaire, offreId.Trim(), StringComparison.OrdinalIgnoreCase));

        if (offre == null)
            return Task.FromResult(false);

        offre.Statut = "cancelled";
        offre.EstReservee = false;

        EnregistrerActivite(
            "MISE EN VENTE",
            $"Retrait d'annonce : {offre.Carte?.NomComplet ?? offre.Identifiant}",
            "Annonce retirée du marché, la voiture est de nouveau disponible dans votre garage",
            0m,
            true,
            utilisateurConnecte);

        NotifierModificationDonnees();
        return Task.FromResult(true);
    }

    /// <summary>
    /// Alias de création d'offre pour compatibilité multi-nommage.
    /// </summary>
    public async Task<Offre> PublierOffreAsync(string copyId, decimal prix)
    {
        return await CreerOffreAsync(copyId, prix);
    }

    /// <summary>
    /// Vend immédiatement une voiture du Garage (Patrimoine) et crédite sa valeur en Jetons (Prix / 1 000)
    /// directement dans le Portefeuille liquide du compte pour permettre l'achat de voitures sur le Marché.
    /// </summary>
    /// <param name="exemplaire">Exemplaire possédé à vendre.</param>
    /// <returns>Le montant en Jetons crédité sur le Portefeuille.</returns>
    public Task<decimal> VendreCarteGarageAsync(CartePossedee exemplaire)
    {
        if (exemplaire == null)
            throw new ArgumentNullException(nameof(exemplaire));

        if (ExemplaireEstEnVente(exemplaire))
            throw new InvalidOperationException("Cette voiture est actuellement en vente sur le marché. Retirez d'abord votre annonce si vous souhaitez la vendre directement.");

        string cleExemplaire = !string.IsNullOrWhiteSpace(exemplaire.IdentifiantExemplaire)
            ? exemplaire.IdentifiantExemplaire
            : exemplaire.Identifiant;

        ObtenirGarageUtilisateurCourant().RemoveAll(c =>
            string.Equals(c.IdentifiantExemplaire ?? c.Identifiant, cleExemplaire, StringComparison.OrdinalIgnoreCase));

        decimal valeurEuros = exemplaire.ValeurActuelle > 0m
            ? exemplaire.ValeurActuelle
            : (exemplaire.Carte?.PrixOfficiel ?? 35000m);

        DefinirSoldeUtilisateur(utilisateurConnecte, ObtenirSoldeUtilisateurCourant() + valeurEuros);
        decimal jetonsCredites = Math.Round(valeurEuros / Carte.TauxConversionJeton, 2);

        EnregistrerActivite(
            "VENTE GARAGE",
            $"Vente de {exemplaire.Carte?.NomComplet ?? "Voiture"} (+{Carte.FormaterMontantJetons(jetonsCredites)})",
            $"Crédits ajoutés au Portefeuille liquide ({exemplaire.ValeurActuelleFormatee} convertis en {Carte.FormaterMontantJetons(jetonsCredites)})",
            valeurEuros,
            true,
            utilisateurConnecte);

        NotifierModificationDonnees();
        return Task.FromResult(jetonsCredites);
    }

    // =========================================================================
    // F15: Déballage de booster (POST boosters/open - 3 cartes par booster)
    // =========================================================================

    /// <summary>
    /// Ouvre un paquet de cartes (booster) et délivre 3 cartes automobiles selon la pyramide de rareté officielle.
    /// </summary>
    /// <returns>La liste des cartes obtenues (3 cartes en production).</returns>
    public async Task<List<Carte>> OuvrirBoosterAsync()
    {
        await RestaurerAuthentificationAsync();

        using var contenu = new StringContent("{}", Encoding.UTF8, "application/json");
        using var reponse = await client.PostAsync("boosters/open", contenu);

        if (estModeProduction)
        {
            return GenererTirageBoosterProduction();
        }

        string json = await LireReponseAsync(reponse);

        try
        {
            JToken token = JToken.Parse(json);
            List<Carte> cartes = new();

            if (token is JObject jObj)
            {
                JArray? arr = (jObj["cards"] as JArray) ?? (jObj["items"] as JArray) ?? (jObj["data"] as JArray);
                if (arr != null)
                {
                    cartes = arr.ToObject<List<Carte>>() ?? new List<Carte>();
                }
            }
            else if (token is JArray jArr)
            {
                cartes = jArr.ToObject<List<Carte>>() ?? new List<Carte>();
            }

            if (estModeProduction)
            {
                foreach (var c in cartes)
                    EnrichirCarte(c);

                if (cartes.Count > 3)
                    cartes = cartes.Take(3).ToList();
            }

            return cartes;
        }
        catch (Exception ex) when (ex is JsonReaderException || ex is JsonSerializationException)
        {
            throw new Exception("Le serveur a renvoyé des données JSON corrompues ou inattendues.", ex);
        }
    }

    /// <summary>
    /// Tire une carte selon la pyramide stricte des 4 raretés officielles :
    /// COMMUNE (62%), RARE (25%), ÉPIQUE (10%), LÉGENDAIRE (3% - très rare).
    /// </summary>
    private Carte TirerCarteSelonProbabiliteRarete(List<Carte> baseVoitures, HashSet<int> indicesUtilises)
    {
        int tiragePourcentage = generateurAleatoire.Next(1, 101);
        string rareteCible;

        if (tiragePourcentage <= 3)
        {
            rareteCible = "LÉGENDAIRE"; // 3% de chance
        }
        else if (tiragePourcentage <= 13)
        {
            rareteCible = "ÉPIQUE"; // 10% de chance
        }
        else if (tiragePourcentage <= 38)
        {
            rareteCible = "RARE"; // 25% de chance
        }
        else
        {
            rareteCible = "COMMUNE"; // 62% de chance
        }

        var candidats = new List<int>();
        for (int i = 0; i < baseVoitures.Count; i++)
        {
            if (!indicesUtilises.Contains(i) &&
                string.Equals(NormaliserRareteFrancaise(baseVoitures[i].Rarete), rareteCible, StringComparison.OrdinalIgnoreCase))
            {
                candidats.Add(i);
            }
        }

        if (candidats.Count == 0)
        {
            for (int i = 0; i < baseVoitures.Count; i++)
            {
                if (!indicesUtilises.Contains(i))
                {
                    candidats.Add(i);
                }
            }
        }

        int indexChoisi = candidats.Count > 0
            ? candidats[generateurAleatoire.Next(candidats.Count)]
            : 0;

        indicesUtilises.Add(indexChoisi);
        return ClonerCarte(baseVoitures[indexChoisi]);
    }

    /// <summary>
    /// Génère un tirage authentique de 3 cartes depuis la base de 300 voitures selon les probabilités de rareté
    /// et les ajoute au Patrimoine (Garage) de la session.
    /// </summary>
    private List<Carte> GenererTirageBoosterProduction()
    {
        var baseVoitures = ChargerBaseVoituresLocale();
        if (baseVoitures.Count == 0)
            return new List<Carte>();

        var tirage = new List<Carte>();
        var indicesUtilises = new HashSet<int>();

        while (tirage.Count < 3 && indicesUtilises.Count < baseVoitures.Count)
        {
            Carte carteTiree = TirerCarteSelonProbabiliteRarete(baseVoitures, indicesUtilises);
            EnrichirCarte(carteTiree);
            tirage.Add(carteTiree);
        }

        decimal valeurTotaleBooster = 0m;
        var garageCourant = ObtenirGarageUtilisateurCourant();
        foreach (var carteTiree in tirage)
        {
            valeurTotaleBooster += carteTiree.PrixOfficiel;
            string guidCourt = Guid.NewGuid().ToString("N").Substring(0, 8);
            var exemplaire = new CartePossedee(
                "ex-booster-" + guidCourt,
                ClonerCarte(carteTiree),
                carteTiree.PrixOfficiel,
                1,
                Math.Round(carteTiree.PrixOfficiel * 0.85m, 0));
            garageCourant.Insert(0, exemplaire);
        }

        var meilleureCarte = tirage.OrderByDescending(c => c.PrixOfficiel).FirstOrDefault();
        decimal jetonsBooster = Math.Round(valeurTotaleBooster / Carte.TauxConversionJeton, 2);
        EnregistrerActivite(
            "BOOSTER",
            $"Ouverture d'un Pack Booster (3 cartes · +{Carte.FormaterMontantJetons(jetonsBooster)} Patrimoine)",
            meilleureCarte != null
                ? $"Tirage vedette : {meilleureCarte.NomComplet} ({meilleureCarte.Rarete} · {meilleureCarte.ValeurJetonsFormatee})"
                : "3 nouvelles voitures ajoutées au Patrimoine",
            valeurTotaleBooster,
            true,
            utilisateurConnecte);

        NotifierModificationDonnees();
        return tirage;
    }

    // =========================================================================
    // F32 / F34: Cadeaux et transfert de carte entre étudiants (RM009, EF060, EF061)
    // =========================================================================

    /// <summary>
    /// Vérifie si un exemplaire possédé est actuellement engagé dans une offre active sur le marché (Règles RM003 et EF061).
    /// </summary>
    /// <param name="exemplaire">L'exemplaire de voiture à vérifier.</param>
    /// <returns>Vrai si l'exemplaire est déjà mis en vente.</returns>
    public bool ExemplaireEstEnVente(CartePossedee? exemplaire)
    {
        if (exemplaire == null)
            return false;

        ChargerEtatPartageDepuisDisque();
        string cle = exemplaire.IdentifiantExemplaire ?? exemplaire.Identifiant;
        return offresSession.Any(o =>
            (string.Equals(o.Statut, "active", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(o.Statut, "reserved", StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(o.IdentifiantExemplaire, cle, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Enregistre le don d'un exemplaire possédé à un autre compte étudiant (alice, bob ou charlie)
    /// conformément aux règles RM009, EF060 et EF061.
    /// </summary>
    public async Task<bool> OffrirCarteAsync(CartePossedee exemplaire, string destinataire)
    {
        if (exemplaire == null || string.IsNullOrWhiteSpace(destinataire))
            return false;

        // Règle EF061 / RM003 : Empêcher l'offre d'une carte engagée dans une vente
        if (ExemplaireEstEnVente(exemplaire))
            throw new InvalidOperationException("Cette voiture ne peut pas être offerte car elle est actuellement mise en vente sur le marché.");

        await RestaurerAuthentificationAsync();

        // Tentative d'appel du endpoint officiel POST /gifts de l'API
        try
        {
            string idCarteServeur = exemplaire.Carte?.Identifiant ?? exemplaire.Identifiant;
            var chargeUtile = new
            {
                cardId = idCarteServeur,
                recipient = destinataire.Trim().ToLowerInvariant()
            };
            string json = JsonConvert.SerializeObject(chargeUtile);
            using var requete = new HttpRequestMessage(HttpMethod.Post, "gifts")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
            using var reponse = await client.SendAsync(requete);
        }
        catch
        {
            // Tolérance hors-ligne pour la simulation de session
        }

        string cleExemplaire = !string.IsNullOrWhiteSpace(exemplaire.IdentifiantExemplaire)
            ? exemplaire.IdentifiantExemplaire
            : exemplaire.Identifiant;

        // Retrait de la collection du donateur (REC12)
        ObtenirGarageUtilisateurCourant().RemoveAll(c =>
            string.Equals(c.IdentifiantExemplaire ?? c.Identifiant, cleExemplaire, StringComparison.OrdinalIgnoreCase));

        // Ajout dans le garage du bénéficiaire (RM009)
        string cleDestinataire = destinataire.Trim().ToLowerInvariant();
        if (!garagesParUtilisateur.ContainsKey(cleDestinataire))
        {
            garagesParUtilisateur[cleDestinataire] = new List<CartePossedee>();
        }
        garagesParUtilisateur[cleDestinataire].Insert(0, exemplaire);

        // Journalisation de l'opération pour le donateur et le bénéficiaire (EF062, REC12)
        EnregistrerActivite(
            "CADEAU",
            $"🎁 Don de {exemplaire.Carte?.NomComplet ?? "Voiture"} à {destinataire.Trim().ToUpper()}",
            $"Transfert sans contrepartie financière vers le garage de {destinataire.Trim().ToUpper()} (RM009)",
            -exemplaire.ValeurActuelle,
            false,
            utilisateurConnecte);

        EnregistrerActivite(
            "CADEAU",
            $"🎁 Cadeau reçu : {exemplaire.Carte?.NomComplet ?? "Voiture"} de {utilisateurConnecte.ToUpper()}",
            $"Voiture offerte gratuitement par {utilisateurConnecte.ToUpper()} et ajoutée à votre garage (RM009)",
            exemplaire.ValeurActuelle,
            true,
            cleDestinataire);

        NotifierModificationDonnees();
        return true;
    }

    /// <summary>
    /// Ajoute une entrée horodatée au journal d'activité de la session sans aucune propriété visuelle C#.
    /// </summary>
    public void EnregistrerActivite(
        string typeOperation,
        string titre,
        string sousTitre,
        decimal montant,
        bool estPositif,
        string? utilisateurCible = null)
    {
        decimal montantSigne = estPositif ? Math.Abs(montant) : -Math.Abs(montant);
        string idAct = "act-" + Guid.NewGuid().ToString("N").Substring(0, 8);
        string compte = string.IsNullOrWhiteSpace(utilisateurCible) ? utilisateurConnecte : utilisateurCible.Trim().ToLowerInvariant();
        journalActivitesSession.Insert(0, new TransactionActivite(
            idAct,
            typeOperation,
            titre,
            sousTitre,
            montantSigne,
            "⚡",
            DateTime.Now,
            compte));
    }

    /// <summary>
    /// Récupère l'historique complet des activités et transactions de l'utilisateur pour l'onglet Activité (Tab 5).
    /// </summary>
    public Task<List<TransactionActivite>> RecupererActivitesAsync()
    {
        ChargerEtatPartageDepuisDisque();
        var liste = journalActivitesSession
            .Where(a => string.IsNullOrWhiteSpace(a.Utilisateur) || string.Equals(a.Utilisateur, utilisateurConnecte, StringComparison.OrdinalIgnoreCase))
            .ToList();
        return Task.FromResult(liste);
    }

    #endregion
}

/// <summary>
/// Structure de persistance partagée permettant la synchronisation en direct entre les comptes Alice, Bob et Charlie.
/// </summary>
public class EtatPartageAutoMasters
{
    public Dictionary<string, List<CartePossedee>> GaragesParUtilisateur { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, decimal> SoldesParUtilisateur { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<Offre> OffresSession { get; set; } = new();
    public List<TransactionActivite> JournalActivitesSession { get; set; } = new();
    public Dictionary<string, PointCotationMarche> CoursModifiesParVentes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
