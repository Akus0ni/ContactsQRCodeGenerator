# ContactQR.Storage

The operator's local client library: one SQLite file, no server, no sync.

SQLite rather than loose JSON because it gives transactional writes — no half-saved record
after a crash — indexed search, and a single file to back up.

## Rules

- **Deletes are soft.** `deleted_at` is stamped and the row stays. Saving with the same id
  restores it. An accidental delete must be recoverable.
- **The default path is `%APPDATA%\ContactQR\library.db`, deliberately outside any sync root.**
  A database inside OneDrive can be locked mid-write or produce conflict copies, and this file
  becomes the operator's client record of value within a year (PRD EC-19).
- **`ExportJson` is the backup story, not a database copy.** It must stay readable and
  diffable without this application installed — the backup has to survive the app (FR-7.6).
- **Search is substring, not fuzzy.** A few hundred records do not need fuzzy matching, and a
  fuzzy match surfacing the wrong client is worse than no match.

## Schema note

The contact record is stored as a JSON payload in one column rather than as normalised
columns. That keeps `ClientRecord` free to change shape without a migration, at the cost of
querying inside JSON — acceptable at this scale, and revisit if the library ever grows past a
few thousand records.
