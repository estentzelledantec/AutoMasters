using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur de la page de connexion (ConnexionPage).
/// Respecte strictement la règle F28 : aucune logique métier, aucun appel réseau,
/// initialisation du BindingContext et des composants graphiques uniquement.
/// </summary>
public partial class ConnexionPage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection de dépendance du ViewModel.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel de connexion injectée par le conteneur DI.</param>
    public ConnexionPage(ConnexionVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur sans paramètre requis pour le système de templates XAML de MAUI et le routage Shell.
    /// </summary>
    public ConnexionPage() : this(new ConnexionVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
