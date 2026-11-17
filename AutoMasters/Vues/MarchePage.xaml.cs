using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur pour la page Marché (Feature F22).
/// Conforme strictement au principe de pureté MVVM BTS SIO :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class MarchePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du ViewModel de marché.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel injectée par le conteneur DI.</param>
    public MarchePage(MarcheVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur sans paramètre requis pour le système de navigation Shell.
    /// </summary>
    public MarchePage() : this(new MarcheVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
