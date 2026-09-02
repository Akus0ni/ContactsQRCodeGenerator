# ContactQR.App

The WPF application. `net10.0-windows`. Everything here is presentation — no vCard rules, no
capacity maths, no colour thresholds. If a decision belongs to the product rather than to the
screen, it lives in `ContactQR.Core` or `ContactQR.Rendering`.

| Path | Purpose |
|---|---|
| `Theme/Tokens.xaml` | Colours, type and spacing from `DESIGN.md` §3. |
| `Theme/Controls.xaml` | Control styles from `DESIGN.md` §7. |
| `MainWindow.xaml` | Shell: header, status bar, and both primary views sharing one grid cell. |
| `Views/LibraryView.xaml` | The Library — search, list, open, duplicate, delete (`DESIGN.md` §6.2). |
| `Views/UnsafeExportDialog.xaml` | The FR-4.5 hard gate (`DESIGN.md` §6.4). |
| `ViewModels/ShellViewModel.cs` | Owns the library connection and moves between views. |
| `ViewModels/LibraryViewModel.cs` | Search, list, open, duplicate, delete. |
| `ViewModels/EditorViewModel.cs` | Live preview, debounce, budget, remedies, save, export. |
| `Converters/` | Token-name to brush, and an invertible boolean to visibility. |
| `Diagnostics/DiagnosticLog.cs` | The local log file. The only diagnostic channel this product has. |
| `Assets/appicon.ico` | Application icon. Generated, dark-on-light, 16–256 px. |

## Layout rules that carry meaning

- **The export button sits below the verdict and the remedies.** Neither the pointer nor the
  tab order can reach it without crossing the number the application exists to compute. This
  is design principle P1 expressed as layout; do not move it to the header.
- **The preview canvas is `#8C8C8C` and does not follow the theme.** A dark canvas flatters a
  light-background code and a white one flatters a dark one. It must stay a constant neutral
  reference field or the same QR looks differently trustworthy in each theme.
- **All greys are exactly neutral (R = G = B).** The operator judges a client's brand colours
  on our canvas; a tinted interface shifts his perception of them.
- **`RenderOptions.BitmapScalingMode="NearestNeighbor"` on the preview.** Smooth scaling would
  make a too-dense code look cleaner on screen than it prints, which is the exact lie the
  product exists to prevent.
- **The verdict never animates.** No cross-fade, no count-up. Instruments snap.
- **Offline is styled as confirmation, never a warning** — a filled green dot, not a struck
  cloud. It is the product promise being kept, and it never changes.

## Gotchas

- **`InvariantGlobalization` must stay `false` here.** WPF's binding engine resolves a specific
  culture per conversion and throws `Cannot find non-neutral culture related to 'en-us'` under
  invariant mode, before the first window appears. The repo root enables it for deterministic
  library behaviour; `Directory.Build.props` in this folder opts out. Numbers that must not
  vary by locale state `CultureInfo.InvariantCulture` explicitly.
- **`CA1515` is suppressed here** because WPF requires public partial classes for XAML
  code-behind. It stays enabled in the libraries.
- The view model calls `Regenerate()` in its constructor. Without it the panel opens blank and
  says nothing about what is missing, which contradicts P1.

## The export gate

`ExportPngCommand` enforces two gates that are not the same thing:

- **The module-size gate is overridable.** A `WillFail` verdict routes through
  `UnsafeExportDialog`, which requires a ticked acknowledgement, writes `_UNSAFE` into the
  filename, and records `UnsafeOverride = true` in the export log (FR-4.5).
- **A failed self-test is not overridable at all.** It means we drew something we cannot
  verify, which is never a judgement call to hand the operator. There is deliberately no path
  past it.
- **`ExceedsCapacity` is also not overridable** — there is no code to export, only a payload
  that will not encode.

Three frictions in the dialog are load-bearing and must not be "improved" away: the confirm
button is not default-focused, `Enter` does not activate it, and its label says
`Export unsafe code` rather than `Continue`. FR-4.5 and metric M7 both depend on this gate
staying believed, and a gate dismissible by muscle memory is equivalent to no gate.

## The log

`%APPDATA%\ContactQR\logs\contactqr-{date}.log`, beside the client library. One file per day,
deleted after 14 days.

This is the **only** channel by which a failure on the operator's machine can reach anyone. The
product may not report crashes over the network (FR-8.1, FR-8.2), so a failure that writes
nothing to disk is a failure nobody can act on. That is not hypothetical: 1.0.1 shipped an
installer whose application died during `Application.DoStartup`, and the entire available bug
report was "it does not open".

- **The handlers are wired in the `App` constructor, not `OnStartup`.** WPF loads the
  `StartupUri` window from inside `DoStartup`, so a broken resource, dictionary or binding
  throws before any override would run. That is precisely the failure that shipped.
- **`DiagnosticLog` never throws.** Every write failure is swallowed. A logger that can crash
  the application it is diagnosing is worse than no logger.
- **Its members are static** so they work from `AppDomain.UnhandledException`, which can fire
  when no object graph exists.
- **Log outcomes and decisions, not UI churn.** Export, override, self-test failure, save,
  delete, library open. Do not log keystrokes, preview regeneration or debounce ticks — the
  file has to stay readable by a person looking for one event.
- **Never write a vCard payload to the log.** The database already holds the snapshot
  (FR-7.7), and the log is the file an operator will attach to an email.

## The icon

Three QR finder patterns, dark marks on a light field, with a few accent-coloured data modules
in the empty quadrant. Deliberately simple — it has to stay legible at 16 px in a taskbar.

It is **dark-on-light on purpose.** The application blocks light-on-dark codes outright
(FR-5.8) because many decoders never attempt inversion, so an inverted icon would contradict
the product on its own taskbar.

**`<ApplicationIcon>` and the window icon are two different things, and this cost a release.**
`<ApplicationIcon>` only stamps the Win32 icon into the executable; it does not create an
MSBuild item, so nothing copies or embeds the file. `MainWindow` also asks WPF for that icon at
runtime, which needs a compiled `<Resource>` — and the missing resource was invisible in
development because `dotnet run` sets the working directory to the project folder, where the
file happens to sit on disk. Every other layout, including the MSI, crashed on launch.

The window icon is therefore referenced as `pack://application:,,,/Assets/appicon.ico`. The
explicit pack URI is the point: it can only resolve from assembly resources, so it cannot
silently fall back to a file next to the working directory and hide the same defect again.

## Not built yet

Export dialog with DPI and quiet-zone controls, logo file picking, colour pickers, payload
inspector, export-log view. Export goes straight to a save dialog. The print test sheet is
wired in and writes a PNG; sending it directly to a printer is not.
