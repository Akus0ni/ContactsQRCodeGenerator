using SkiaSharp;
using ZXing;
using ZXing.Common;

namespace ContactQR.Rendering;

/// <summary>The outcome of decoding a rendered code back to its source payload.</summary>
/// <param name="Passed">Whether the decoded text matched the source exactly.</param>
/// <param name="DecodedText">What the decoder read, or <see langword="null"/> when it read nothing.</param>
/// <param name="Diagnostics">A sentence naming what went wrong, when it did.</param>
public readonly record struct SelfTestResult(bool Passed, string? DecodedText, string? Diagnostics);

/// <summary>
/// Decodes a rendered bitmap and checks it still carries the exact payload it was built from.
/// </summary>
/// <remarks>
/// <para>
/// This is the difference between "we drew a QR" and "we verified a QR" (PRD FR-4.4). It runs
/// against the final rendered bitmap — logo composited, colours applied — so it catches the
/// whole class of defects where the overlay, a colour choice or a rounding error corrupted the
/// code. Those defects are otherwise found by a client three weeks after printing.
/// </para>
/// <para>
/// The honest limitation: passing proves the symbol is structurally decodable, not that it
/// survives print at the chosen size. It complements the module-size gate and never replaces
/// it.
/// </para>
/// </remarks>
public static class QrSelfTest
{
    /// <summary>
    /// Decodes a rendered bitmap and compares it with the payload it should contain.
    /// </summary>
    /// <param name="bitmap">The final rendered bitmap.</param>
    /// <param name="expectedPayload">The exact text the code was built from.</param>
    /// <returns>Whether the round trip was exact, and why not when it was not.</returns>
    public static SelfTestResult Verify(SKBitmap bitmap, string expectedPayload)
    {
        ArgumentNullException.ThrowIfNull(bitmap);
        ArgumentNullException.ThrowIfNull(expectedPayload);

        var decoded = Decode(bitmap);

        if (decoded is null)
        {
            return new SelfTestResult(
                Passed: false,
                DecodedText: null,
                Diagnostics: "The rendered code could not be decoded at all. The most likely causes are a logo "
                    + "that covers too much of the symbol, or a foreground and background too close in tone.");
        }

        if (!string.Equals(decoded, expectedPayload, StringComparison.Ordinal))
        {
            return new SelfTestResult(
                Passed: false,
                DecodedText: decoded,
                Diagnostics: $"The rendered code decoded to {decoded.Length} characters but the payload is "
                    + $"{expectedPayload.Length}. The contact details would not arrive intact on the scanning phone.");
        }

        return new SelfTestResult(Passed: true, DecodedText: decoded, Diagnostics: null);
    }

    /// <summary>Decodes a bitmap, returning <see langword="null"/> when nothing is readable.</summary>
    /// <param name="bitmap">The bitmap to read.</param>
    /// <returns>The decoded text, or <see langword="null"/>.</returns>
    public static string? Decode(SKBitmap bitmap)
    {
        ArgumentNullException.ThrowIfNull(bitmap);

        var reader = new BarcodeReaderGeneric
        {
            AutoRotate = false,
            Options = new DecodingOptions
            {
                PossibleFormats = [BarcodeFormat.QR_CODE],
                TryHarder = true,
                PureBarcode = false,
            },
        };

        var luminance = ToLuminanceSource(bitmap);

        return reader.Decode(luminance)?.Text;
    }

    private static RGBLuminanceSource ToLuminanceSource(SKBitmap bitmap)
    {
        var pixels = bitmap.Pixels;
        var rgb = new byte[pixels.Length * 3];

        for (var index = 0; index < pixels.Length; index++)
        {
            var pixel = pixels[index];
            rgb[index * 3] = pixel.Red;
            rgb[(index * 3) + 1] = pixel.Green;
            rgb[(index * 3) + 2] = pixel.Blue;
        }

        return new RGBLuminanceSource(rgb, bitmap.Width, bitmap.Height, RGBLuminanceSource.BitmapFormat.RGB24);
    }
}
