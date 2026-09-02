using System.Globalization;
using System.IO;
using System.Text;

namespace ContactQR.App.Diagnostics;

/// <summary>
/// A plain text log written beside the client library, for diagnosing failures on the operator's
/// own machine.
/// </summary>
/// <remarks>
/// <para>
/// This exists because version 1.0.1 shipped an installer whose application could not start: a
/// XAML resource was missing from the packaged output, and the process died before any window
/// appeared, leaving the operator with nothing to report but "it does not open". A local log is
/// the only diagnostic channel available to a product that may not touch the network
/// (PRD FR-8.1), so it is not optional infrastructure.
/// </para>
/// <para>
/// Members are static deliberately. The log has to be usable from
/// <see cref="AppDomain.UnhandledException"/>, which can fire before or after any object graph
/// exists, so it cannot depend on one being constructed.
/// </para>
/// <para>
/// Nothing here ever throws. A logger that can crash the application it is diagnosing is worse
/// than no logger, so every failure to write is swallowed.
/// </para>
/// </remarks>
internal static class DiagnosticLog
{
    private static readonly Lock WriteGate = new();

    /// <summary>
    /// Logs older than this are deleted at startup. Long enough to cover "it broke sometime last
    /// week", short enough that the folder never needs managing.
    /// </summary>
    private static readonly TimeSpan Retention = TimeSpan.FromDays(14);

    /// <summary>The folder holding the log files.</summary>
    internal static string DirectoryPath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ContactQR",
        "logs");

    /// <summary>The file the current session writes to. One file per day.</summary>
    internal static string CurrentFilePath =>
        Path.Combine(
            DirectoryPath,
            FormattableString.Invariant($"contactqr-{DateTime.Now:yyyy-MM-dd}.log"));

    /// <summary>Records something that happened.</summary>
    internal static void Information(string message) => Append("INFO ", message);

    /// <summary>Records something that did not work but did not stop the operator.</summary>
    internal static void Warning(string message) => Append("WARN ", message);

    /// <summary>Records a failure, with the exception detail needed to act on it.</summary>
    internal static void Failure(string message, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        // ToString rather than Message: the message alone almost never identifies the cause. The
        // launch failure this class was written for reported "Cannot locate resource" only in the
        // inner exception, several frames down.
        Append("ERROR", $"{message}{Environment.NewLine}{exception}");
    }

    /// <summary>
    /// Opens the log for a new session and records the environment a failure would need to be
    /// interpreted against.
    /// </summary>
    internal static void RecordSessionStart()
    {
        DeleteExpiredLogs();

        var assembly = typeof(DiagnosticLog).Assembly;

        Append("INFO ", string.Join(
            Environment.NewLine,
            "--- session start ---",
            $"    version    {assembly.GetName().Version}",
            $"    executable {Environment.ProcessPath}",
            $"    directory  {AppContext.BaseDirectory}",
            $"    working    {Environment.CurrentDirectory}",
            $"    runtime    {Environment.Version}",
            $"    os         {Environment.OSVersion.VersionString}",
            $"    culture    {CultureInfo.CurrentCulture.Name}"));
    }

    /// <summary>Records the session ending normally, so a missing line means a hard crash.</summary>
    internal static void RecordSessionEnd(int exitCode) =>
        Append("INFO ", $"--- session end, exit code {exitCode} ---");

    private static void Append(string level, string message)
    {
        var line = new StringBuilder()
            .Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz", CultureInfo.InvariantCulture))
            .Append("  ")
            .Append(level)
            .Append("  ")
            .Append(message)
            .Append(Environment.NewLine)
            .ToString();

        try
        {
            lock (WriteGate)
            {
                Directory.CreateDirectory(DirectoryPath);
                File.AppendAllText(CurrentFilePath, line, Encoding.UTF8);
            }
        }
        catch (Exception writeFailure) when (
            writeFailure is IOException
                or UnauthorizedAccessException
                or NotSupportedException)
        {
            // Nowhere to report this to — reporting it is what just failed.
        }
    }

    private static void DeleteExpiredLogs()
    {
        try
        {
            if (!Directory.Exists(DirectoryPath))
            {
                return;
            }

            var expiredBefore = DateTime.Now - Retention;

            foreach (var path in Directory.EnumerateFiles(DirectoryPath, "contactqr-*.log"))
            {
                if (File.GetLastWriteTime(path) < expiredBefore)
                {
                    File.Delete(path);
                }
            }
        }
        catch (Exception pruneFailure) when (
            pruneFailure is IOException
                or UnauthorizedAccessException)
        {
            // A log folder that cannot be tidied is not a reason to refuse to start.
        }
    }
}
