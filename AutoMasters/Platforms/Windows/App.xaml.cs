using Microsoft.UI.Xaml;

namespace AutoMasters.WinUI;

/// <summary>
/// Point de démarrage Windows fourni par le modèle MAUI.
/// </summary>
public partial class App : MauiWinUIApplication
{
	#region Attributs

	#endregion

	#region Constructeurs

	/// <summary>
	/// Initialise l'application Windows avant de créer l'application MAUI.
	/// </summary>
	public App()
	{
		this.InitializeComponent();
	}

	#endregion

	#region Getter/Setter

	#endregion

	#region Methodes

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	#endregion
}
