using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Modeles;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel gérant la création et publication d'une offre de vente sur le marché (Feature F24).
/// Permet la sélection d'un véhicule du garage, la saisie et validation stricte du prix demandé (> 0),
/// et l'envoi vers POST /offers.
/// </summary>
[QueryProperty(nameof(ExemplairePreselectionne), "exemplaire")]
public partial class CreationOffreVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private ObservableCollection<CartePossedee> cartesPossedees = new ObservableCollection<CartePossedee>();

    [ObservableProperty]
    private CartePossedee? carteSelectionnee;

    [ObservableProperty]
    private CartePossedee? exemplairePreselectionne;

    [ObservableProperty]
    private string prixSaisi = "";

    [ObservableProperty]
    private string messageErreur = "";

    [ObservableProperty]
    private bool garageEstVide;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// </summary>
    public CreationOffreVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
    public CreationOffreVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
        _ = ChargerVehiculesGarageAsync();
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    #endregion

    #region Methodes

    partial void OnExemplairePreselectionneChanged(CartePossedee? value)
    {
        if (value != null)
        {
            CarteSelectionnee = value;
        }
    }

    /// <summary>
    /// Charge les véhicules possédés du garage éligibles à la vente.
    /// </summary>
    [RelayCommand]
    public async Task ChargerVehiculesGarageAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        MessageErreur = "";

        try
        {
            var portefeuille = await serviceApi.RecupererPortefeuilleAsync();
            var eligibles = (portefeuille.CartesPossedees ?? new ObservableCollection<CartePossedee>())
                .Where(c => !serviceApi.ExemplaireEstEnVente(c))
                .ToList();
            CartesPossedees = new ObservableCollection<CartePossedee>(eligibles);
            GarageEstVide = CartesPossedees.Count == 0;

            if (GarageEstVide)
            {
                MessageErreur = "Votre garage est vide. Vous ne possédez aucun véhicule à mettre en vente.";
            }
            else if (ExemplairePreselectionne != null)
            {
                CarteSelectionnee = ExemplairePreselectionne;
            }
            else if (CarteSelectionnee == null && CartesPossedees.Count > 0)
            {
                CarteSelectionnee = CartesPossedees[0];
            }
        }
        catch (Exception ex)
        {
            MessageErreur = $"Impossible d'accéder au garage : {ex.Message}";
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Valide les informations saisies et publie l'offre sur le marché numérique (F24_03, F24_B02, F24_B03).
    /// </summary>
    [RelayCommand]
    public async Task CreerOffreAsync()
    {
        if (EstOccupe)
            return;

        MessageErreur = "";

        // 1. Validation de la sélection du véhicule (F24_01)
        if (CarteSelectionnee == null)
        {
            MessageErreur = "Veuillez sélectionner un véhicule de votre garage.";
            return;
        }

        // 2. Nettoyage et validation du format du prix (support virgules et espaces, F24_B02, F24_B03)
        string prixNettoye = (PrixSaisi ?? string.Empty).Replace(" ", string.Empty).Replace(',', '.');
        if (!decimal.TryParse(prixNettoye, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal prixFinal))
        {
            MessageErreur = "Veuillez saisir un montant numérique valide.";
            return;
        }

        // 3. Validation stricte du prix > 0 (F24_03, RM001)
        if (prixFinal <= 0)
        {
            MessageErreur = "Le prix de vente doit être strictement supérieur à zéro.";
            return;
        }

        EstOccupe = true;

        try
        {
            string copyId = !string.IsNullOrWhiteSpace(CarteSelectionnee.IdentifiantExemplaire)
                ? CarteSelectionnee.IdentifiantExemplaire
                : CarteSelectionnee.Identifiant;

            var offre = await serviceApi.CreerOffreAsync(copyId, prixFinal);

            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Offre publiée !",
                    $"Votre {CarteSelectionnee.Carte.Marque} {CarteSelectionnee.Carte.Modele} est en vente sur le marché au prix de {offre.PrixReelFormate} ({offre.ValeurJetonsFormatee}).",
                    "OK");
            }

            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync("..");
            }
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Annule la mise en vente et ferme la page.
    /// </summary>
    [RelayCommand]
    public async Task AnnulerAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    #endregion
}
