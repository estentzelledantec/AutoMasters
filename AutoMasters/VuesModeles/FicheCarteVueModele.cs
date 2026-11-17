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
/// ViewModel responsable de la fiche détaillée d'un véhicule (Fonctionnalité F22).
/// Reçoit la carte sélectionnée ou son identifiant via Shell QueryProperty,
/// charge les spécifications depuis ServiceAPI si nécessaire et navigue vers le marché.
/// Conforme aux conventions BTS SIO SLAM 2ème année.
/// </summary>
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
    [NotifyPropertyChangedFor(nameof(PrixOfficielFormate))]
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

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// Utilisé lors de l'instanciation de la vue.
    /// </summary>
    public FicheCarteVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP pour les tests unitaires.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
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

    public string PrixOfficielFormate => CarteSelectionnee?.PrixOfficielFormate ?? $"{PrixOfficiel:N0} coins";

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

    /// <summary>
    /// Charge automatiquement la courbe de marché dès qu'une carte est sélectionnée.
    /// </summary>
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
    /// Récupère l'historique des cotations boursières depuis l'API (/cards/{id}/quotes) et construit la courbe de marché (RM001).
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

                // Notifie l'interface si le dernier cours ou l'horodatage a été rafraîchi depuis l'API
                OnPropertyChanged(nameof(PrixOfficiel));
                OnPropertyChanged(nameof(PrixOfficielFormate));
                OnPropertyChanged(nameof(PrixReelFormate));
                OnPropertyChanged(nameof(ValeurJetonsFormatee));
                OnPropertyChanged(nameof(VariationCoursFormatee));
                OnPropertyChanged(nameof(EstVariationPositive));
                OnPropertyChanged(nameof(HorodatageCoursFormate));
            }
        }
        catch
        {
            // Conserve l'affichage de la fiche même si l'historique des cours n'est pas joignable
        }
    }

    /// <summary>
    /// Offre l'exemplaire possédé à un autre compte étudiant (Alice, Bob ou Charlie) conformément aux règles RM009 et EF060.
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
            "Confirmation du cadeau (RM009)",
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

    /// <summary>
    /// Réaction à la réception de l'identifiant de carte via Shell QueryProperty.
    /// Si l'objet complet n'est pas déjà présent en mémoire, lance le chargement API.
    /// </summary>
    partial void OnCarteIdChanged(string value)
    {
        if (!string.IsNullOrWhiteSpace(value) && (CarteSelectionnee == null || CarteSelectionnee.Identifiant != value))
        {
            _ = ChargerDetailsAsync(value);
        }
    }

    /// <summary>
    /// Récupère la fiche technique complète de la carte via son identifiant (F09).
    /// </summary>
    /// <param name="id">Identifiant de la carte à charger.</param>
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
