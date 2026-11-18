using AutoMasters.Vues;
using Microsoft.Maui.Controls;

namespace AutoMasters;

public partial class AppShell : Shell
{
    #region Attributs

    #endregion

    #region Constructeurs

    public AppShell()
    {
        InitializeComponent();
        EnregistrerRoutes();
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    /// <summary>
    /// Enregistre les routes pour naviguer vers la fiche d'une voiture ou la page de mise en vente
    /// </summary>
    private void EnregistrerRoutes()
    {
        Routing.RegisterRoute(nameof(FicheCartePage), typeof(FicheCartePage));
        Routing.RegisterRoute("FicheCartePage", typeof(FicheCartePage));
        Routing.RegisterRoute(nameof(CreationOffrePage), typeof(CreationOffrePage));
        Routing.RegisterRoute("CreationOffrePage", typeof(CreationOffrePage));
    }

    #endregion
}
