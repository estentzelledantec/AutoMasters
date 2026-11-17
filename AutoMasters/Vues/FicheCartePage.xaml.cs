using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Page de consultation détaillée d'une carte automobile (Feature F22).
/// Respecte strictement la pureté du code-behind BTS SIO SLAM :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class FicheCartePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du ViewModel de fiche détaillée.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel injectée par le conteneur DI.</param>
    public FicheCartePage(FicheCarteVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur par défaut requis pour le système de navigation Shell.
    /// </summary>
    public FicheCartePage() : this(new FicheCarteVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
