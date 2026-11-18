using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;
using AutoMasters.Vues;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel de la page du Marché des voitures
/// </summary>
public partial class MarcheVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;
    private ObservableCollection<Offre> listeBruteOffres = new ObservableCollection<Offre>();
    private System.Collections.Generic.List<Offre> offresFiltreesCompletes = new System.Collections.Generic.List<Offre>();

    /// <summary>
    /// Nombre d'offres affichées par page sur le marché.
    /// </summary>
    public const int TaillePage = 14;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Offres))]
    private ObservableCollection<Offre> offresFiltrees = new ObservableCollection<Offre>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutAllerPagePrecedente))]
    [NotifyPropertyChangedFor(nameof(PeutAllerPageSuivante))]
    [NotifyPropertyChangedFor(nameof(LibellePagination))]
    private int pageActuelle = 1;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PeutAllerPagePrecedente))]
    [NotifyPropertyChangedFor(nameof(PeutAllerPageSuivante))]
    [NotifyPropertyChangedFor(nameof(AfficherPagination))]
    [NotifyPropertyChangedFor(nameof(LibellePagination))]
    private int nombreTotalPages = 1;

    [ObservableProperty]
    private string saisiePage = "1";

    [ObservableProperty]
    private string texteRecherche = "";

    [ObservableProperty]
    private string filtreRarete = "TOUTES";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LibelleOffresActives))]
    private int nombreOffresActives = 0;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SoldeFormate))]
    [NotifyPropertyChangedFor(nameof(SoldeJetons))]
    [NotifyPropertyChangedFor(nameof(SoldeJetonsFormate))]
    [NotifyPropertyChangedFor(nameof(SoldeCompletFormate))]
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

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// </summary>
    public MarcheVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP.
    /// </summary>
    public MarcheVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
        this.serviceApi.DonneesMarcheModifiees += () => _ = ChargerOffresAsync();
        _ = ChargerOffresAsync();
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Liste des offres actuellement affichées sur la page
    /// </summary>
    public ObservableCollection<Offre> Offres => OffresFiltrees;

    public string SoldeFormate => $"{SoldeUtilisateur:N0} coins";

    /// <summary>
    /// Solde liquide du Portefeuille converti en Jetons (1 Jeton = 1 000 €).
    /// </summary>
    public decimal SoldeJetons => Math.Round(SoldeUtilisateur / Carte.TauxConversionJeton, 2);

    /// <summary>
    /// Solde liquide formaté en Jetons (ex: "250 Jetons", "25,25 Jetons").
    /// </summary>
    public string SoldeJetonsFormate => Carte.FormaterMontantJetons(SoldeJetons);

    /// <summary>
    /// Solde complet du Portefeuille pour l'en-tête du Marché.
    /// </summary>
    public string SoldeCompletFormate => $"Portefeuille : {SoldeJetonsFormate}";

    public string LibelleOffresActives => $"{NombreOffresActives} offres actives";

    /// <summary>
    /// Indique s'il est possible de revenir à la page précédente.
    /// </summary>
    public bool PeutAllerPagePrecedente => PageActuelle > 1;

    /// <summary>
    /// Indique s'il est possible d'avancer à la page suivante.
    /// </summary>
    public bool PeutAllerPageSuivante => PageActuelle < NombreTotalPages;

    /// <summary>
    /// Indique si plusieurs pages sont disponibles.
    /// </summary>
    public bool AfficherPagination => NombreTotalPages > 1;

    /// <summary>
    /// Texte indiquant la page courante sur le nombre total de pages.
    /// </summary>
    public string LibellePagination => $"Page {PageActuelle} / {NombreTotalPages}";

    #endregion

    #region Methodes

    // Réagit au changement de texte de recherche.
    partial void OnTexteRechercheChanged(string value)
    {
        AppliquerFiltres();
    }

    // Réagit au changement de filtre de rareté.
    partial void OnFiltreRareteChanged(string value)
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Charge la liste des offres disponibles depuis l'API REST du Groupe 2 et synchronise le Portefeuille liquide.
    /// </summary>
    [RelayCommand]
    public async Task ChargerOffresAsync()
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

            try
            {
                var portefeuille = await serviceApi.RecupererPortefeuilleAsync();
                if (portefeuille != null && (ReferenceEquals(serviceApi, ServiceAPI.Instance) || portefeuille.Solde > 0m))
                {
                    SoldeUtilisateur = portefeuille.Solde;
                }
            }
            catch
            {
                // Ignore en cas d'erreur de récupération du solde
            }
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
    public void AppliquerFiltres()
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

        offresFiltreesCompletes = requete.ToList();
        NombreOffresActives = offresFiltreesCompletes.Count;

        if (offresFiltreesCompletes.Count == 0)
        {
            NombreTotalPages = 1;
        }
        else
        {
            NombreTotalPages = (int)Math.Ceiling((double)offresFiltreesCompletes.Count / TaillePage);
        }

        PageActuelle = 1;
        SaisiePage = "1";
        MettreAJourPageOffres();
    }

    // Permet de changer directement de page lorsque l'utilisateur saisit un numéro de page.
    partial void OnSaisiePageChanged(string value)
    {
        if (int.TryParse(value?.Trim(), out int numeroPage))
        {
            if (numeroPage >= 1 && numeroPage <= NombreTotalPages && numeroPage != PageActuelle)
            {
                PageActuelle = numeroPage;
                MettreAJourPageOffres();
            }
        }
    }

    /// <summary>
    /// Valide la saisie manuelle du numéro de page.
    /// </summary>
    [RelayCommand]
    public void AllerAPage()
    {
        if (int.TryParse(SaisiePage?.Trim(), out int numeroPage))
        {
            if (numeroPage < 1)
                numeroPage = 1;
            if (numeroPage > NombreTotalPages)
                numeroPage = NombreTotalPages;

            PageActuelle = numeroPage;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageOffres();
        }
        else
        {
            SaisiePage = PageActuelle.ToString();
        }
    }

    /// <summary>
    /// Affiche uniquement les 14 offres correspondant à la page actuelle.
    /// </summary>
    private void MettreAJourPageOffres()
    {
        int indexDepart = (PageActuelle - 1) * TaillePage;
        var pageOffres = offresFiltreesCompletes
            .Skip(indexDepart)
            .Take(TaillePage)
            .ToList();

        OffresFiltrees = new ObservableCollection<Offre>(pageOffres);
    }

    /// <summary>
    /// Passe aux 14 offres suivantes du marché.
    /// </summary>
    [RelayCommand]
    public void PageSuivante()
    {
        if (PageActuelle < NombreTotalPages)
        {
            PageActuelle = PageActuelle + 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageOffres();
        }
    }

    /// <summary>
    /// Revient aux 14 offres précédentes du marché.
    /// </summary>
    [RelayCommand]
    public void PagePrecedente()
    {
        if (PageActuelle > 1)
        {
            PageActuelle = PageActuelle - 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageOffres();
        }
    }

    /// <summary>
    /// Sélectionne un critère de rareté pour le filtrage rapide.
    /// </summary>
    [RelayCommand]
    public void DefinirFiltreRarete(string rarete)
    {
        FiltreRarete = rarete ?? "TOUTES";
        AppliquerFiltres();
    }

    /// <summary>
    /// Permet d'acheter une voiture mise en vente sur le marché
    /// </summary>
    [RelayCommand]
    public async Task AcheterOffreAsync(Offre? offre)
    {
        if (EstOccupe || offre == null)
            return;

        if (offre.EstMonOffre)
        {
            Message = "Cette voiture vous appartient déjà : vous ne pouvez pas acheter votre propre vente.";
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Action non autorisée",
                    "Cette voiture vous appartient déjà : elle est en attente d'un acheteur sur le marché.",
                    "OK");
            }
            return;
        }

        EstOccupe = true;
        try
        {
            // Vérification que le joueur a assez de jetons dans son portefeuille
            if (SoldeUtilisateur < offre.Prix)
            {
                Message = $"Fonds insuffisants : vous possédez {SoldeJetonsFormate} mais ce véhicule coûte {offre.ValeurJetonsFormatee}.";
                if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                {
                    await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                        "Fonds insuffisants",
                        $"Vous possédez {SoldeJetonsFormate}, mais cette voiture coûte {offre.ValeurJetonsFormatee}.",
                        "Fermer");
                }
                return;
            }

            string nomVehicule = $"{offre.Carte?.Marque} {offre.Carte?.Modele}".Trim();
            string messageRecap = $"Acheter {nomVehicule} pour {offre.ValeurJetonsFormatee} ({offre.PrixReelFormate}) ?\nSolde actuel : {SoldeJetonsFormate}.";

            bool confirmation = true;
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                confirmation = await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Confirmation d'achat",
                    messageRecap,
                    "Acheter immédiatement",
                    "Annuler");
            }

            if (!confirmation)
                return;

            Message = "";

            bool succes = await serviceApi.AcheterOffreAsync(offre.Identifiant);
            if (succes)
            {
                SoldeUtilisateur -= offre.Prix;
                listeBruteOffres.Remove(offre);
                AppliquerFiltres();

                Message = $"Félicitations ! Vous venez d'acquérir {nomVehicule}. Il a été ajouté à votre garage.";
                if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                {
                    await Application.Current.Windows[0].Page!.DisplayAlertAsync(
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
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
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
    /// Permet de réserver une offre sur le marché pendant 15 minutes
    /// </summary>
    [RelayCommand]
    public async Task ReserverOffreAsync(Offre? offre)
    {
        if (EstOccupe || offre == null)
            return;

        if (offre.EstMonOffre)
        {
            Message = "Cette voiture vous appartient déjà : vous ne pouvez pas réserver votre propre vente.";
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Action non autorisée",
                    "Cette voiture vous appartient déjà : vous ne pouvez pas réserver votre propre annonce.",
                    "OK");
            }
            return;
        }

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

                Message = "Offre réservée avec succès ! Verrouillée pendant 15 minutes.";
                if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                {
                    await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                        "Réservation confirmée",
                        "Cette offre vous est réservée pendant 15 minutes.",
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
    /// Permet au vendeur de retirer sa propre annonce du marché tant qu'elle n'est pas vendue
    /// </summary>
    [RelayCommand]
    public async Task RetirerOffreAsync(Offre? offre)
    {
        if (EstOccupe || offre == null)
            return;

        EstOccupe = true;
        try
        {
            bool succes = await serviceApi.RetirerOffreAsync(offre.Identifiant);
            if (succes)
            {
                listeBruteOffres.Remove(offre);
                AppliquerFiltres();
                Message = "Votre annonce a été retirée du marché. Votre voiture est de nouveau disponible dans votre garage.";
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
    /// Ouvre la page permettant de mettre en vente une voiture du garage (bouton +).
    /// </summary>
    [RelayCommand]
    public async Task OuvrirCreationOffreAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(CreationOffrePage));
        }
    }

    /// <summary>
    /// Ouvre la fiche détaillée de la voiture correspondant à l'offre sélectionnée.
    /// </summary>
    [RelayCommand]
    public async Task VoirDetailsCarteAsync(Offre? offre)
    {
        if (offre == null || offre.Carte == null)
            return;

        if (Shell.Current != null)
        {
            var parametres = new System.Collections.Generic.Dictionary<string, object>
            {
                { "carte", offre.Carte },
                { "id", offre.Carte.Identifiant }
            };
            await Shell.Current.GoToAsync(nameof(FicheCartePage), parametres);
        }
    }

    #endregion
}
