using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Page de présentation du catalogue officiel AutoMasters (Feature F21).
/// Respecte strictement la pureté du code-behind BTS SIO SLAM :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class CataloguePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du ViewModel de catalogue.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel injectée par le conteneur DI.</param>
    public CataloguePage(CatalogueVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur par défaut requis pour le système de templates XAML.
    /// </summary>
    public CataloguePage() : this(new CatalogueVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
