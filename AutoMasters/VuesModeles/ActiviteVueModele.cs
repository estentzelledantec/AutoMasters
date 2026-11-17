using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel dédié au journal d'Activité et des transactions du collectionneur (Onglet 5).
/// Affiche l'historique horodaté des ouvertures de boosters, achats, mises en vente,
/// réservations et dons, avec filtrage par catégorie et indicateurs statistiques.
/// Conforme au standard BTS SIO SLAM 2ème année (4 régions C# obligatoires).
/// </summary>
public partial class ActiviteVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI _serviceApi;

    [ObservableProperty]
    private ObservableCollection<TransactionActivite> _activites = new();

    [ObservableProperty]
    private string _filtreSelectionne = "TOUT";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool _estOccupe;

    [ObservableProperty]
    private string _message = string.Empty;

    [ObservableProperty]
    private int _nombreTotalOperations;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VolumeTotalFormate))]
    [NotifyPropertyChangedFor(nameof(VolumeTotalJetonsFormate))]
    private decimal _volumeTotalCoins;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant l'instance unique ServiceAPI.Instance.
    /// </summary>
    public ActiviteVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur permettant de passer le service API en paramètre.
    /// </summary>
    public ActiviteVueModele(ServiceAPI? serviceApi)
    {
        _serviceApi = serviceApi ?? ServiceAPI.Instance;
        _serviceApi.DonneesMarcheModifiees += () => _ = ChargerActivitesAsync();
        _ = ChargerActivitesAsync();
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public string VolumeTotalFormate => $"{VolumeTotalCoins:N0} coins";

    public string VolumeTotalJetonsFormate => Carte.FormaterMontantJetons(Math.Round(VolumeTotalCoins / Carte.TauxConversionJeton, 2));

    public string UtilisateurActuel => _serviceApi.UtilisateurConnecte.ToUpperInvariant();

    #endregion

    #region Methodes

    /// <summary>
    /// Charge et filtre le journal d'activité depuis ServiceAPI.
    /// </summary>
    [RelayCommand]
    public async Task ChargerActivitesAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = string.Empty;

        try
        {
            var toutes = await _serviceApi.RecupererActivitesAsync();
            NombreTotalOperations = toutes.Count;
            VolumeTotalCoins = toutes.Sum(a => a.Montant);

            var filtrees = string.Equals(FiltreSelectionne, "TOUT", StringComparison.OrdinalIgnoreCase)
                ? toutes
                : toutes.Where(a => string.Equals(a.TypeOperation, FiltreSelectionne, StringComparison.OrdinalIgnoreCase)).ToList();

            Activites = new ObservableCollection<TransactionActivite>(filtrees);
            OnPropertyChanged(nameof(UtilisateurActuel));
        }
        catch (Exception ex)
        {
            Message = $"Erreur lors du chargement de l'activité : {ex.Message}";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Filtre l'historique par type d'opération ("TOUT", "BOOSTER", "ACHAT", "MISE EN VENTE", "RÉSERVATION").
    /// </summary>
    [RelayCommand]
    public async Task FiltrerParTypeAsync(string? typeOp)
    {
        FiltreSelectionne = string.IsNullOrWhiteSpace(typeOp) ? "TOUT" : typeOp.Trim();
        await ChargerActivitesAsync();
    }

    /// <summary>
    /// Déconnecte l'utilisateur et renvoie vers la page de connexion.
    /// </summary>
    [RelayCommand]
    public async Task SeDeconnecterAsync()
    {
        await _serviceApi.LogoutAsync();
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//ConnexionPage");
        }
    }

    #endregion
}
