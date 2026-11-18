using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

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
