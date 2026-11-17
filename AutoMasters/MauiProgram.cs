using Microsoft.Extensions.Logging;
using AutoMasters.Services;
using AutoMasters.Vues;
using AutoMasters.VuesModeles;

namespace AutoMasters;

/// <summary>
/// Point d'entrée de configuration de l'application .NET MAUI.
/// Configure les polices, la journalisation et le conteneur d'injection de dépendances (DI).
/// Conforme au standard BTS SIO SLAM 2ème année.
/// </summary>
public static class MauiProgram
{
    #region Attributs

    #endregion

    #region Constructeurs

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    public static MauiApp CreateMauiApp()
    {
        var constructeur = MauiApp.CreateBuilder();
        constructeur
            .UseMauiApp<App>()
            .ConfigureFonts(polices =>
            {
                polices.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                polices.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        constructeur.Logging.AddDebug();
#endif

        // =====================================================================
        // ENREGISTREMENT DE L'INJECTION DE DÉPENDANCES (DI CONTAINER) (F28)
        // =====================================================================

        // 1. Services : ServiceAPI singleton unique
        constructeur.Services.AddSingleton<ServiceAPI>(sp => ServiceAPI.Instance);

        // 2. Coquille de navigation Shell
        constructeur.Services.AddSingleton<AppShell>();

        // 3. ViewModels
        constructeur.Services.AddTransient<ConnexionVueModele>();
        constructeur.Services.AddTransient<CatalogueVueModele>();
        constructeur.Services.AddTransient<FicheCarteVueModele>();
        constructeur.Services.AddTransient<MarcheVueModele>();
        constructeur.Services.AddTransient<CreationOffreVueModele>();
        constructeur.Services.AddTransient<GarageVueModele>();
        constructeur.Services.AddTransient<BoosterVueModele>();
        constructeur.Services.AddTransient<OrdresVueModele>();
        constructeur.Services.AddTransient<ActiviteVueModele>();

        // 4. Vues (Pages)
        constructeur.Services.AddTransient<ConnexionPage>();
        constructeur.Services.AddTransient<CataloguePage>();
        constructeur.Services.AddTransient<FicheCartePage>();
        constructeur.Services.AddTransient<MarchePage>();
        constructeur.Services.AddTransient<CreationOffrePage>();
        constructeur.Services.AddTransient<GaragePage>();
        constructeur.Services.AddTransient<OrdresPage>();
        constructeur.Services.AddTransient<ActivitePage>();

        return constructeur.Build();
    }

    #endregion
}
