using System;
using System.Collections.Generic;
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
/// ViewModel gérant le Garage personnel du collectionneur, le Patrimoine, le Portefeuille en Jetons
/// et le déclencheur de boosters (Features F25, F26).
/// Gère la valorisation du Patrimoine en Jetons (Prix Réel / 1 000) et en Euros, le Portefeuille liquide en Jetons,
/// le P&L net coloré, le quota de boosters ("3/3 disponibles" avec recharge de +1 toutes les 30 minutes),
/// la vente directe contre des Jetons et l'ouverture du modal booster de 3 cartes.
/// </summary>
public partial class GarageVueModele : ObservableObject
{
    #region Attributs

    private const int DureeRechargeBoosterSecondes = 1800; // 30 minutes (1800 secondes)
    public const int TaillePage = 14;
    private readonly ServiceAPI serviceApi;
    private bool minuteurRechargeActif = false;
    private List<CartePossedee> toutesLesCartesPossedees = new List<CartePossedee>();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PatrimoineJetonsFormate))]
    [NotifyPropertyChangedFor(nameof(PatrimoineEurosFormate))]
    [NotifyPropertyChangedFor(nameof(PortefeuilleJetonsFormate))]
    [NotifyPropertyChangedFor(nameof(PortefeuilleEurosFormate))]
    private Portefeuille portefeuille = new Portefeuille();

    [ObservableProperty]
    private ObservableCollection<CartePossedee> cartesPossedees = new ObservableCollection<CartePossedee>();

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
    [NotifyPropertyChangedFor(nameof(ValeurTotaleFormatee))]
    [NotifyPropertyChangedFor(nameof(PatrimoineJetonsFormate))]
    [NotifyPropertyChangedFor(nameof(PatrimoineEurosFormate))]
    private decimal valeurTotale;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PortefeuilleJetonsFormate))]
    [NotifyPropertyChangedFor(nameof(PortefeuilleEurosFormate))]
    private decimal soldePortefeuille;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalGainPerteFormate))]
    [NotifyPropertyChangedFor(nameof(EstEnGain))]
    private decimal totalGainPerte;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TitreCollection))]
    private int nombreTotalCartes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuotaBoosterFormate))]
    [NotifyPropertyChangedFor(nameof(PeutOuvrirBooster))]
    [NotifyPropertyChangedFor(nameof(CompteAReboursBooster))]
    private int paquetsDisponibles = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(QuotaBoosterFormate))]
    [NotifyPropertyChangedFor(nameof(CompteAReboursBooster))]
    private int paquetsMax = 3;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CompteAReboursBooster))]
    private int tempsRestantSecondes = DureeRechargeBoosterSecondes; // 30:00 (30 minutes)

    [ObservableProperty]
    private bool estBoosterModalOuvert;

    [ObservableProperty]
    private BoosterVueModele? boosterVueModele;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    [NotifyPropertyChangedFor(nameof(PeutOuvrirBooster))]
    private bool estOccupe;

    [ObservableProperty]
    private string message = "";

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur par défaut utilisant l'instance Singleton de ServiceAPI.
    /// </summary>
    public GarageVueModele() : this(ServiceAPI.Instance)
    {
    }

    /// <summary>
    /// Constructeur avec injection du service HTTP.
    /// </summary>
    /// <param name="serviceApi">Instance de ServiceAPI.</param>
    public GarageVueModele(ServiceAPI? serviceApi)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
        this.serviceApi.DonneesMarcheModifiees += () => _ = ChargerGarageAsync();
        _ = ChargerGarageAsync();
        DemarrerMinuteurRechargeBooster();
    }

    #endregion

    #region Getter/Setter

    public bool EstDisponible => !EstOccupe;

    public bool PeutOuvrirBooster => PaquetsDisponibles > 0 && !EstOccupe;

    public string ValeurTotaleFormatee => $"{ValeurTotale:N0} coins";

    /// <summary>
    /// Valeur totale du Patrimoine (ensemble des voitures possédées) convertie en Jetons (Prix / 1 000).
    /// </summary>
    public string PatrimoineJetonsFormate => Carte.FormaterMontantJetons(Math.Round(ValeurTotale / Carte.TauxConversionJeton, 2));

    /// <summary>
    /// Valeur totale réelle en Euros de toutes les voitures du Patrimoine.
    /// </summary>
    public string PatrimoineEurosFormate => $"Valeur réelle : {ValeurTotale:N0} €";

    /// <summary>
    /// Argent liquide disponible en Jetons dans le Portefeuille pour acheter sur le Marché.
    /// </summary>
    public string PortefeuilleJetonsFormate => Carte.FormaterMontantJetons(Math.Round(SoldePortefeuille / Carte.TauxConversionJeton, 2));

    /// <summary>
    /// Équivalent en Euros du Portefeuille liquide.
    /// </summary>
    public string PortefeuilleEurosFormate => $"Liquide dispo ({SoldePortefeuille:N0} € éq.)";

    public bool EstEnGain => TotalGainPerte >= 0;

    /// <summary>
    /// Format strict validé par les tests F25_03, F25_B02 (+32 500 € P&L total) et F25_B03 (-2 000 € P&L total).
    /// </summary>
    public string TotalGainPerteFormate => $"{(EstEnGain ? "+" : "")}{TotalGainPerte:N0} € P&L total";

    public string QuotaBoosterFormate => $"{PaquetsDisponibles}/{PaquetsMax} disponibles";

    public string CompteAReboursBooster => PaquetsDisponibles >= PaquetsMax
        ? $"Recharge : +1 toutes les 30 min (Max {PaquetsMax}/{PaquetsMax})"
        : $"Prochain dans {(TempsRestantSecondes / 60):D2}:{(TempsRestantSecondes % 60):D2}";

    public string TitreCollection => $"MA COLLECTION · {NombreTotalCartes} CARTES";

    /// <summary>
    /// Indique s'il est possible de revenir à la page précédente du garage.
    /// </summary>
    public bool PeutAllerPagePrecedente => PageActuelle > 1;

    /// <summary>
    /// Indique s'il est possible d'avancer à la page suivante du garage.
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
    /// Démarre le minuteur de recharge automatique (+1 booster toutes les 30 minutes dans la limite de 3/3).
    /// </summary>
    private void DemarrerMinuteurRechargeBooster()
    {
        if (minuteurRechargeActif || Application.Current == null)
            return;

        minuteurRechargeActif = true;
        _ = Task.Run(async () =>
        {
            while (minuteurRechargeActif)
            {
                await Task.Delay(1000);
                if (PaquetsDisponibles < PaquetsMax)
                {
                    if (TempsRestantSecondes > 1)
                    {
                        TempsRestantSecondes--;
                    }
                    else
                    {
                        PaquetsDisponibles = Math.Min(PaquetsMax, PaquetsDisponibles + 1);
                        TempsRestantSecondes = DureeRechargeBoosterSecondes;
                    }
                }
                else
                {
                    TempsRestantSecondes = DureeRechargeBoosterSecondes;
                }
            }
        });
    }

    /// <summary>
    /// Charge l'intégralité du portefeuille de l'utilisateur depuis l'API REST.
    /// </summary>
    [RelayCommand]
    public async Task ChargerGarageAsync()
    {
        if (EstOccupe)
            return;

        EstOccupe = true;
        Message = "";

        try
        {
            var res = await serviceApi.RecupererPortefeuilleAsync();
            Portefeuille = res;
            toutesLesCartesPossedees = (res.CartesPossedees ?? new ObservableCollection<CartePossedee>()).ToList();

            if (toutesLesCartesPossedees.Count == 0)
            {
                NombreTotalPages = 1;
            }
            else
            {
                NombreTotalPages = (int)Math.Ceiling((double)toutesLesCartesPossedees.Count / TaillePage);
            }

            if (PageActuelle > NombreTotalPages)
            {
                PageActuelle = NombreTotalPages;
            }
            if (PageActuelle < 1)
            {
                PageActuelle = 1;
            }

            SaisiePage = PageActuelle.ToString();
            MettreAJourPageGarage();
            ValeurTotale = res.ValeurTotale;
            SoldePortefeuille = res.Solde;
            TotalGainPerte = res.TotalGainPerte;
            NombreTotalCartes = res.NombreTotalCartes;
        }
        catch (Exception ex)
        {
            Message = $"Erreur lors de la synchronisation du garage : {ex.Message}";
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
                MettreAJourPageGarage();
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
            MettreAJourPageGarage();
        }
        else
        {
            SaisiePage = PageActuelle.ToString();
        }
    }

    /// <summary>
    /// Affiche uniquement les 14 voitures du garage correspondant à la page actuelle.
    /// </summary>
    private void MettreAJourPageGarage()
    {
        int indexDepart = (PageActuelle - 1) * TaillePage;
        var pageCartes = toutesLesCartesPossedees
            .Skip(indexDepart)
            .Take(TaillePage)
            .ToList();

        CartesPossedees = new ObservableCollection<CartePossedee>(pageCartes);
    }

    /// <summary>
    /// Passe aux 14 véhicules suivants du garage.
    /// </summary>
    [RelayCommand]
    public void PageSuivante()
    {
        if (PageActuelle < NombreTotalPages)
        {
            PageActuelle = PageActuelle + 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageGarage();
        }
    }

    /// <summary>
    /// Revient aux 14 véhicules précédents du garage.
    /// </summary>
    [RelayCommand]
    public void PagePrecedente()
    {
        if (PageActuelle > 1)
        {
            PageActuelle = PageActuelle - 1;
            SaisiePage = PageActuelle.ToString();
            MettreAJourPageGarage();
        }
    }

    /// <summary>
    /// Lance l'ouverture d'un booster et affiche la modale de révélation interactive uniquement après succès (F26, F32).
    /// </summary>
    [RelayCommand]
    public async Task OuvrirBoosterAsync()
    {
        if (EstOccupe)
            return;

        if (PaquetsDisponibles <= 0)
        {
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Quota épuisé",
                    $"Vous n'avez plus de booster disponible. {CompteAReboursBooster}.",
                    "OK");
            }
            return;
        }

        EstOccupe = true;
        try
        {
            var vmBooster = new BoosterVueModele(serviceApi);
            await vmBooster.DemarrerOuvertureAsync();

            if (!string.IsNullOrEmpty(vmBooster.Message) || vmBooster.CarteActuelle == null)
            {
                string msgErreur = string.IsNullOrEmpty(vmBooster.Message)
                    ? "Impossible de récupérer les cartes du booster."
                    : vmBooster.Message;

                if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
                {
                    await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                        "Erreur Booster",
                        msgErreur,
                        "OK");
                }
                return;
            }

            bool aTraiteFin = false;
            vmBooster.SurBoosterTermine += async () =>
            {
                if (aTraiteFin)
                    return;
                aTraiteFin = true;

                EstBoosterModalOuvert = false;
                bool etaitAuMaximum = PaquetsDisponibles >= PaquetsMax;
                PaquetsDisponibles = Math.Max(0, PaquetsDisponibles - 1);
                if (etaitAuMaximum)
                {
                    TempsRestantSecondes = DureeRechargeBoosterSecondes;
                }
                await ChargerGarageAsync();
            };

            BoosterVueModele = vmBooster;
            EstBoosterModalOuvert = true;
        }
        catch (Exception ex)
        {
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Erreur Booster",
                    $"Une erreur est survenue lors de l'ouverture : {ex.Message}",
                    "OK");
            }
        }
        finally
        {
            EstOccupe = false;
        }
    }

    /// <summary>
    /// Ferme la modale booster.
    /// </summary>
    [RelayCommand]
    public void FermerBoosterModal()
    {
        EstBoosterModalOuvert = false;
    }

    /// <summary>
    /// Ouvre la fiche détaillée de la voiture sélectionnée dans le garage.
    /// </summary>
    [RelayCommand]
    public async Task VoirDetailsCarteAsync(CartePossedee? exemplaire)
    {
        if (exemplaire == null || exemplaire.Carte == null)
            return;

        if (Shell.Current != null)
        {
            var parametres = new System.Collections.Generic.Dictionary<string, object>
            {
                { "carte", exemplaire.Carte },
                { "id", exemplaire.Carte.Identifiant }
            };
            await Shell.Current.GoToAsync(nameof(FicheCartePage), parametres);
        }
    }

    /// <summary>
    /// Raccourci contextuel pour mettre en vente une carte sur le marché public depuis le garage.
    /// </summary>
    [RelayCommand]
    public async Task VendreCarteAsync(CartePossedee? carte)
    {
        if (carte == null) return;

        if (serviceApi.ExemplaireEstEnVente(carte))
        {
            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Déjà en vente",
                    "Cette voiture est déjà en vente sur le marché.",
                    "OK");
            }
            return;
        }

        if (Shell.Current != null)
        {
            var parametres = new System.Collections.Generic.Dictionary<string, object>
            {
                { "exemplaire", carte }
            };
            await Shell.Current.GoToAsync(nameof(CreationOffrePage), parametres);
        }
    }

    /// <summary>
    /// Retire l'annonce de vente active d'un véhicule du garage afin qu'il redevienne libre (EF032).
    /// </summary>
    [RelayCommand]
    public async Task RetirerVenteCarteAsync(CartePossedee? exemplaire)
    {
        if (exemplaire == null || EstOccupe)
            return;

        string cle = exemplaire.IdentifiantExemplaire ?? exemplaire.Identifiant;
        bool succes = await serviceApi.RetirerOffreAsync(cle);
        if (succes)
        {
            await ChargerGarageAsync();
        }
    }

    /// <summary>
    /// Vend immédiatement la voiture sélectionnée du Garage contre sa valeur en Jetons (Prix / 1 000).
    /// Les Jetons sont crédités dans le Portefeuille liquide pour acheter des voitures sur le Marché.
    /// </summary>
    [RelayCommand]
    public async Task VendreCarteDirecteAsync(CartePossedee? carte)
    {
        if (carte == null || EstOccupe)
            return;

        string nomVoiture = carte.Carte?.NomComplet ?? "cette voiture";
        string valeurJetons = carte.ValeurJetonsFormatee;
        string valeurEuros = carte.ValeurActuelleFormatee;

        if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
        {
            bool confirmer = await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                "Vendre contre des Jetons",
                $"Voulez-vous vendre {nomVoiture} ({valeurEuros}) pour créditer +{valeurJetons} directement dans votre Portefeuille ?",
                "Vendre",
                "Annuler");

            if (!confirmer)
                return;
        }

        try
        {
            decimal jetonsRecus = await serviceApi.VendreCarteGarageAsync(carte);
            await ChargerGarageAsync();

            if (Application.Current?.Windows.Count > 0 && Application.Current.Windows[0].Page != null)
            {
                await Application.Current.Windows[0].Page!.DisplayAlertAsync(
                    "Vente effectuée",
                    $"+{Carte.FormaterMontantJetons(jetonsRecus)} ont été crédités dans votre Portefeuille liquide ! Vous pouvez utiliser cet argent pour acheter des voitures sur le Marché.",
                    "Parfait");
            }
        }
        catch (Exception ex)
        {
            Message = $"Erreur lors de la vente : {ex.Message}";
        }
    }

    /// <summary>
    /// Offre un exemplaire possédé à un autre compte étudiant (Alice, Bob ou Charlie) conformément à la règle RM009 et l'exigence EF060.
    /// </summary>
    /// <param name="exemplaire">Exemplaire de voiture à offrir en cadeau.</param>
    [RelayCommand]
    public async Task OffrirCarteAsync(CartePossedee? exemplaire)
    {
        if (exemplaire == null || EstOccupe)
            return;

        Page? pageActive = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;
        if (pageActive == null)
            return;

        // Règle EF061 : empêcher le don si la carte est actuellement sur le marché
        if (serviceApi.ExemplaireEstEnVente(exemplaire))
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
            $"🎁 Offrir {exemplaire.Carte?.NomComplet ?? "cette voiture"} à :",
            "Annuler",
            null,
            destinatairesPossibles);

        if (string.IsNullOrWhiteSpace(action) || action == "Annuler")
            return;

        string destinataire = action.ToLowerInvariant();

        // Écran de confirmation obligatoire selon l'exigence EF060 du cahier des charges
        bool confirmer = await pageActive.DisplayAlertAsync(
            "Confirmation du cadeau (RM009)",
            $"Confirmez-vous le don de votre {exemplaire.Carte?.NomComplet} à {action.ToUpper()} ?\n\nCe transfert est définitif et sans contrepartie financière.",
            "Offrir la voiture",
            "Annuler");

        if (!confirmer)
            return;

        EstOccupe = true;
        try
        {
            bool succes = await serviceApi.OffrirCarteAsync(exemplaire, destinataire);
            if (succes)
            {
                await ChargerGarageAsync();
                await pageActive.DisplayAlertAsync(
                    "Cadeau envoyé !",
                    $"🎉 Votre {exemplaire.Carte?.NomComplet} a été transmise avec succès au garage de {action.ToUpper()} !",
                    "OK");
            }
            else
            {
                await pageActive.DisplayAlertAsync("Erreur", "Le transfert de la voiture n'a pas pu aboutir.", "OK");
            }
        }
        catch (Exception ex)
        {
            await pageActive.DisplayAlertAsync("Erreur", $"Impossible d'offrir la carte : {ex.Message}", "OK");
        }
        finally
        {
            EstOccupe = false;
        }
    }

    #endregion
}
