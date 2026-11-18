using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class FicheCartePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public FicheCartePage(FicheCarteVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public FicheCartePage() : this(new FicheCarteVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
