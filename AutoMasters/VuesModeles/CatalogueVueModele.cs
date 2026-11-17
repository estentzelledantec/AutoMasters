using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using AutoMasters.Modeles;
using AutoMasters.Services;
using AutoMasters.Vues;

namespace AutoMasters.VuesModeles;

/// <summary>
/// Vue-modèle de la page du Catalogue des voitures (Fonctionnalité F21).
/// Gère le chargement des voitures depuis ServiceAPI, la recherche par texte,
/// le filtrage par rareté et l'ouverture de la fiche détaillée d'une carte.
/// </summary>
public partial class CatalogueVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;
    private List<Carte> toutesLesCartes = new();
    private List<Carte> cartesFiltreesCompletes = new();

    /// <summary>
    /// Nombre de voitures affichées par page dans le catalogue.
    /// </summary>
    public const int TaillePage = 14;

    [ObservableProperty]
    private ObservableCollection<Carte> cartes = new();

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
    private ObservableCollection<string> raretes = new();

    [ObservableProperty]
    private string texteRecherche = "";

    [ObservableProperty]
    private string rareteSelectionnee = "TOUTES";

    [ObservableProperty]
    private Carte? carteSelectionnee;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private bool estEnActualisation;

    [ObservableProperty]
    private string messageErreur = "";

    [ObservableProperty]
    private bool aErreur;

    [ObservableProperty]
    private bool estVide;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(NombreResultatsFormate))]
    private int nombreResultats;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant le singleton ServiceAPI.
    /// Utilisé lors de l'instanciation XAML de la vue.
    /// </summary>
    public CatalogueVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP pour les tests unitaires déterministes.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
    public CatalogueVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;

        Raretes = new ObservableCollection<string>
        {
            "TOUTES",
            "COMMUNE",
            "RARE",
            "ÉPIQUE",
            "LÉGENDAIRE"
        };

        // Déclenchement du chargement initial et abonnement aux mises à jour du marché
        this.serviceApi.DonneesMarcheModifiees += () => _ = ChargerCatalogueAsync();
        _ = ChargerCatalogueAsync();
    }

    #endregion

    #region Getter/Setter

    /// <summary>
    /// Indique si l'interface est prête pour l'interaction utilisateur.
    /// </summary>
    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Texte indiquant le nombre de véhicules trouvés dans le catalogue.
    /// </summary>
    public string NombreResultatsFormate
    {
        get
        {
            if (NombreResultats == 0)
                return "Aucun véhicule disponible";
            if (NombreResultats == 1)
                return "1 véhicule disponible";
            return $"{NombreResultats} véhicules disponibles";
        }
    }

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

    /// <summary>
    /// Détecte les modifications du texte de recherche et recalcule les filtres immédiatement.
    /// </summary>
    partial void OnTexteRechercheChanged(string value)
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Détecte les modifications du filtre de rareté et recalcule les filtres immédiatement.
    /// </summary>
    partial void OnRareteSelectionneeChanged(string value)
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Charge l'intégralité du catalogue depuis l'API REST officielle.
    /// </summary>
    [RelayCommand]
    public async Task ChargerCatalogueAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        MessageErreur = "";
        AErreur = false;

        try
        {
            var resultat = await serviceApi.RecupererCatalogueAsync();
            toutesLesCartes = resultat ?? new List<Carte>();
            AppliquerFiltres();
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
            AErreur = true;
        }
        finally
        {
            EstOccupe = false;
            EstEnActualisation = false;
        }
    }

    /// <summary>
    /// Commande de rafraîchissement déclenchée par le geste Pull-to-Refresh.
    /// </summary>
    [RelayCommand]
    public async Task ActualiserAsync()
    {
        EstEnActualisation = true;
        try
        {
            var resultat = await serviceApi.RecupererCatalogueAsync();
            toutesLesCartes = resultat ?? new List<Carte>();
            AppliquerFiltres();
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
            AErreur = true;
        }
        finally
        {
            EstEnActualisation = false;
        }
    }

    /// <summary>
    /// Filtre les cartes en mémoire selon le texte saisi et la rareté sélectionnée.
    /// </summary>
    private void AppliquerFiltres()
    {
        IEnumerable<Carte> requete = toutesLesCartes;

        // Filtrage textuel (marque, modèle ou variante)
        if (!string.IsNullOrWhiteSpace(TexteRecherche))
        {
            string recherche = TexteRecherche.Trim();
            requete = requete.Where(c =>
                (c.Marque != null && c.Marque.Contains(recherche, StringComparison.OrdinalIgnoreCase)) ||
                (c.Modele != null && c.Modele.Contains(recherche, StringComparison.OrdinalIgnoreCase)) ||
                (c.Variante != null && c.Variante.Contains(recherche, StringComparison.OrdinalIgnoreCase)));
        }

        // Filtrage par rareté
        if (!string.IsNullOrWhiteSpace(RareteSelectionnee) &&
            !string.Equals(RareteSelectionnee, "TOUTES", StringComparison.OrdinalIgnoreCase))
        {
            requete = requete.Where(c => string.Equals(c.Rarete, RareteSelectionnee, StringComparison.OrdinalIgnoreCase));
        }

        cartesFiltreesCompletes = requete.ToList();
        NombreResultats = cartesFiltreesCompletes.Count;
        EstVide = NombreResultats == 0 && !EstOccupe;

        if (cartesFiltreesCompletes.Count == 0)
        {
            NombreTotalPages = 1;
        }
        else
        {
            NombreTotalPages = (int)Math.Ceiling((double)cartesFiltreesCompletes.Count / TaillePage);
        }

        PageActuelle = 1;
        SaisiePage = "1";
        MettreAJourPageCatalogue();
    }

    /// <summary>
    /// Permet de changer directement de page lorsque l'utilisateur saisit un numéro de page (ex: 13).
    /// </summary>
    partial void OnSaisiePageChanged(string value)
    {
        if (int.TryParse(value?.Trim(), out int numeroPage))
        {
            if (numeroPage >= 1 && numeroPage <= NombreTotalPages && numeroPage != PageActuelle)
            {
                PageActuelle = numeroPage;
                MettreAJourPageCatalogue();
            }
        }
    }

    /// <summary>
    /// Valide la saisie manuelle du numéro de page.
    /// </summary>
    [RelayCommand]
    private void AllerAPage()
    {
        if (int.TryParse(SaisiePage?.Trim(), out int numeroPage))
        {
            if (numeroPage < 1)
                numeroPage = 1;
            if (numeroPage > NombreTotalPages)
                numeroPage = NombreTotalPages;

            PageActuelle = numeroPage;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageCatalogue();
        }
        else
        {
            SaisiePage = PageActuelle.ToString();
        }
    }

    /// <summary>
    /// Affiche uniquement les 14 voitures correspondant à la page actuelle.
    /// </summary>
    private void MettreAJourPageCatalogue()
    {
        int indexDepart = (PageActuelle - 1) * TaillePage;
        var pageCartes = cartesFiltreesCompletes
            .Skip(indexDepart)
            .Take(TaillePage)
            .ToList();

        Cartes.Clear();
        foreach (var carte in pageCartes)
        {
            Cartes.Add(carte);
        }
    }

    /// <summary>
    /// Passe aux 14 véhicules suivants du catalogue.
    /// </summary>
    [RelayCommand]
    private void PageSuivante()
    {
        if (PageActuelle < NombreTotalPages)
        {
            PageActuelle = PageActuelle + 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageCatalogue();
        }
    }

    /// <summary>
    /// Revient aux 14 véhicules précédents du catalogue.
    /// </summary>
    [RelayCommand]
    private void PagePrecedente()
    {
        if (PageActuelle > 1)
        {
            PageActuelle = PageActuelle - 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageCatalogue();
        }
    }

    /// <summary>
    /// Commande explicite de recherche pour la SearchBar.
    /// </summary>
    [RelayCommand]
    private void Rechercher()
    {
        AppliquerFiltres();
    }

    /// <summary>
    /// Sélectionne un niveau de rareté via les puces interactives.
    /// </summary>
    [RelayCommand]
    private void FiltrerParRarete(string rarete)
    {
        if (string.IsNullOrWhiteSpace(rarete))
            RareteSelectionnee = "TOUTES";
        else
            RareteSelectionnee = rarete;
    }

    /// <summary>
    /// Réinitialise l'ensemble des filtres de recherche et de rareté.
    /// </summary>
    [RelayCommand]
    private void ReinitialiserFiltres()
    {
        TexteRecherche = "";
        RareteSelectionnee = "TOUTES";
        AppliquerFiltres();
    }

    /// <summary>
    /// Navigue vers la fiche détaillée du véhicule sélectionné.
    /// Transmet simultanément l'objet Carte complet et son identifiant.
    /// </summary>
    [RelayCommand]
    private async Task SelectionnerCarteAsync(Carte? carte)
    {
        var cible = carte ?? CarteSelectionnee;
        if (cible == null)
            return;

        var parametres = new Dictionary<string, object>
        {
            { "carte", cible },
            { "id", cible.Identifiant }
        };

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync(nameof(FicheCartePage), parametres);
        }

        CarteSelectionnee = null;
    }

    #endregion
}
