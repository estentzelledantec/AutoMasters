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
/// Vue-modèle de la page Mes Ordres (Fonctionnalité F33).
/// Permet d'afficher les ventes en cours et les réservations de l'utilisateur.
/// </summary>
public partial class OrdresVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;
    private List<Offre> toutesMesVentes = new();
    private List<Offre> toutesMesReservations = new();

    /// <summary>
    /// Nombre d'ordres affichés par page.
    /// </summary>
    public const int TaillePage = 14;

    [ObservableProperty]
    private ObservableCollection<Offre> mesVentes = new();

    [ObservableProperty]
    private ObservableCollection<Offre> mesReservations = new();

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
    [NotifyPropertyChangedFor(nameof(AfficherMesVentes))]
    [NotifyPropertyChangedFor(nameof(AfficherMesReservations))]
    private int ongletActifIndex = 0; // 0 = Mes Ventes, 1 = Mes Réservations

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private string message = "";

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// </summary>
    public OrdresVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
    public OrdresVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
        this.serviceApi.DonneesMarcheModifiees += () => _ = ChargerDonneesAsync();
        _ = ChargerDonneesAsync();
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public bool AfficherMesVentes => OngletActifIndex == 0;

    public bool AfficherMesReservations => OngletActifIndex == 1;

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
    /// Récupère la liste des offres depuis l'API et sépare les ventes actives des réservations en cours.
    /// </summary>
    [RelayCommand]
    public async Task ChargerDonneesAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = "";

        try
        {
            var toutesLesOffres = await serviceApi.RecupererOffresAsync();

            // Mes Ventes : Offres de vente actives ou réservées
            toutesMesVentes = toutesLesOffres
                .Where(o => string.Equals(o.Statut, "active", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(o.Statut, "reserved", StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Mes Réservations : Offres temporairement réservées
            toutesMesReservations = toutesLesOffres
                .Where(o => o.EstReservee)
                .ToList();

            PageActuelle = 1;
            SaisiePage = "1";
            MettreAJourPageOrdres();
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

    /// <summary>
    /// Permet de changer directement de page lorsque l'utilisateur saisit un numéro de page.
    /// </summary>
    partial void OnSaisiePageChanged(string value)
    {
        if (int.TryParse(value?.Trim(), out int numeroPage))
        {
            if (numeroPage >= 1 && numeroPage <= NombreTotalPages && numeroPage != PageActuelle)
            {
                PageActuelle = numeroPage;
                MettreAJourPageOrdres();
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
            MettreAJourPageOrdres();
        }
        else
        {
            SaisiePage = PageActuelle.ToString();
        }
    }

    /// <summary>
    /// Affiche uniquement les 14 ordres correspondant à la page actuelle.
    /// </summary>
    private void MettreAJourPageOrdres()
    {
        int totalElements = OngletActifIndex == 0 ? toutesMesVentes.Count : toutesMesReservations.Count;
        if (totalElements == 0)
        {
            NombreTotalPages = 1;
        }
        else
        {
            NombreTotalPages = (int)Math.Ceiling((double)totalElements / TaillePage);
        }

        if (PageActuelle > NombreTotalPages)
        {
            PageActuelle = NombreTotalPages;
        }

        int indexDepart = (PageActuelle - 1) * TaillePage;
        MesVentes = new ObservableCollection<Offre>(toutesMesVentes.Skip(indexDepart).Take(TaillePage));
        MesReservations = new ObservableCollection<Offre>(toutesMesReservations.Skip(indexDepart).Take(TaillePage));
    }

    /// <summary>
    /// Passe aux 14 ordres suivants.
    /// </summary>
    [RelayCommand]
    public void PageSuivante()
    {
        if (PageActuelle < NombreTotalPages)
        {
            PageActuelle = PageActuelle + 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageOrdres();
        }
    }

    /// <summary>
    /// Revient aux 14 ordres précédents.
    /// </summary>
    [RelayCommand]
    public void PagePrecedente()
    {
        if (PageActuelle > 1)
        {
            PageActuelle = PageActuelle - 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageOrdres();
        }
    }

    /// <summary>
    /// Change l'onglet affiché : 0 pour Mes Ventes, 1 pour Mes Réservations.
    /// </summary>
    [RelayCommand]
    public void ChangerOnglet(string? indexStr)
    {
        if (int.TryParse(indexStr, out int index))
        {
            OngletActifIndex = index;
            PageActuelle = 1;
            SaisiePage = "1";
            MettreAJourPageOrdres();
        }
    }

    /// <summary>
    /// Ouvre la fiche détaillée de la voiture lorsqu'on clique sur une offre dans Mes Ordres.
    /// </summary>
    [RelayCommand]
    public async Task VoirDetailsCarteAsync(Offre? offre)
    {
        if (offre == null || offre.Carte == null)
            return;

        if (Shell.Current != null)
        {
            var parametres = new Dictionary<string, object>
            {
                { "carte", offre.Carte },
                { "id", offre.Carte.Identifiant }
            };
            await Shell.Current.GoToAsync(nameof(FicheCartePage), parametres);
        }
    }

    #endregion
}
