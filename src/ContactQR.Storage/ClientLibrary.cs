using System.Text.Json;
using ContactQR.Core.Contacts;
using Microsoft.Data.Sqlite;

namespace ContactQR.Storage;

/// <summary>A stored client, with the identity and timestamps the library list needs.</summary>
public sealed record StoredClient
{
    /// <summary>Stable identifier for this record.</summary>
    public required Guid Id { get; init; }

    /// <summary>The client's contact details.</summary>
    public required ClientRecord Record { get; init; }

    /// <summary>When the record was first saved.</summary>
    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>When the record was last changed.</summary>
    public DateTimeOffset UpdatedAt { get; init; }

    /// <summary>When a PNG was last exported for this client, if ever.</summary>
    public DateTimeOffset? LastExportedAt { get; init; }

    /// <summary>The name shown in the library list.</summary>
    public string DisplayName => Record.FullName;
}

/// <summary>
/// The operator's local client library, stored in a single SQLite file.
/// </summary>
/// <remarks>
/// <para>
/// SQLite rather than loose JSON because it gives transactional writes — no half-saved record
/// after a crash — indexed search, and a single file to back up. Deletes are soft, so an
/// accidental one is recoverable.
/// </para>
/// <para>
/// The default location sits outside any sync root deliberately. A database inside OneDrive
/// can be locked mid-write or produce conflict copies, and this file becomes the operator's
/// client record of value within a year (PRD EC-19, FR-7.1).
/// </para>
/// </remarks>
public sealed class ClientLibrary : IDisposable
{
    private static readonly JsonSerializerOptions BackupFormat = new() { WriteIndented = true };

    private readonly SqliteConnection connection;

    /// <summary>Opens or creates a library at the given path.</summary>
    /// <param name="databasePath">Path to the SQLite file. Use <c>:memory:</c> for a transient library.</param>
    public ClientLibrary(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        connection = new SqliteConnection($"Data Source={databasePath}");
        connection.Open();
        CreateSchema();
    }

    /// <summary>The default library location, outside any cloud sync root.</summary>
    public static string DefaultPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ContactQR",
        "library.db");

    /// <summary>Saves a new client or updates an existing one.</summary>
    /// <param name="client">The record to save.</param>
    /// <param name="id">An existing identifier, or <see langword="null"/> to create a new record.</param>
    /// <returns>The identifier of the saved record.</returns>
    public Guid Save(ClientRecord client, Guid? id = null)
    {
        ArgumentNullException.ThrowIfNull(client);

        var identifier = id ?? Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO clients (id, payload, created_at, updated_at, deleted_at)
            VALUES ($id, $payload, $now, $now, NULL)
            ON CONFLICT(id) DO UPDATE SET
                payload = excluded.payload,
                updated_at = excluded.updated_at,
                deleted_at = NULL;
            """;
        command.Parameters.AddWithValue("$id", identifier.ToString());
        command.Parameters.AddWithValue("$payload", JsonSerializer.Serialize(client));
        command.Parameters.AddWithValue("$now", now.ToUnixTimeSeconds());
        command.ExecuteNonQuery();

        return identifier;
    }

    /// <summary>Returns every client that has not been deleted, most recently changed first.</summary>
    /// <returns>The stored clients.</returns>
    public IReadOnlyList<StoredClient> All() => Query("SELECT * FROM clients WHERE deleted_at IS NULL ORDER BY updated_at DESC", null);

    /// <summary>
    /// Finds clients whose name, company or email contains the search text.
    /// </summary>
    /// <param name="searchText">The text to look for. Empty returns everything.</param>
    /// <returns>The matching clients.</returns>
    /// <remarks>
    /// A substring match, not fuzzy. A library of a few hundred records does not need fuzzy
    /// matching, and a fuzzy match surfacing the wrong client is worse than no match at all.
    /// </remarks>
    public IReadOnlyList<StoredClient> Search(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return All();
        }

        return Query(
            "SELECT * FROM clients WHERE deleted_at IS NULL AND LOWER(payload) LIKE $term ORDER BY updated_at DESC",
            $"%{searchText.Trim().ToLowerInvariant()}%");
    }

    /// <summary>Soft-deletes a client, leaving it recoverable.</summary>
    /// <param name="id">The record to delete.</param>
    public void Delete(Guid id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE clients SET deleted_at = $now WHERE id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.ExecuteNonQuery();
    }

    /// <summary>Records that a PNG was exported for a client.</summary>
    /// <param name="id">The client exported.</param>
    public void RecordExport(Guid id)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "UPDATE clients SET last_exported_at = $now WHERE id = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        command.Parameters.AddWithValue("$now", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        command.ExecuteNonQuery();
    }

    /// <summary>
    /// Exports the whole library as JSON.
    /// </summary>
    /// <returns>A human-readable backup.</returns>
    /// <remarks>
    /// JSON rather than a database copy because it is readable and diffable without this
    /// application installed. That matters: the backup must survive the app (PRD FR-7.6).
    /// </remarks>
    public string ExportJson() =>
        JsonSerializer.Serialize(All(), BackupFormat);

    /// <inheritdoc />
    public void Dispose() => connection.Dispose();

    private void CreateSchema()
    {
        using var command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE IF NOT EXISTS clients (
                id               TEXT PRIMARY KEY,
                payload          TEXT NOT NULL,
                created_at       INTEGER NOT NULL,
                updated_at       INTEGER NOT NULL,
                last_exported_at INTEGER NULL,
                deleted_at       INTEGER NULL
            );
            CREATE INDEX IF NOT EXISTS idx_clients_updated ON clients (updated_at DESC);
            """;
        command.ExecuteNonQuery();
    }

    private List<StoredClient> Query(string sql, string? term)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;

        if (term is not null)
        {
            command.Parameters.AddWithValue("$term", term);
        }

        var results = new List<StoredClient>();
        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var record = JsonSerializer.Deserialize<ClientRecord>(reader.GetString(reader.GetOrdinal("payload")));

            if (record is null)
            {
                continue;
            }

            var lastExportedOrdinal = reader.GetOrdinal("last_exported_at");

            results.Add(new StoredClient
            {
                Id = Guid.Parse(reader.GetString(reader.GetOrdinal("id"))),
                Record = record,
                CreatedAt = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(reader.GetOrdinal("created_at"))),
                UpdatedAt = DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(reader.GetOrdinal("updated_at"))),
                LastExportedAt = reader.IsDBNull(lastExportedOrdinal)
                    ? null
                    : DateTimeOffset.FromUnixTimeSeconds(reader.GetInt64(lastExportedOrdinal)),
            });
        }

        return results;
    }
}
