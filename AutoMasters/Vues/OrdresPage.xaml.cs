using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Page de gestion des ordres et réservations de l'utilisateur (Feature F33).
/// Respecte strictement la pureté du code-behind BTS SIO SLAM :
/// Constructeur avec InitializeComponent() uniquement, aucune logique métier ni appel HTTP.
/// </summary>
public partial class OrdresPage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Constructeur avec injection du ViewModel OrdresVueModele.
    /// </summary>
    /// <param name="vueModele">Instance du ViewModel injectée par le conteneur DI.</param>
    public OrdresPage(OrdresVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    /// <summary>
    /// Constructeur par défaut requis pour le système de navigation XAML.
    /// </summary>
    public OrdresPage() : this(new OrdresVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
