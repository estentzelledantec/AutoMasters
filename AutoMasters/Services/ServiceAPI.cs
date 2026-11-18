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
    private const string CleStockageMotDePasseSync = "automasters_sync_pwd";
    private const string ComptePartageServeur = "sync_partage_g2";
    private static ServiceAPI instance = new ServiceAPI();
    private HttpClient client;
    private readonly HttpClient clientSyncDistant;
    private string? jetonSession;
    private string? motDePasseSession;
    private string? jetonSyncDistant;
    private string idCarteReferenceSync = "4fc8c5aa-2f6e-4cbf-be48-9dc4dc30777e";
    private string empreinteDernierSyncServeur = "";
    private bool synchronisationServeurEnCours;
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
    /// Constructeur par défaut qui configure le client HTTP vers l'API du Groupe 2
    /// </summary>
    public ServiceAPI() : this(null)
    {
    }

    /// <summary>
    /// Constructeur permettant de passer un gestionnaire HTTP personnalisé
    /// </summary>
    public ServiceAPI(HttpMessageHandler? handler = null)
    {
        estModeProduction = handler == null;
        client = handler != null ? new HttpClient(handler) : new HttpClient();
        client.BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/");
        client.Timeout = TimeSpan.FromSeconds(15);

        clientSyncDistant = new HttpClient
        {
            BaseAddress = new Uri("https://api-cartes-g2.fldsio.com/"),
            Timeout = TimeSpan.FromSeconds(15)
        };

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

    // Base locale des voitures --------------------------------------

    /// <summary>
    /// Charge le fichier local cars_database.json contenant les 300 voitures
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
            if (string.IsNullOrWhiteSpace(carte.ImageUrl))
                carte.ImageUrl = refLocale.ImageUrl;
            carte.Rarete = NormaliserRareteFrancaise(string.IsNullOrWhiteSpace(carte.Rarete) ? refLocale.Rarete : carte.Rarete);

            // Garantit que le prix reste cohérent avec la valeur réelle du véhicule (ex: BMW X5 M 2023 autour de 165 000 €)
            // tout en appliquant la variation boursière en direct renvoyée par l'API
            if (refLocale.PrixOfficiel > 0m &&
                (carte.PrixOfficiel <= 0m ||
                 carte.PrixOfficiel < refLocale.PrixOfficiel * 0.85m ||
                 carte.PrixOfficiel > refLocale.PrixOfficiel * 1.15m))
            {
                carte.PrixOfficiel = Math.Round(refLocale.PrixOfficiel * (1m + (carte.VariationCours / 100m)), 2);
            }

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
        }

        return carte;
    }

    /// <summary>
    /// Vérifie si l'application complète est lancée avec une fenêtre graphique MAUI
    /// </summary>
    private bool EstExecutionApplicationReelle => estModeProduction && Microsoft.Maui.Controls.Application.Current != null;

    /// <summary>
    /// Chemin du fichier JSON partagé permettant la persistance locale et la synchronisation entre fenêtres.
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
    /// Recharge l'état partagé du marché, des garages et des soldes depuis le disque local.
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
    /// Sauvegarde l'état partagé sur le disque local.
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
    /// Retourne l'index numérique (1..300) d'une voiture dans la base locale cars_database.json.
    /// </summary>
    private int ObtenirIndexVoitureLocale(Carte? carte)
    {
        if (carte == null)
            return 1;

        var baseLocale = ChargerBaseVoituresLocale();
        var refLocale = TrouverCarteReferenceLocale(carte);
        if (refLocale != null && int.TryParse(refLocale.Identifiant, out int idNum) && idNum >= 1 && idNum <= 999)
        {
            return idNum;
        }

        int idx = baseLocale.FindIndex(c =>
            string.Equals(NormaliserTexteComparaison(c.Marque), NormaliserTexteComparaison(carte.Marque), StringComparison.OrdinalIgnoreCase) &&
            string.Equals(NormaliserTexteComparaison(c.Modele), NormaliserTexteComparaison(carte.Modele), StringComparison.OrdinalIgnoreCase));

        return idx >= 0 ? (idx + 1) : 1;
    }

    /// <summary>
    /// Retrouve une voiture dans la base locale cars_database.json à partir de son index numérique (1..300).
    /// </summary>
    private Carte TrouverVoitureParIndexLocal(int indexVoiture)
    {
        var baseLocale = ChargerBaseVoituresLocale();
        var parId = baseLocale.FirstOrDefault(c => string.Equals(c.Identifiant, indexVoiture.ToString(), StringComparison.OrdinalIgnoreCase));
        if (parId != null)
        {
            return ClonerCarte(parId);
        }

        if (indexVoiture >= 1 && indexVoiture <= baseLocale.Count)
        {
            return ClonerCarte(baseLocale[indexVoiture - 1]);
        }

        return baseLocale.Count > 0 ? ClonerCarte(baseLocale[0]) : new Carte { Marque = "BMW", Modele = "X5 M", PrixOfficiel = 165000m };
    }

    /// <summary>
    /// Encode un montant en Euros (par pas de 10 € = 0,01 Jeton) dans une date UTC (Jour 1..28, Heure, Minute, Seconde).
    /// </summary>
    private static string EncoderMontantDansDate(int annee, int mois, decimal montantEuros)
    {
        long centimesJetons = (long)Math.Clamp(Math.Round(montantEuros / 10m, 0), 0m, 2419199m);
        int jour = 1 + (int)(centimesJetons / 86400L);
        int reste = (int)(centimesJetons % 86400L);
        int heure = reste / 3600;
        int minute = (reste % 3600) / 60;
        int seconde = reste % 60;

        var dto = new DateTimeOffset(annee, Math.Clamp(mois, 1, 12), Math.Clamp(jour, 1, 28), heure, minute, seconde, TimeSpan.Zero);
        return dto.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }

    /// <summary>
    /// Décode un montant en Euros depuis une date UTC encodée par EncoderMontantDansDate.
    /// </summary>
    private static decimal DecoderMontantDepuisDate(DateTimeOffset dto)
    {
        var utc = dto.ToUniversalTime();
        long centimesJetons = ((long)(utc.Day - 1) * 86400L) + (utc.Hour * 3600L) + (utc.Minute * 60L) + utc.Second;
        return centimesJetons * 10m;
    }

    /// <summary>
    /// Authentifie le canal de synchronisation multi-PC sur https://api-cartes-g2.fldsio.com en utilisant
    /// le mot de passe saisi lors de la connexion de l'étudiant (aucun mot de passe codé en dur).
    /// </summary>
    private async Task<bool> AssurerAuthentificationSyncDistantAsync()
    {
        if (!EstExecutionApplicationReelle)
            return false;

        if (!string.IsNullOrWhiteSpace(jetonSyncDistant))
            return true;

        if (string.IsNullOrWhiteSpace(motDePasseSession))
        {
            try
            {
                motDePasseSession = await SecureStorage.Default.GetAsync(CleStockageMotDePasseSync);
            }
            catch
            {
                // Ignore si SecureStorage indisponible
            }
        }

        if (string.IsNullOrWhiteSpace(motDePasseSession))
            return false;

        try
        {
            var chargeLogin = new
            {
                username = ComptePartageServeur,
                password = motDePasseSession
            };
            string jsonLogin = JsonConvert.SerializeObject(chargeLogin);
            using var repLogin = await clientSyncDistant.PostAsync("auth/login", new StringContent(jsonLogin, Encoding.UTF8, "application/json"));
            if (repLogin.IsSuccessStatusCode)
            {
                string contenu = await repLogin.Content.ReadAsStringAsync();
                JObject obj = JObject.Parse(contenu);
                jetonSyncDistant = (string?)obj["accessToken"];
            }
            else
            {
                var chargeReg = new
                {
                    username = ComptePartageServeur,
                    displayName = "Synchronisation Temps Reel",
                    password = motDePasseSession
                };
                string jsonReg = JsonConvert.SerializeObject(chargeReg);
                using var repReg = await clientSyncDistant.PostAsync("auth/register", new StringContent(jsonReg, Encoding.UTF8, "application/json"));
                if (repReg.IsSuccessStatusCode)
                {
                    string contenu = await repReg.Content.ReadAsStringAsync();
                    JObject obj = JObject.Parse(contenu);
                    jetonSyncDistant = (string?)obj["accessToken"];
                }
            }

            if (!string.IsNullOrWhiteSpace(jetonSyncDistant))
            {
                clientSyncDistant.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jetonSyncDistant);
                return true;
            }
        }
        catch
        {
            // Tolérance réseau
        }

        return false;
    }

    /// <summary>
    /// Publie l'état complet (garages d'Alice/Bob/Charlie, offres du marché, soldes et prix révisés)
    /// sur le serveur https://api-cartes-g2.fldsio.com pour que tous les PC connectés voient exactement la même chose en direct.
    /// </summary>
    private async Task PublierEtatSurServeurDistantAsync()
    {
        if (!EstExecutionApplicationReelle)
            return;

        int attente = 0;
        while (synchronisationServeurEnCours && attente < 40)
        {
            await Task.Delay(50);
            attente++;
        }

        if (synchronisationServeurEnCours)
            return;

        synchronisationServeurEnCours = true;
        try
        {
            if (!await AssurerAuthentificationSyncDistantAsync())
                return;

            // 1. Construire la liste des enregistrements souhaités (clé signature -> (threshold, expiresAt))
            var souhaites = new List<(string Signature, decimal Threshold, string ExpiresAt)>();
            string[] joueurs = new[] { "alice", "bob", "charlie" };

            for (int j = 0; j < joueurs.Length; j++)
            {
                string joueur = joueurs[j];
                int anneeGarage = 2101 + j;
                int anneeSolde = 2201 + j;

                // Enregistrement du solde Portefeuille du joueur (toujours présent pour marquer l'initialisation du serveur)
                decimal solde = soldesParUtilisateur.ContainsKey(joueur) ? soldesParUtilisateur[joueur] : 0m;
                string expSolde = EncoderMontantDansDate(anneeSolde, 1, solde);
                souhaites.Add(($"SOLDE|{anneeSolde}|{expSolde}", 0.001m, expSolde));

                // Enregistrement de chaque voiture possédée par le joueur (dans son garage et éventuellement en vente sur le marché)
                if (garagesParUtilisateur.TryGetValue(joueur, out var listeGarage) && listeGarage != null)
                {
                    foreach (var cp in listeGarage)
                    {
                        int idxVoiture = ObtenirIndexVoitureLocale(cp.Carte);
                        decimal thresholdVoiture = Math.Clamp(Math.Round(idxVoiture / 1000m, 4), 0.001m, 0.999m);
                        string cleEx = cp.IdentifiantExemplaire ?? cp.Identifiant;

                        var offreActive = offresSession.FirstOrDefault(o =>
                            (string.Equals(o.Statut, "active", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(o.Statut, "reserved", StringComparison.OrdinalIgnoreCase)) &&
                            string.Equals(o.IdentifiantExemplaire, cleEx, StringComparison.OrdinalIgnoreCase));

                        int moisStatut = 1;
                        decimal montantReference = cp.PrixAchat > 0m ? cp.PrixAchat : (cp.ValeurActuelle > 0m ? cp.ValeurActuelle : (cp.Carte?.PrixOfficiel ?? 35000m));

                        if (offreActive != null)
                        {
                            montantReference = offreActive.Prix;
                            if (offreActive.EstReservee)
                            {
                                string resPar = (offreActive.ReservePar ?? "").Trim().ToLowerInvariant();
                                moisStatut = resPar == "alice" ? 3 : (resPar == "bob" ? 4 : 5);
                            }
                            else
                            {
                                moisStatut = 2;
                            }
                        }

                        string expVoiture = EncoderMontantDansDate(anneeGarage, moisStatut, montantReference);
                        souhaites.Add(($"CAR|{anneeGarage}|{idxVoiture}|{expVoiture}", thresholdVoiture, expVoiture));
                    }
                }
            }

            // Enregistrement des cours modifiés par les ventes sur le marché
            foreach (var kvp in coursModifiesParVentes)
            {
                var baseLocale = ChargerBaseVoituresLocale();
                var refCarte = baseLocale.FirstOrDefault(c =>
                    string.Equals($"{NormaliserTexteComparaison(c.Marque)}|{NormaliserTexteComparaison(c.Modele)}", kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (refCarte != null && kvp.Value.Prix > 0m)
                {
                    int idxVoiture = ObtenirIndexVoitureLocale(refCarte);
                    decimal thresholdVoiture = Math.Clamp(Math.Round(idxVoiture / 1000m, 4), 0.001m, 0.999m);
                    int anneeCote = kvp.Value.Variation >= 0m ? 2301 : 2302;
                    int moisVar = Math.Clamp(1 + (int)Math.Round(Math.Abs(kvp.Value.Variation) / 2m, 0), 1, 12);
                    string expCote = EncoderMontantDansDate(anneeCote, moisVar, kvp.Value.Prix);
                    souhaites.Add(($"QUOTE|{anneeCote}|{idxVoiture}|{expCote}", thresholdVoiture, expCote));
                }
            }

            // 2. Récupérer les enregistrements actuellement actifs sur le serveur
            using var repActuels = await clientSyncDistant.GetAsync("conditional-orders");
            if (!repActuels.IsSuccessStatusCode)
                return;

            string jsonActuels = await repActuels.Content.ReadAsStringAsync();
            JObject objActuels = JObject.Parse(jsonActuels);
            JArray? itemsActuels = objActuels["items"] as JArray;

            var existantsActifs = new List<(string Id, string Signature)>();
            if (itemsActuels != null)
            {
                foreach (JToken t in itemsActuels)
                {
                    string? statut = (string?)t["status"];
                    string? id = (string?)t["id"];
                    string? expStr = (string?)t["expiresAt"];
                    decimal thresh = t["threshold"] != null ? (decimal)t["threshold"]! : 0.001m;

                    if (string.Equals(statut, "active", StringComparison.OrdinalIgnoreCase) &&
                        !string.IsNullOrWhiteSpace(id) &&
                        !string.IsNullOrWhiteSpace(expStr) &&
                        DateTimeOffset.TryParse(expStr, out DateTimeOffset dto))
                    {
                        var utc = dto.ToUniversalTime();
                        string expNorm = utc.ToString("yyyy-MM-ddTHH:mm:ssZ");
                        int idxV = (int)Math.Round(thresh * 1000m, 0);
                        string sig = (utc.Year >= 2201 && utc.Year <= 2203)
                            ? $"SOLDE|{utc.Year}|{expNorm}"
                            : (utc.Year >= 2301 && utc.Year <= 2302)
                                ? $"QUOTE|{utc.Year}|{idxV}|{expNorm}"
                                : $"CAR|{utc.Year}|{idxV}|{expNorm}";
                        existantsActifs.Add((id, sig));
                    }
                }
            }

            // 3. Calculer les suppressions et les ajouts nécessaires (diff minimal ultra-rapide)
            var restantsASouhaiter = souhaites.ToList();
            var idsAConserver = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var ex in existantsActifs)
            {
                int idxMatch = restantsASouhaiter.FindIndex(s => string.Equals(s.Signature, ex.Signature, StringComparison.OrdinalIgnoreCase));
                if (idxMatch >= 0)
                {
                    idsAConserver.Add(ex.Id);
                    restantsASouhaiter.RemoveAt(idxMatch);
                }
            }

            foreach (var ex in existantsActifs)
            {
                if (!idsAConserver.Contains(ex.Id))
                {
                    using var repDel = await clientSyncDistant.DeleteAsync($"conditional-orders/{ex.Id}");
                }
            }

            foreach (var ajout in restantsASouhaiter)
            {
                var body = new
                {
                    type = "buy",
                    cardId = idCarteReferenceSync,
                    copyId = (string?)null,
                    threshold = ajout.Threshold,
                    quantity = 1,
                    expiresAt = ajout.ExpiresAt
                };
                string jsonBody = JsonConvert.SerializeObject(body);
                using var repPost = await clientSyncDistant.PostAsync("conditional-orders", new StringContent(jsonBody, Encoding.UTF8, "application/json"));
            }
        }
        catch
        {
            // Tolérance réseau
        }
        finally
        {
            synchronisationServeurEnCours = false;
        }
    }

    /// <summary>
    /// Télécharge en une seule requête GET l'état partagé depuis https://api-cartes-g2.fldsio.com
    /// afin que n'importe quel PC connecté (Alice, Bob ou Charlie) voie instantanément les ventes, garages et soldes.
    /// </summary>
    public async Task SynchroniserDepuisServeurDistantAsync()
    {
        if (!EstExecutionApplicationReelle || synchronisationServeurEnCours)
            return;

        synchronisationServeurEnCours = true;
        try
        {
            if (!await AssurerAuthentificationSyncDistantAsync())
                return;

            using var reponse = await clientSyncDistant.GetAsync("conditional-orders");
            if (!reponse.IsSuccessStatusCode)
                return;

            string json = await reponse.Content.ReadAsStringAsync();
            JObject obj = JObject.Parse(json);
            JArray? items = obj["items"] as JArray;
            if (items == null)
                return;

            var actifs = new List<(string Id, decimal Threshold, DateTimeOffset ExpiresAt)>();
            foreach (JToken t in items)
            {
                string? statut = (string?)t["status"];
                string? id = (string?)t["id"];
                string? expStr = (string?)t["expiresAt"];
                decimal thresh = t["threshold"] != null ? (decimal)t["threshold"]! : 0.001m;

                if (string.Equals(statut, "active", StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrWhiteSpace(id) &&
                    !string.IsNullOrWhiteSpace(expStr) &&
                    DateTimeOffset.TryParse(expStr, out DateTimeOffset dto))
                {
                    var utc = dto.ToUniversalTime();
                    if (utc.Year >= 2101 && utc.Year <= 2302)
                    {
                        actifs.Add((id, thresh, utc));
                    }
                }
            }

            // Si le serveur distant est encore vierge mais que ce PC possède déjà des voitures/offres locales, on les publie sur le serveur
            if (actifs.Count == 0)
            {
                bool aDonneesLocales = offresSession.Count > 0 ||
                    garagesParUtilisateur.Values.Any(g => g.Count > 0) ||
                    soldesParUtilisateur.Values.Any(s => s > 0m);

                if (aDonneesLocales)
                {
                    synchronisationServeurEnCours = false;
                    await PublierEtatSurServeurDistantAsync();
                }
                return;
            }

            string nouvelleEmpreinte = string.Join(";", actifs
                .OrderBy(a => a.Id)
                .Select(a => $"{a.Id}:{a.Threshold}:{a.ExpiresAt:yyyyMMddHHmmss}"));

            if (string.Equals(nouvelleEmpreinte, empreinteDernierSyncServeur, StringComparison.Ordinal))
                return;

            empreinteDernierSyncServeur = nouvelleEmpreinte;

            var nouveauxGarages = new Dictionary<string, List<CartePossedee>>(StringComparer.OrdinalIgnoreCase)
            {
                { "alice", new List<CartePossedee>() },
                { "bob", new List<CartePossedee>() },
                { "charlie", new List<CartePossedee>() }
            };
            var nouveauxSoldes = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
            {
                { "alice", 0m },
                { "bob", 0m },
                { "charlie", 0m }
            };
            var nouvellesOffres = new List<Offre>();
            var nouvellesCotes = new Dictionary<string, PointCotationMarche>(StringComparer.OrdinalIgnoreCase);

            // 1. Décoder d'abord les cotes modifiées par les ventes (2301..2302)
            foreach (var item in actifs.Where(a => a.ExpiresAt.Year == 2301 || a.ExpiresAt.Year == 2302))
            {
                int idxVoiture = (int)Math.Round(item.Threshold * 1000m, 0);
                Carte carteRef = TrouverVoitureParIndexLocal(idxVoiture);
                decimal nouveauPrix = DecoderMontantDepuisDate(item.ExpiresAt);
                decimal variationAbs = (item.ExpiresAt.Month - 1) * 2m;
                decimal variation = item.ExpiresAt.Year == 2301 ? variationAbs : -variationAbs;
                string cleCours = $"{NormaliserTexteComparaison(carteRef.Marque)}|{NormaliserTexteComparaison(carteRef.Modele)}";

                nouvellesCotes[cleCours] = new PointCotationMarche(
                    nouveauPrix,
                    variation,
                    DateTime.Now.ToString("dd/MM/yyyy à HH:mm:ss"),
                    54);
            }

            // 2. Décoder les soldes des portefeuilles (2201..2203)
            foreach (var item in actifs.Where(a => a.ExpiresAt.Year >= 2201 && a.ExpiresAt.Year <= 2203))
            {
                string joueur = item.ExpiresAt.Year == 2201 ? "alice" : (item.ExpiresAt.Year == 2202 ? "bob" : "charlie");
                nouveauxSoldes[joueur] = DecoderMontantDepuisDate(item.ExpiresAt);
            }

            // 3. Décoder les voitures des garages et les offres du marché (2101..2103)
            foreach (var item in actifs.Where(a => a.ExpiresAt.Year >= 2101 && a.ExpiresAt.Year <= 2103))
            {
                string proprietaire = item.ExpiresAt.Year == 2101 ? "alice" : (item.ExpiresAt.Year == 2102 ? "bob" : "charlie");
                int idxVoiture = (int)Math.Round(item.Threshold * 1000m, 0);
                Carte carte = TrouverVoitureParIndexLocal(idxVoiture);
                EnrichirCarte(carte);

                string cleCours = $"{NormaliserTexteComparaison(carte.Marque)}|{NormaliserTexteComparaison(carte.Modele)}";
                if (nouvellesCotes.TryGetValue(cleCours, out var coteMod) && coteMod.Prix > 0m)
                {
                    carte.PrixOfficiel = coteMod.Prix;
                    carte.VariationCours = coteMod.Variation;
                }

                decimal montantEuros = DecoderMontantDepuisDate(item.ExpiresAt);
                string idCourt = item.Id.Replace("-", "").Substring(0, Math.Min(10, item.Id.Replace("-", "").Length));
                string idExemplaire = "ex-srv-" + idCourt;

                decimal coutAcquisition = item.ExpiresAt.Month == 1
                    ? (montantEuros > 0m ? montantEuros : carte.PrixOfficiel)
                    : carte.PrixOfficiel;

                var exemplaire = new CartePossedee(
                    idExemplaire,
                    ClonerCarte(carte),
                    carte.PrixOfficiel,
                    1,
                    coutAcquisition);
                nouveauxGarages[proprietaire].Add(exemplaire);

                if (item.ExpiresAt.Month >= 2)
                {
                    bool estRes = item.ExpiresAt.Month >= 3;
                    string statutOffre = estRes ? "reserved" : "active";
                    string idOffre = "offre-srv-" + idCourt;

                    var offre = new Offre(
                        idOffre,
                        idExemplaire,
                        montantEuros > 0m ? montantEuros : carte.PrixOfficiel,
                        proprietaire,
                        statutOffre,
                        ClonerCarte(carte));

                    if (estRes)
                    {
                        offre.EstReservee = true;
                        offre.ReservePar = item.ExpiresAt.Month == 3 ? "alice" : (item.ExpiresAt.Month == 4 ? "bob" : "charlie");
                        offre.DateExpiration = DateTime.UtcNow.AddMinutes(15);
                    }

                    nouvellesOffres.Add(offre);
                }
            }

            lock (verrouPartage)
            {
                garagesParUtilisateur.Clear();
                foreach (var kvp in nouveauxGarages)
                    garagesParUtilisateur[kvp.Key] = kvp.Value;

                soldesParUtilisateur.Clear();
                foreach (var kvp in nouveauxSoldes)
                    soldesParUtilisateur[kvp.Key] = kvp.Value;

                offresSession.Clear();
                offresSession.AddRange(nouvellesOffres);

                coursModifiesParVentes.Clear();
                foreach (var kvp in nouvellesCotes)
                    coursModifiesParVentes[kvp.Key] = kvp.Value;
            }

            SauvegarderEtatPartageSurDisque();

            if (Microsoft.Maui.Controls.Application.Current?.Dispatcher != null)
            {
                Microsoft.Maui.Controls.Application.Current.Dispatcher.Dispatch(() =>
                {
                    DonneesMarcheModifiees?.Invoke();
                });
            }
        }
        catch
        {
            // Tolérance réseau
        }
        finally
        {
            synchronisationServeurEnCours = false;
        }
    }

    /// <summary>
    /// Déclenche l'événement DonneesMarcheModifiees sur le thread principal de l'interface
    /// et publie immédiatement l'état sur le serveur distant https://api-cartes-g2.fldsio.com.
    /// </summary>
    private void NotifierModificationDonnees()
    {
        SauvegarderEtatPartageSurDisque();
        _ = Task.Run(async () =>
        {
            await PublierEtatSurServeurDistantAsync();
        });

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
    /// Surveille toutes les 2,5 secondes les mises à jour sur le serveur https://api-cartes-g2.fldsio.com
    /// (et sur le disque local) afin que les actions d'un PC (ex: Bob) s'affichent en direct sur le PC d'un autre joueur (ex: Alice).
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
                await Task.Delay(2500);
                try
                {
                    await SynchroniserDepuisServeurDistantAsync();

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

    /// <summary>
    /// Lit le contenu d'une réponse HTTP et lève une erreur explicite si le code HTTP n'est pas valide
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
    /// Récupère le token JWT sauvegardé dans SecureStorage et l'ajoute à l'en-tête HTTP
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
            // Ignore si SecureStorage n'est pas disponible
        }

        return null;
    }

    /// <summary>
    /// Sauvegarde le token JWT dans l'en-tête HTTP et dans SecureStorage
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
            // Ignore si SecureStorage n'est pas disponible
        }
    }

    /// <summary>
    /// Vide le token en mémoire et dans SecureStorage lors de la déconnexion
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
            // Ignore si SecureStorage n'est pas disponible
        }
    }

    /// <summary>
    /// Déconnecte l'utilisateur actuel
    /// </summary>
    public async Task SeDeconnecterAsync()
    {
        ReinitialiserSession();
        await Task.CompletedTask;
    }

    // Connexion et authentification --------------------------------------

    /// <summary>
    /// Connecte un joueur via POST /auth/login et enregistre son token JWT
    /// </summary>
    public async Task<ReponseConnexion> SeConnecterAsync(string identifiant, string motDePasse)
    {
        if (string.IsNullOrWhiteSpace(identifiant) || string.IsNullOrWhiteSpace(motDePasse))
            throw new ArgumentException("L'identifiant et le mot de passe sont obligatoires.");

        var demande = new DemandeConnexion
        {
            Identifiant = identifiant.Trim(),
            MotDePasse = motDePasse
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

        utilisateurConnecte = identifiant.Trim().ToLowerInvariant();
        motDePasseSession = motDePasse.Trim();
        await SauvegarderJetonAsync(connexion.JetonAcces);

        try
        {
            if (EstExecutionApplicationReelle)
            {
                await SecureStorage.Default.SetAsync(CleStockageMotDePasseSync, motDePasseSession);
            }
        }
        catch
        {
            // Ignore dans un environnement sans SecureStorage
        }

        if (estModeProduction)
        {
            ChargerEtatPartageDepuisDisque();
            if (EstExecutionApplicationReelle)
            {
                await SynchroniserDepuisServeurDistantAsync();
            }

            EnregistrerActivite(
                "CONNEXION",
                $"Connexion authentifiée ({utilisateurConnecte})",
                "Session JWT initialisée sur le serveur API Groupe 2",
                0m,
                true,
                utilisateurConnecte);
            SauvegarderEtatPartageSurDisque();
            DonneesMarcheModifiees?.Invoke();
        }

        return connexion;
    }

    // Catalogue et fiches voitures --------------------------------------

    /// <summary>
    /// Récupère la liste des 300 voitures du catalogue depuis GET /cards avec filtres optionnels
    /// </summary>
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

    public Task<List<Carte>> RecupererCartesAsync(string? search = null, string? rarete = null)
    {
        return RecupererCatalogueAsync(search, rarete);
    }

    /// <summary>
    /// Récupère la fiche détaillée d'une voiture via GET /cards/{id}
    /// </summary>
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
    /// Récupère les derniers prix d'une voiture depuis GET /cards/{id}/quotes pour tracer la courbe de marché
    /// </summary>
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

                        if (estModeProduction && points.Count > 0)
                        {
                            var refLocale = TrouverCarteReferenceLocale(carte);
                            decimal prixCible = (refLocale != null && refLocale.PrixOfficiel > 0m)
                                ? refLocale.PrixOfficiel
                                : carte.PrixOfficiel;
                            decimal dernierPrixServeur = points.Last().Prix;

                            if (prixCible > 0m && dernierPrixServeur > 0m &&
                                (dernierPrixServeur > prixCible * 1.15m || dernierPrixServeur < prixCible * 0.85m))
                            {
                                decimal facteurEchelle = prixCible / dernierPrixServeur;
                                foreach (var pt in points)
                                {
                                    pt.Prix = Math.Round(pt.Prix * facteurEchelle, 0);
                                }
                            }
                        }
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

            // Met à jour le dernier prix reçu et sa date sur la carte
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

    // Garage et portefeuille --------------------------------------

    /// <summary>
    /// Récupère le garage et le solde du joueur connecté
    /// </summary>
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

        if (EstExecutionApplicationReelle)
        {
            await SynchroniserDepuisServeurDistantAsync();
        }

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

    // Marché, achats et ventes --------------------------------------

    /// <summary>
    /// Récupère la liste des annonces en vente sur le marché
    /// </summary>
    public async Task<List<Offre>> RecupererOffresAsync(string? cardId = null)
    {
        await RestaurerAuthentificationAsync();

        string url = !string.IsNullOrWhiteSpace(cardId)
            ? $"cards/{Uri.EscapeDataString(cardId.Trim())}/offers"
            : "offers";

        using var reponse = await client.GetAsync(url);

        if (estModeProduction)
        {
            if (EstExecutionApplicationReelle)
            {
                await SynchroniserDepuisServeurDistantAsync();
            }
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

    /// <summary>
    /// Permet d'acheter une voiture sur le marché via POST /orders/buy
    /// </summary>
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
                    // Mise à jour du prix de marché de la voiture (moyenne entre l'ancien prix et le prix de vente)
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

    /// <summary>
    /// Réserve une offre sur le marché pendant 15 minutes
    /// </summary>
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

    /// <summary>
    /// Met en vente une voiture du garage sur le marché (le prix saisi est en jetons)
    /// </summary>
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

        decimal prixNormaliseEuros = estModeProduction
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
    /// Permet au vendeur d'annuler son offre de vente
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

    public async Task<Offre> PublierOffreAsync(string copyId, decimal prix)
    {
        return await CreerOffreAsync(copyId, prix);
    }

    /// <summary>
    /// Vend directement une voiture du garage contre sa valeur en jetons
    /// </summary>
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

    // Ouverture des boosters --------------------------------------

    /// <summary>
    /// Ouvre un booster et tire 3 voitures selon leur rareté
    /// </summary>
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

    // Cadeaux et historique d'activité --------------------------------------

    /// <summary>
    /// Vérifie si une voiture du garage est actuellement mise en vente sur le marché
    /// </summary>
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
    /// Transfère une voiture du garage en cadeau à un autre joueur (alice, bob ou charlie)
    /// </summary>
    public async Task<bool> OffrirCarteAsync(CartePossedee exemplaire, string destinataire)
    {
        if (exemplaire == null || string.IsNullOrWhiteSpace(destinataire))
            return false;

        if (ExemplaireEstEnVente(exemplaire))
            throw new InvalidOperationException("Cette voiture ne peut pas être offerte car elle est actuellement mise en vente sur le marché.");

        await RestaurerAuthentificationAsync();

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
            // Ignore si le serveur ne répond pas sur /gifts
        }

        string cleExemplaire = !string.IsNullOrWhiteSpace(exemplaire.IdentifiantExemplaire)
            ? exemplaire.IdentifiantExemplaire
            : exemplaire.Identifiant;

        // Retrait de la voiture du garage de celui qui offre
        ObtenirGarageUtilisateurCourant().RemoveAll(c =>
            string.Equals(c.IdentifiantExemplaire ?? c.Identifiant, cleExemplaire, StringComparison.OrdinalIgnoreCase));

        // Ajout de la voiture dans le garage de celui qui reçoit le cadeau
        string cleDestinataire = destinataire.Trim().ToLowerInvariant();
        if (!garagesParUtilisateur.ContainsKey(cleDestinataire))
        {
            garagesParUtilisateur[cleDestinataire] = new List<CartePossedee>();
        }
        garagesParUtilisateur[cleDestinataire].Insert(0, exemplaire);

        // Ajout dans l'historique d'activité des deux joueurs
        EnregistrerActivite(
            "CADEAU",
            $"🎁 Don de {exemplaire.Carte?.NomComplet ?? "Voiture"} à {destinataire.Trim().ToUpper()}",
            $"Transfert gratuit vers le garage de {destinataire.Trim().ToUpper()}",
            -exemplaire.ValeurActuelle,
            false,
            utilisateurConnecte);

        EnregistrerActivite(
            "CADEAU",
            $"🎁 Cadeau reçu : {exemplaire.Carte?.NomComplet ?? "Voiture"} de {utilisateurConnecte.ToUpper()}",
            $"Voiture offerte gratuitement par {utilisateurConnecte.ToUpper()} et ajoutée à votre garage",
            exemplaire.ValeurActuelle,
            true,
            cleDestinataire);

        NotifierModificationDonnees();
        return true;
    }

    /// <summary>
    /// Ajoute une opération dans l'historique des activités
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
