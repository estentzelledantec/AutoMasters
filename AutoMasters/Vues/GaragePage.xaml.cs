using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur pour la page Garage &amp; Portfolio (Feature F25).
/// Conforme strictement au principe de pureté MVVM BTS SIO :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class GaragePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du ViewModel de garage.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel injectée par le conteneur DI.</param>
    public GaragePage(GarageVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur sans paramètre requis pour le système de navigation Shell.
    /// </summary>
    public GaragePage() : this(new GarageVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
