using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactQR.App.Diagnostics;
using ContactQR.Storage;

namespace ContactQR.App.ViewModels;

/// <summary>
/// Holds the two primary views and moves between them.
/// </summary>
/// <remarks>
/// The application opens on the Library, not the Editor. Opening an existing client outruns
/// starting a new one by roughly four to one on a reprint-heavy book, and the returning-client
/// job has the tightest time budget in the product (DESIGN §6.2, PRD M3).
/// </remarks>
public sealed partial class ShellViewModel : ObservableObject, IDisposable
{
    private readonly ClientLibrary library;

    [ObservableProperty]
    private bool isEditorVisible;

    public ShellViewModel()
        : this(new ClientLibrary(EnsureLibraryPath()))
    {
    }

    public ShellViewModel(ClientLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);

        this.library = library;
        Editor = new EditorViewModel(library);
        Library = new LibraryViewModel(library);

        Library.OpenRequested += (_, client) =>
        {
            Editor.Load(client);
            IsEditorVisible = true;
        };

        Library.NewRequested += (_, _) =>
        {
            Editor.StartNew();
            IsEditorVisible = true;
        };
    }

    /// <summary>The Editor view model.</summary>
    public EditorViewModel Editor { get; }

    /// <summary>The Library view model.</summary>
    public LibraryViewModel Library { get; }

    /// <inheritdoc />
    public void Dispose() => library.Dispose();

    [RelayCommand]
    private void BackToLibrary()
    {
        Library.Refresh();
        IsEditorVisible = false;
    }

    private static string EnsureLibraryPath()
    {
        var path = ClientLibrary.DefaultPath;
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        // Opening the library is the first thing that touches the filesystem, so it is the first
        // thing that can fail on a machine the application has never run on. Recording the path
        // it resolved to turns "it will not start" into a one-line answer.
        DiagnosticLog.Information($"Opening client library at {path}.");

        return path;
    }
}
