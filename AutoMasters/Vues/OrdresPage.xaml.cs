using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class OrdresPage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public OrdresPage(OrdresVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public OrdresPage() : this(new OrdresVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
