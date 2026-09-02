# ContactQR.Rendering

Encoding, drawing, PNG export and the decode-back self-test. Targets `net10.0` — no WPF, no
Windows-only API — so it stays testable headlessly.

| File | Purpose |
|---|---|
| `QrSymbol.cs` | `QrEncoder` (QRCoder) → a plain module matrix. The encode/render boundary. |
| `QrImageRenderer.cs` | Draws the matrix with SkiaSharp. Colours, logo compositing. |
| `PngDensityWriter.cs` | Writes the `pHYs` DPI chunk SkiaSharp omits. |
| `QrSelfTest.cs` | Decodes the rendered bitmap back (ZXing.Net) and compares. |
| `ColourContrast.cs` | Measures a brand colour pair; blocks inverted and low-contrast. |
| `QrExporter.cs` | Ties it together: assess, encode, render, verify, encode PNG. |

## Rules that are not stylistic

- **Modules are whole pixels and nothing is anti-aliased.** Grey edge pixels misrepresent
  print quality in exactly the direction that causes reprints. A test asserts the rendered
  bitmap contains exactly two distinct colours.
- **The self-test runs against the final bitmap**, logo and colours applied — not against the
  matrix. Its whole value is catching defects introduced *by* rendering.
- **A failed self-test is fatal and not overridable.** The FR-4.5 override applies to the
  module-size gate only. `QrExporter` returns both results as data; the caller must treat
  `SelfTest.Passed == false` as unexportable.
- **Inversion is checked before contrast.** A light-on-dark pair can have excellent contrast
  and still fail, because many decoders never attempt inversion.
- **`QrEncoder` forces UTF-8.** Without it QRCoder falls back to ISO-8859-1 for payloads that
  happen to fit, so an accented character silently changes the byte count.

## Why `PngDensityWriter` exists

SkiaSharp writes no `pHYs` chunk, so its PNGs carry no DPI. InDesign then assumes 72 dpi and
places the image at ~4× intended size; the operator scales it down by eye, resampling it and
destroying the module edges. A test pins the SkiaSharp gap — if SkiaSharp ever starts writing
`pHYs`, that test fails and this class can be reconsidered.

## The capacity table is now verified

`QrCapacityCrossVerificationTests` checks all 40 versions × 4 correction levels against
QRCoder: a payload of exactly the tabulated capacity must fit, one byte more must not. This is
what cleared `QrCapacityTable` to gate real exports (PRD FR-3.3). **Do not delete it** — it is
the only thing standing between a transcription error and a mis-sized code.
