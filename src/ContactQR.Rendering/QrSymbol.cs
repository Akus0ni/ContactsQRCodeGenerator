using ContactQR.Core.Scannability;
using QRCoder;

namespace ContactQR.Rendering;

/// <summary>
/// An encoded QR symbol: the module matrix, its version, and the quiet zone around it.
/// </summary>
/// <remarks>
/// Encoding and rendering are kept apart (PRD FR-3.4). This type is the boundary — a plain
/// grid of light and dark modules that a renderer draws and a self-test decodes, with no
/// drawing concepts attached to it.
/// </remarks>
public sealed class QrSymbol
{
    private readonly bool[,] modules;

    internal QrSymbol(bool[,] modules, int version, int quietZoneModules)
    {
        this.modules = modules;
        Version = version;
        QuietZoneModules = quietZoneModules;
    }

    /// <summary>The QR version, between 1 and 40.</summary>
    public int Version { get; }

    /// <summary>The quiet zone included on each edge of <see cref="TotalModulesPerSide"/>.</summary>
    public int QuietZoneModules { get; }

    /// <summary>Modules per side of the symbol itself, excluding the quiet zone.</summary>
    public int ModulesPerSide => QrCapacityTable.ModulesPerSide(Version);

    /// <summary>Modules per side including the quiet zone on both edges.</summary>
    public int TotalModulesPerSide => modules.GetLength(0);

    /// <summary>Whether the module at a position is dark.</summary>
    /// <param name="x">Column, including the quiet zone.</param>
    /// <param name="y">Row, including the quiet zone.</param>
    /// <returns><see langword="true"/> when the module is dark.</returns>
    public bool IsDark(int x, int y) => modules[y, x];
}

/// <summary>
/// Encodes a payload into a <see cref="QrSymbol"/> using QRCoder.
/// </summary>
/// <remarks>
/// QRCoder is a pure managed, MIT-licensed encoder with no native or network dependency,
/// which is what makes it auditable against the offline guarantee (PRD FR-3.1, FR-8.3).
/// </remarks>
public static class QrEncoder
{
    /// <summary>
    /// Encodes text as a QR symbol at the given correction level.
    /// </summary>
    /// <param name="payload">The text to encode, typically a vCard.</param>
    /// <param name="level">The effective error-correction level.</param>
    /// <param name="quietZoneModules">Quiet zone per edge. May not be below the specification minimum of four.</param>
    /// <returns>The encoded symbol.</returns>
    /// <exception cref="ArgumentException"><paramref name="payload"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The quiet zone is below four modules.</exception>
    public static QrSymbol Encode(
        string payload,
        ErrorCorrectionLevel level,
        int quietZoneModules = ScannabilityCalculator.MinimumQuietZoneModules)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(payload);
        ArgumentOutOfRangeException.ThrowIfLessThan(
            quietZoneModules,
            ScannabilityCalculator.MinimumQuietZoneModules);

        using var generator = new QRCodeGenerator();

        // forceUtf8 keeps the encoding deterministic. Without it QRCoder falls back to
        // ISO-8859-1 for payloads that happen to be representable in it, so an accented
        // character could silently change the byte count between two otherwise similar
        // clients (PRD FR-2.5).
        using var data = generator.CreateQrCode(payload, ToQrCoderLevel(level), forceUtf8: true);

        return FromQrCoder(data, quietZoneModules);
    }

    private static QrSymbol FromQrCoder(QRCodeData data, int quietZoneModules)
    {
        // QRCoder returns the matrix with its own four-module quiet zone already applied.
        // Strip it and re-apply the requested one, so the quiet zone is ours to control
        // (PRD FR-6.5 allows increasing it but never going below four).
        const int QrCoderQuietZone = 4;

        var source = data.ModuleMatrix;
        var sourceSize = source.Count;
        var symbolSize = sourceSize - (2 * QrCoderQuietZone);
        var totalSize = symbolSize + (2 * quietZoneModules);

        var modules = new bool[totalSize, totalSize];

        for (var y = 0; y < symbolSize; y++)
        {
            for (var x = 0; x < symbolSize; x++)
            {
                modules[y + quietZoneModules, x + quietZoneModules] =
                    source[y + QrCoderQuietZone][x + QrCoderQuietZone];
            }
        }

        var version = (symbolSize - 17) / 4;

        return new QrSymbol(modules, version, quietZoneModules);
    }

    private static QRCodeGenerator.ECCLevel ToQrCoderLevel(ErrorCorrectionLevel level) => level switch
    {
        ErrorCorrectionLevel.L => QRCodeGenerator.ECCLevel.L,
        ErrorCorrectionLevel.M => QRCodeGenerator.ECCLevel.M,
        ErrorCorrectionLevel.Q => QRCodeGenerator.ECCLevel.Q,
        ErrorCorrectionLevel.H => QRCodeGenerator.ECCLevel.H,
        _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Unknown error-correction level."),
    };
}
