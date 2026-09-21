using Microsoft.Maui;
using Microsoft.Maui.Controls;
using System;
using System.IO;
using System.Threading.Tasks;

namespace GreenRetail;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();

        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        return new Window(new AppShell());
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        LogCrash(e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        LogCrash(e.Exception);
        e.SetObserved();
    }

    internal static void LogCrash(Exception? exception)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GreenRetail");

            Directory.CreateDirectory(dir);

            var logPath = Path.Combine(dir, "crash.log");

            var message =
                $"[{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}] " +
                $"{exception?.GetType().FullName}: {exception?.Message}{Environment.NewLine}" +
                $"{exception?.StackTrace}{Environment.NewLine}{Environment.NewLine}";

            File.AppendAllText(logPath, message);
        }
        catch
        {
            // Logging must never crash the app.
        }
    }
}