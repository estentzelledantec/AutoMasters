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

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel de la page de détails d'une voiture
/// </summary>
// [QueryProperty] permet de récupérer la voiture sélectionnée envoyée lors du changement de page avec Shell.Current.GoToAsync
[QueryProperty(nameof(CarteSelectionnee), "carte")]
[QueryProperty(nameof(CarteId), "id")]
public partial class FicheCarteVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    private string carteId = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitrePage))]
    [NotifyPropertyChangedFor(nameof(Marque))]
    [NotifyPropertyChangedFor(nameof(Modele))]
    [NotifyPropertyChangedFor(nameof(Variante))]
    [NotifyPropertyChangedFor(nameof(ModeleComplet))]
    [NotifyPropertyChangedFor(nameof(Annee))]
    [NotifyPropertyChangedFor(nameof(Puissance))]
    [NotifyPropertyChangedFor(nameof(PuissanceFormatee))]
    [NotifyPropertyChangedFor(nameof(VitesseMax))]
    [NotifyPropertyChangedFor(nameof(VitesseMaxFormatee))]
    [NotifyPropertyChangedFor(nameof(ZeroACent))]
    [NotifyPropertyChangedFor(nameof(ZeroACentFormate))]
    [NotifyPropertyChangedFor(nameof(Rarete))]
    [NotifyPropertyChangedFor(nameof(PrixOfficiel))]
    [NotifyPropertyChangedFor(nameof(PrixReelFormate))]
    [NotifyPropertyChangedFor(nameof(ValeurJetonsFormatee))]
    [NotifyPropertyChangedFor(nameof(VariationCoursFormatee))]
    [NotifyPropertyChangedFor(nameof(EstVariationPositive))]
    [NotifyPropertyChangedFor(nameof(HorodatageCoursFormate))]
    [NotifyPropertyChangedFor(nameof(ExemplairesEnCirculation))]
    [NotifyPropertyChangedFor(nameof(ExemplairesEnCirculationFormate))]
    [NotifyPropertyChangedFor(nameof(ImageUrl))]
    [NotifyPropertyChangedFor(nameof(Description))]
    private Carte? carteSelectionnee;

    [ObservableProperty]
    private ObservableCollection<PointCotationMarche> historiqueCours = new();

    [ObservableProperty]
    private string coursMinFormate = "0 €";

    [ObservableProperty]
    private string coursMaxFormate = "0 €";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    [ObservableProperty]
    private string messageErreur = "";

    [ObservableProperty]
    private bool aErreur;

    [ObservableProperty]
    private bool estPossedeeDansGarage;

    [ObservableProperty]
    private CartePossedee? exemplairePossede;

    #endregion

    #region Constructeurs

    public FicheCarteVueModele() : this(ServiceAPI.Instance)
    {
    }

    public FicheCarteVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public string TitrePage => CarteSelectionnee != null
        ? $"{CarteSelectionnee.Marque} {CarteSelectionnee.Modele}"
        : "Fiche Véhicule";

    public string Marque => CarteSelectionnee?.Marque ?? "";

    public string Modele => CarteSelectionnee?.Modele ?? "";

    public string? Variante => CarteSelectionnee?.Variante;

    public string ModeleComplet => string.IsNullOrWhiteSpace(Variante)
        ? Modele
        : $"{Modele} ({Variante})";

    public int Annee => CarteSelectionnee?.Annee ?? 0;

    public int Puissance => CarteSelectionnee?.Puissance ?? 0;

    public string PuissanceFormatee => CarteSelectionnee?.PuissanceFormatee ?? $"{Puissance} ch";

    public int VitesseMax => CarteSelectionnee?.VitesseMax ?? 0;

    public string VitesseMaxFormatee => CarteSelectionnee?.VitesseMaxFormatee ?? $"{VitesseMax} km/h";

    public double ZeroACent => CarteSelectionnee?.ZeroACent ?? 0.0;

    public string ZeroACentFormate => CarteSelectionnee?.ZeroACentFormate ?? $"{ZeroACent:F1} s";

    public string Rarete => CarteSelectionnee?.Rarete ?? "COMMUNE";

    public decimal PrixOfficiel => CarteSelectionnee?.PrixOfficiel ?? 0m;

    public string PrixReelFormate => CarteSelectionnee?.PrixReelFormate ?? $"{PrixOfficiel:N0} €";

    public string ValeurJetonsFormatee => CarteSelectionnee?.ValeurJetonsFormatee ?? Carte.FormaterMontantJetons(Math.Round(PrixOfficiel / Carte.TauxConversionJeton, 2));

    public string VariationCoursFormatee => CarteSelectionnee?.VariationCoursFormatee ?? "▲ +0,00 %";

    public bool EstVariationPositive => CarteSelectionnee?.EstVariationPositive ?? true;

    public string HorodatageCoursFormate => CarteSelectionnee?.HorodatageCoursFormate ?? $"Dernière cotation serveur : {DateTime.Now:dd/MM/yyyy à HH:mm:ss}";

    public int ExemplairesEnCirculation => CarteSelectionnee?.ExemplairesEnCirculation ?? 0;

    public string ExemplairesEnCirculationFormate => ExemplairesEnCirculation > 0
        ? $"{ExemplairesEnCirculation:N0} exemplaires en circulation"
        : "Tirage non spécifié";

    public string ImageUrl => CarteSelectionnee?.ImageUrl ?? "";

    public string Description => !string.IsNullOrWhiteSpace(CarteSelectionnee?.Description)
        ? CarteSelectionnee.Description
        : "Véhicule d'exception de la collection officielle AutoMasters.";

    #endregion

    #region Methodes

    // Charge automatiquement la courbe de marché dès qu'une carte est sélectionnée.
    partial void OnCarteSelectionneeChanged(Carte? value)
    {
        if (value != null)
        {
            VerifierPossessionGarage();
            _ = ChargerHistoriqueCoursAsync();
        }
        else
        {
            EstPossedeeDansGarage = false;
            ExemplairePossede = null;
        }
    }

    /// <summary>
    /// Vérifie si la voiture affichée fait partie du garage de l'étudiant connecté.
    /// </summary>
    private void VerifierPossessionGarage()
    {
        if (CarteSelectionnee == null)
        {
            EstPossedeeDansGarage = false;
            ExemplairePossede = null;
            return;
        }

        var garage = serviceApi.ObtenirGarageUtilisateurCourant();
        var match = garage.FirstOrDefault(cp =>
            string.Equals(cp.Identifiant, CarteSelectionnee.Identifiant, StringComparison.OrdinalIgnoreCase) ||
            (cp.Carte != null && string.Equals(cp.Carte.Identifiant, CarteSelectionnee.Identifiant, StringComparison.OrdinalIgnoreCase)) ||
            (cp.Carte != null && string.Equals(cp.Carte.Modele, CarteSelectionnee.Modele, StringComparison.OrdinalIgnoreCase) && string.Equals(cp.Carte.Marque, CarteSelectionnee.Marque, StringComparison.OrdinalIgnoreCase)));

        EstPossedeeDansGarage = match != null;
        ExemplairePossede = match;
    }

    /// <summary>
    /// Récupère l'historique des prix de la voiture depuis l'API pour afficher la courbe
    /// </summary>
    [RelayCommand]
    public async Task ChargerHistoriqueCoursAsync()
    {
        if (CarteSelectionnee == null)
            return;

        try
        {
            var points = await serviceApi.RecupererHistoriqueCoursAsync(CarteSelectionnee);
            HistoriqueCours = new ObservableCollection<PointCotationMarche>(points);

            if (points.Count > 0)
            {
                var cultureFr = System.Globalization.CultureInfo.GetCultureInfo("fr-FR");
                decimal minP = points.Min(p => p.Prix);
                decimal maxP = points.Max(p => p.Prix);
                CoursMinFormate = minP == Math.Truncate(minP)
                    ? string.Format(cultureFr, "{0:N0} €", minP)
                    : string.Format(cultureFr, "{0:N2} €", minP);
                CoursMaxFormate = maxP == Math.Truncate(maxP)
                    ? string.Format(cultureFr, "{0:N0} €", maxP)
                    : string.Format(cultureFr, "{0:N2} €", maxP);

                // Met à jour l'affichage du prix et de la date sur la fiche
                OnPropertyChanged(nameof(PrixOfficiel));
                OnPropertyChanged(nameof(PrixReelFormate));
                OnPropertyChanged(nameof(ValeurJetonsFormatee));
                OnPropertyChanged(nameof(VariationCoursFormatee));
                OnPropertyChanged(nameof(EstVariationPositive));
                OnPropertyChanged(nameof(HorodatageCoursFormate));
            }
        }
        catch
        {
            // Ignore si l'historique n'est pas disponible
        }
    }

    /// <summary>
    /// Permet d'offrir cette voiture en cadeau à un autre joueur (Alice, Bob ou Charlie)
    /// </summary>
    [RelayCommand]
    private async Task OffrirCarteAsync()
    {
        if (ExemplairePossede == null)
            return;

        Page? pageActive = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
        if (pageActive == null)
            return;

        if (serviceApi.ExemplaireEstEnVente(ExemplairePossede))
        {
            await pageActive.DisplayAlertAsync(
                "Voiture en vente",
                "Impossible d'offrir cette voiture : elle est actuellement mise en vente sur le marché. Veuillez retirer l'offre avant de l'offrir.",
                "OK");
            return;
        }

        string utilisateurActuel = serviceApi.UtilisateurConnecte?.ToLowerInvariant() ?? "bob";
        var destinatairesPossibles = new List<string> { "Alice", "Bob", "Charlie" }
            .Where(u => !string.Equals(u, utilisateurActuel, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        string action = await pageActive.DisplayActionSheetAsync(
            $"🎁 Offrir {CarteSelectionnee?.NomComplet ?? "cette voiture"} à :",
            "Annuler",
            null,
            destinatairesPossibles);

        if (string.IsNullOrWhiteSpace(action) || action == "Annuler")
            return;

        string destinataire = action.ToLowerInvariant();

        bool confirmer = await pageActive.DisplayAlertAsync(
            "Confirmation du cadeau",
            $"Confirmez-vous le don de votre {CarteSelectionnee?.NomComplet} à {action.ToUpper()} ?\n\nCe transfert est définitif et sans contrepartie financière.",
            "Offrir la voiture",
            "Annuler");

        if (!confirmer)
            return;

        try
        {
            bool succes = await serviceApi.OffrirCarteAsync(ExemplairePossede, destinataire);
            if (succes)
            {
                EstPossedeeDansGarage = false;
                ExemplairePossede = null;
                await pageActive.DisplayAlertAsync(
                    "Cadeau envoyé !",
                    $"🎉 Votre {CarteSelectionnee?.NomComplet} a été transmise avec succès au garage de {action.ToUpper()} !",
                    "OK");
            }
        }
        catch (Exception ex)
        {
            await pageActive.DisplayAlertAsync("Erreur", $"Impossible d'offrir la carte : {ex.Message}", "OK");
        }
    }

    partial void OnCarteIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && (CarteSelectionnee == null || CarteSelectionnee.Identifiant != value))
        {
            _ = ChargerDetailsAsync(value);
        }
    }

    /// <summary>
    /// Charge les informations complètes de la carte à partir de son identifiant
    /// </summary>
    public async Task ChargerDetailsAsync(string? id = null)
    {
        string cibleId = id ?? CarteId;
        if (string.IsNullOrWhiteSpace(cibleId))
            return;

        EstOccupe = true;
        MessageErreur = "";
        AErreur = false;

        try
        {
            var carteChargee = await serviceApi.RecupererFicheCarteAsync(cibleId);
            if (carteChargee != null)
            {
                CarteSelectionnee = carteChargee;
            }
        }
        catch (Exception ex)
        {
            MessageErreur = ex.Message;
            AErreur = true;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Commande publique de rechargement.
    /// </summary>
    [RelayCommand]
    private async Task ChargerDetails()
    {
        await ChargerDetailsAsync(CarteId);
    }

    /// <summary>
    /// Navigue vers la place de marché (MarchePage) avec filtre ciblé sur cette carte.
    /// </summary>
    [RelayCommand]
    private async Task VoirOffresMarcheAsync()
    {
        if (CarteSelectionnee == null && string.IsNullOrWhiteSpace(CarteId))
            return;

        string id = CarteSelectionnee?.Identifiant ?? CarteId;
        var parametres = new Dictionary<string, object>
        {
            { "cardId", id },
            { "search", CarteSelectionnee?.Marque ?? "" }
        };

        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("//MainTabs/Marche", parametres);
        }
    }

    /// <summary>
    /// Revient à la page précédente dans la pile de navigation.
    /// </summary>
    [RelayCommand]
    private async Task RetourAsync()
    {
        if (Shell.Current != null)
        {
            await Shell.Current.GoToAsync("..");
        }
    }

    #endregion
}
