using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ContactQR.App.Diagnostics;
using ContactQR.Storage;

namespace ContactQR.App.ViewModels;

/// <summary>
/// The Library: search, browse, open, duplicate and delete clients.
/// </summary>
/// <remarks>
/// The home surface. Search is focused on arrival because the returning-client reprint is the
/// most frequent job and its whole time budget is spent here (DESIGN §6.2, PRD M3).
/// </remarks>
public sealed partial class LibraryViewModel : ObservableObject
{
    private readonly ClientLibrary library;

    [ObservableProperty]
    private string searchText = string.Empty;

    [ObservableProperty]
    private StoredClient? selectedClient;

    public LibraryViewModel(ClientLibrary library)
    {
        ArgumentNullException.ThrowIfNull(library);
        this.library = library;

        Refresh();
    }

    /// <summary>Raised when a client should be opened in the Editor.</summary>
    public event EventHandler<StoredClient>? OpenRequested;

    /// <summary>Raised when a new, empty client should be started.</summary>
    public event EventHandler? NewRequested;

    /// <summary>The clients currently listed, filtered by <see cref="SearchText"/>.</summary>
    public ObservableCollection<StoredClient> Clients { get; } = [];

    /// <summary>Whether the library holds no clients at all, as opposed to none matching a search.</summary>
    public bool IsEmpty => Clients.Count is 0 && string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Whether a search returned nothing.</summary>
    public bool HasNoMatches => Clients.Count is 0 && !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>The count shown in the status bar.</summary>
    public string CountLabel => Clients.Count is 1 ? "1 client" : $"{Clients.Count} clients";

    /// <summary>Re-reads the library and re-applies the current search.</summary>
    public void Refresh()
    {
        Clients.Clear();

        foreach (var client in library.Search(SearchText))
        {
            Clients.Add(client);
        }

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasNoMatches));
        OnPropertyChanged(nameof(CountLabel));
    }

    partial void OnSearchTextChanged(string value) => Refresh();

    [RelayCommand]
    private void NewClient() => NewRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void OpenClient(StoredClient? client)
    {
        if (client is not null)
        {
            OpenRequested?.Invoke(this, client);
        }
    }

    /// <summary>
    /// Copies a client's details into a new unsaved record with the name cleared.
    /// </summary>
    /// <remarks>
    /// Serves a specific recurring job: cards for three partners at the same firm, where the
    /// company details are shared and only the person changes (PRD FR-7.5).
    /// </remarks>
    [RelayCommand]
    private void DuplicateClient(StoredClient? client)
    {
        if (client is null)
        {
            return;
        }

        var copy = client.Record with { GivenName = "New", FamilyName = null };
        library.Save(copy);
        Refresh();
    }

    [RelayCommand]
    private void DeleteClient(StoredClient? client)
    {
        if (client is null)
        {
            return;
        }

        // Soft delete (PRD FR-7.2), but the operator does not see that distinction. Recording it
        // answers "a client vanished from my library" without guesswork.
        DiagnosticLog.Information($"Deleted client {client.Id} from the library.");

        library.Delete(client.Id);
        Refresh();
    }
}
