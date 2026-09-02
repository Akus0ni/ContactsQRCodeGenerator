# ContactQR.App

The WPF application. `net10.0-windows`. Everything here is presentation — no vCard rules, no
capacity maths, no colour thresholds. If a decision belongs to the product rather than to the
screen, it lives in `ContactQR.Core` or `ContactQR.Rendering`.

| Path | Purpose |
|---|---|
| `Theme/Tokens.xaml` | Colours, type and spacing from `DESIGN.md` §3. |
| `Theme/Controls.xaml` | Control styles from `DESIGN.md` §7. |
| `MainWindow.xaml` | The Editor: form / canvas / scannability rail (`DESIGN.md` §6.1). |
| `ViewModels/EditorViewModel.cs` | Live preview, debounce, budget, remedies, export. |
| `Converters/BrushKeyConverter.cs` | Resolves a token name to the brush that token names. |

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

## Not built yet

Library browse screen, export dialog with DPI and quiet-zone controls, unsafe-override
dialog, logo file picking, colour pickers, print test sheet, payload inspector. The export
path currently goes straight to a save dialog at 300 dpi.
