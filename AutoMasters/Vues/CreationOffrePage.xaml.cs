using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind pur pour la page de création d'offre de vente (Feature F24).
/// Conforme strictement au principe de pureté MVVM BTS SIO :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class CreationOffrePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du ViewModel de création d'offre.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel injectée par le conteneur DI.</param>
    public CreationOffrePage(CreationOffreVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur sans paramètre requis pour le système de navigation Shell.
    /// </summary>
    public CreationOffrePage() : this(new CreationOffreVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
