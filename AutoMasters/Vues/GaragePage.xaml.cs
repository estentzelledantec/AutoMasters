using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class GaragePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public GaragePage(GarageVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public GaragePage() : this(new GarageVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
