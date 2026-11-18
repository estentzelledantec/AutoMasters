using System;
using System.Net.Http;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using AutoMasters.Services;
using Microsoft.Maui.Controls;

namespace AutoMasters.VuesModeles;

/// <summary>
/// ViewModel de la page de connexion
/// </summary>
public partial class ConnexionVueModele : ObservableObject
{
    #region Attributs

    private readonly ServiceAPI serviceApi;

    // [ObservableProperty] génère automatiquement la propriété publique Identifiant reliée au champ Entry en XAML
    [ObservableProperty]
    private string identifiant = "";

    // [ObservableProperty] génère automatiquement la propriété publique MotDePasse reliée au champ Entry en XAML
    [ObservableProperty]
    private string motDePasse = "";

    // Message d'erreur affiché en rouge sous le bouton si la connexion échoue
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AUnMessage))]
    private string message = "";

    // Indique si une requête de connexion est en cours pour afficher la roue de chargement
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstDisponible))]
    private bool estOccupe;

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur du ViewModel de connexion
    /// </summary>
    public ConnexionVueModele(ServiceAPI? serviceApi = null)
    {
        this.serviceApi = serviceApi ?? ServiceAPI.Instance;
    }

    #endregion

    #region Getter/Setter

    /// <summary>
    /// Permet de désactiver le bouton Se connecter pendant le chargement (inverse de EstOccupe)
    /// </summary>
    public bool EstDisponible => !EstOccupe;

    /// <summary>
    /// Indique s'il y a un message d'erreur à afficher à l'écran
    /// </summary>
    public bool AUnMessage => !string.IsNullOrWhiteSpace(Message);

    #endregion

    #region Methodes

    /// <summary>
    /// Vérifie les champs saisis, appelle l'API pour connecter le joueur et ouvre la page Catalogue
    /// </summary>
    // [RelayCommand] crée automatiquement la commande SeConnecterCommand utilisée par le bouton en XAML
    [RelayCommand]
    private async Task SeConnecterAsync()
    {
        if (EstOccupe)
            return;

        Message = "";

        // On vérifie que l'utilisateur a bien rempli son identifiant et son mot de passe
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
                // On vide le mot de passe par sécurité une fois connecté
                MotDePasse = string.Empty;
                Message = "";

                // Redirection vers l'onglet Catalogue
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
            // En cas d'erreur, on vide le mot de passe mais on garde l'identifiant saisi
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
