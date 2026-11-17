namespace AutoMasters;

public partial class App : Application
{
    #region Attributs

    #endregion

    #region Constructeurs

    public App()
    {
        InitializeComponent();
    }

    #endregion

    #region Getter/Setter

    #endregion

    #region Methodes

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    #endregion
}
