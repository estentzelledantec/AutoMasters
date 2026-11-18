using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class MarchePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public MarchePage(MarcheVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public MarchePage() : this(new MarcheVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
