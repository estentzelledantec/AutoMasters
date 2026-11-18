using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class CataloguePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public CataloguePage(CatalogueVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public CataloguePage() : this(new CatalogueVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
