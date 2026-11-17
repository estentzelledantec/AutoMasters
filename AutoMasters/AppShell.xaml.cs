using AutoMasters.Vues;
using Microsoft.Maui.Controls;

namespace AutoMasters;

/// <summary>
/// Coquille principale de navigation Shell conforme aux exigences BTS SIO SLAM.
/// Respecte strictement la pureté du code-behind (F28) : aucune logique métier ni appel réseau.
/// </summary>
public partial class AppShell : Shell
{
    #region Attributs

    #endregion

    #region Constructeurs

    /// <summary>
    /// Initialise les composants graphiques Shell et enregistre les routes de détail.
    /// </summary>
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
    /// Enregistre les routes secondaires pour navigation modale ou détaillée.
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
