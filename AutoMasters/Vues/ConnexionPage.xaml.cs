using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class ConnexionPage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public ConnexionPage(ConnexionVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public ConnexionPage() : this(new ConnexionVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
