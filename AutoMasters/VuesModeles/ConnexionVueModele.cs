using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel de la page de connexion (Feature F19, F20).
/// Conforme aux standards BTS SIO SLAM 2ème année et CommunityToolkit.Mvvm.
/// Gère la validation des champs, l'appel à ServiceAPI.SeConnecterAsync,
/// la navigation Shell et l'effacement sécurisé du mot de passe en cas d'échec.
/// </summary>
public partial class ConnexionVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Identifiant))]
    private string email = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Password))]
    private string motDePasse = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMessage))]
    private string message = "";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    [NotifyPropertyChangedFor(nameof(IsBusy))]
    [NotifyPropertyChangedFor(nameof(IsNotBusy))]
    private bool estOccupe;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du service d'API.
    /// Utilise par défaut le singleton ServiceAPI.Instance.
    /// </summary>
    /// <param name="serviceApi">Instance du service API.</param>
    public ConnexionVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }

    #endregion

    #region Getter/Setter

    /// <summary>
    /// Indique si les commandes et contrôles d'action sont disponibles pour l'utilisateur.
    /// </summary>
    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Alias standard pour IsBusy.
    /// </summary>
    public bool IsBusy => EstOccupe;

    /// <summary>
    /// Alias standard pour IsNotBusy.
    /// </summary>
    public bool IsNotBusy => !EstOccupe;

    /// <summary>
    /// Indique si un message d'erreur ou d'information doit être affiché.
    /// </summary>
    public bool AUnMessage => !string.IsNullOrWhiteSpace(Message);

    /// <summary>
    /// Alias de compatibilité pour le champ Identifiant.
    /// </summary>
    public string Identifiant
    {
        get => Email;
        set => Email = value ?? "";
    }

    /// <summary>
    /// Alias de compatibilité pour le mot de passe.
    /// </summary>
    public string Password
    {
        get => MotDePasse;
        set => MotDePasse = value ?? "";
    }

    #endregion

    #region Methodes

    /// <summary>
    /// Commande asynchrone d'authentification.
    /// Valide les champs obligatoires et le format email côté client,
    /// interroge ServiceAPI, navigue vers le catalogue en cas de succès,
    /// ou efface le mot de passe en préservant l'email en cas d'échec.
    /// </summary>
    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        if (EstOccupe)
            return;

        Message = "";

        // 1. Validation de saisie obligatoire
        if (string.IsNullOrWhiteSpace(Identifiant) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            Message = "Veuillez saisir votre identifiant et votre mot de passe.";
            return;
        }

        string identifiantSaisi = Identifiant.Trim();

        EstOccupe = true;

        try
        {
            var reponse = await serviceApi.SeConnecterAsync(identifiantSaisi, MotDePasse);

            if (reponse != null && !string.IsNullOrWhiteSpace(reponse.JetonAcces))
            {
                // Succès : effacement sécurisé du mot de passe en mémoire
                MotDePasse = string.Empty;
                Message = "";

                // Navigation Shell vers les onglets principaux (Catalogue par défaut)
                if (Shell.Current != null)
                {
                    await Shell.Current.GoToAsync("//MainTabs/Catalogue");
                }
            }
            else
            {
                MotDePasse = string.Empty;
                Message = "Identifiants invalides. Vérifiez votre identifiant et votre mot de passe.";
            }
        }
        catch (ArgumentException)
        {
            MotDePasse = string.Empty;
            Message = "L'identifiant et le mot de passe sont obligatoires.";
        }
        catch (Exception ex)
        {
            // En cas d'échec : effacer le mot de passe mais conserver l'email (F19_B02)
            MotDePasse = string.Empty;

            string details = ex.Message ?? "";
            if (details.Contains("401") ||
                details.Contains("Unauthorized", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("invalide", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("refusée", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("refusee", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("expirée", StringComparison.OrdinalIgnoreCase) ||
                details.Contains("expiree", StringComparison.OrdinalIgnoreCase))
            {
                Message = "Identifiants invalides. Vérifiez votre identifiant et votre mot de passe.";
            }
            else if (ex is HttpRequestException ||
                     details.Contains("connexion", StringComparison.OrdinalIgnoreCase) ||
                     details.Contains("serveur", StringComparison.OrdinalIgnoreCase) ||
                     details.Contains("HttpRequestException", StringComparison.OrdinalIgnoreCase))
            {
                Message = "Impossible de joindre le serveur. Vérifiez votre connexion Internet.";
            }
            else
            {
                Message = details.Length > 0 ? details : "Une erreur est survenue lors de la connexion. Veuillez réessayer.";
            }
        }
        finally
        {
            EstOccupe = false;
        }
    }

    #endregion
}
