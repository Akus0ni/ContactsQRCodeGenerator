# installer

WiX 6 MSI. Install, upgrade and uninstall.

```bash
dotnet publish src/ContactQR.App -c Release -r win-x64 --self-contained false -p:DebugType=none -o artifacts/publish
dotnet build installer/ContactQR.Installer.wixproj -c Release
# -> installer/bin/Release/ContactQR.msi
```

**The publish must run first.** `Package.wxs` harvests `artifacts/publish` with the `Files`
element; building the wixproj against a stale or missing publish silently ships the wrong
payload.

## Version

The MSI version comes from `<Version>` in the repository root `Directory.Build.props`, passed
through as the `ProductVersion` preprocessor variable. Do not hardcode a version here — the
number in Programs and Features, the assembly version and the release tag must stay the same
number, and one source of truth is how that is guaranteed.

`UpgradeCode` is fixed forever at `6f2a1c94-3d7e-4b16-9f8a-1c0d5e7b2a43`. **Never change it.**
It is what tells Windows that a new MSI is an upgrade of the installed product rather than a
different product, and changing it would leave every operator with two copies installed.

Bumping `<Version>` and merging to `main` publishes a release automatically — see the root
`CLAUDE.md`.

## Decisions worth knowing

- **No in-application update check, by design.** PRD FR-8.2 bans it — an updater is a network
  client, and the product's promise is that client data never leaves the machine. Updates are a
  new MSI the operator runs. `MajorUpgrade` makes that a clean in-place replace.
- **Uninstall deliberately leaves `%APPDATA%\ContactQR\library.db` in place.** It is the
  operator's own client records built up over years. Destroying it on uninstall would be
  indefensible, and an upgrade that reinstalls would silently wipe the book.
- **`Heat` is not used.** WiX 6 errors on it as deprecated. The `Files` element replaces it.
- **`InstallerPlatform` is x64** because the payload is win-x64 and `INSTALLFOLDER` sits under
  `ProgramFiles64Folder`. Without it every component is 32-bit and ICE80 fails the build.
- **ICE60 and ICE61 are suppressed with cause**, documented in the wixproj. ICE60 fires on
  SkiaSharp's native DLL; ICE61 is the accepted trade-off for `AllowSameVersionUpgrades`.

## Not done yet

**The MSI is unsigned.** PRD FR-8.7 requires a code-signing certificate, and without one
SmartScreen warns on every install — corrosive for a product whose pitch is trustworthiness.
This is a purchasing decision with real lead time (PRD Q2), not a build step.
