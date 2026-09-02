using System.Windows;
using System.Windows.Threading;
using ContactQR.App.Diagnostics;

namespace ContactQR.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private const int CrashExitCode = 1;

    /// <summary>
    /// Installs the failure handlers before anything else runs.
    /// </summary>
    /// <remarks>
    /// The constructor, not <c>OnStartup</c>. WPF loads the window named by <c>StartupUri</c>
    /// from inside <c>Application.DoStartup</c>, and a failure there — a missing resource, a
    /// broken binding, a theme dictionary that will not parse — happens before any override
    /// would run. That is exactly the failure that shipped in 1.0.1, and it left no trace.
    /// </remarks>
    public App()
    {
        DiagnosticLog.RecordSessionStart();

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    /// <inheritdoc />
    protected override void OnExit(ExitEventArgs e)
    {
        ArgumentNullException.ThrowIfNull(e);

        DiagnosticLog.RecordSessionEnd(e.ApplicationExitCode);
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        DiagnosticLog.Failure("Unhandled exception on the UI thread.", e.Exception);

        // Handled, so the operator gets a message naming the log rather than the process
        // disappearing with no explanation — which is all 1.0.1 gave them.
        e.Handled = true;
        ReportAndShutDown(e.Exception);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception failure)
        {
            DiagnosticLog.Failure("Unhandled exception outside the UI thread.", failure);
            return;
        }

        DiagnosticLog.Warning($"Unhandled non-exception thrown: {e.ExceptionObject}");
    }

    private void ReportAndShutDown(Exception failure)
    {
        MessageBox.Show(
            $"ContactQR has to close.\n\n{failure.GetBaseException().Message}\n\n"
                + $"The full detail is in the log:\n{DiagnosticLog.CurrentFilePath}",
            "ContactQR",
            MessageBoxButton.OK,
            MessageBoxImage.Error);

        Shutdown(CrashExitCode);
    }
}
