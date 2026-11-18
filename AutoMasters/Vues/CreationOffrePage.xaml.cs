using AutoMasters.VuesModeles;

namespace AutoMasters.Vues;

public partial class CreationOffrePage : ContentPage
{
    #region Attributs

    #endregion

    #region Constructeurs

    public CreationOffrePage(CreationOffreVueModele vueModele)
    {
        InitializeComponent();
        BindingContext = vueModele;
    }

    public CreationOffrePage() : this(new CreationOffreVueModele())
    {
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    #endregion
}
