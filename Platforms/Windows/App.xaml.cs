using Microsoft.UI.Xaml;

namespace GreenRetail.WinUI;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : global::Microsoft.Maui.MauiWinUIApplication
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        this.UnhandledException += OnUnhandledException;
    }

    protected override global::Microsoft.Maui.Hosting.MauiApp CreateMauiApp() 
        => global::GreenRetail.MauiProgram.CreateMauiApp();

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        base.OnLaunched(args);
    }

    private void OnUnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        try
        {
            global::GreenRetail.App.LogCrash(e.Exception);
            e.Handled = true;
        }
        catch
        {
            // Do not throw from an exception handler.
        }
    }
}