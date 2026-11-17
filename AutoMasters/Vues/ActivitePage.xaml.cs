using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

/// <summary>
/// Code-behind strictement pur pour l'écran Activité (Onglet 5).
/// Toute la logique est déléguée à ActiviteVueModele conformément au pattern MVVM BTS SIO SLAM.
/// </summary>
public partial class ActivitePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public ActivitePage() : this(new ActiviteVueModele())
    {
    }

    public ActivitePage(ActiviteVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
