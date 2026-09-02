using System.Buffers.Binary;

namespace ContactQR.Rendering;

/// <summary>
/// Writes physical resolution into a PNG's <c>pHYs</c> chunk.
/// </summary>
/// <remarks>
/// <para>
/// SkiaSharp does not write <c>pHYs</c>, so a PNG it encodes carries no DPI at all. Without
/// it, InDesign and Illustrator assume 72 dpi and place the image at roughly four times its
/// intended size; the operator then scales it down by eye, resampling it and destroying the
/// module edges that decide whether it scans (PRD FR-6.4).
/// </para>
/// <para>
/// This is the known implementation trap called out in the design brief, handled by rewriting
/// the encoded byte stream rather than by taking a dependency on System.Drawing.
/// </para>
/// </remarks>
public static class PngDensityWriter
{
    private const int InchesPerMetreNumerator = 10_000;
    private const int InchesPerMetreDenominator = 254;
    private const byte UnitIsMetre = 1;

    private static ReadOnlySpan<byte> Signature => [0x89, (byte)'P', (byte)'N', (byte)'G', 0x0D, 0x0A, 0x1A, 0x0A];

    /// <summary>
    /// Returns a copy of a PNG with its <c>pHYs</c> chunk set to the given resolution.
    /// </summary>
    /// <param name="png">A complete PNG byte stream.</param>
    /// <param name="dotsPerInch">The physical resolution to record.</param>
    /// <returns>A new PNG byte stream carrying the resolution.</returns>
    /// <exception cref="ArgumentException"><paramref name="png"/> is not a PNG.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="dotsPerInch"/> is not positive.</exception>
    public static byte[] WithResolution(byte[] png, int dotsPerInch)
    {
        ArgumentNullException.ThrowIfNull(png);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(dotsPerInch);

        if (png.Length < Signature.Length || !png.AsSpan(0, Signature.Length).SequenceEqual(Signature))
        {
            throw new ArgumentException("The supplied bytes are not a PNG stream.", nameof(png));
        }

        var pixelsPerMetre = ToPixelsPerMetre(dotsPerInch);
        var chunk = BuildPhysChunk(pixelsPerMetre);

        using var rewritten = new MemoryStream(png.Length + chunk.Length);
        rewritten.Write(Signature);

        var position = Signature.Length;
        var chunkWritten = false;

        while (position + 8 <= png.Length)
        {
            var dataLength = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
            var type = System.Text.Encoding.ASCII.GetString(png, position + 4, 4);
            var totalLength = 12 + dataLength;

            // An existing pHYs is replaced rather than duplicated; two would be invalid.
            if (type is "pHYs")
            {
                position += totalLength;
                continue;
            }

            rewritten.Write(png, position, totalLength);
            position += totalLength;

            // pHYs must appear before the first IDAT. Writing it straight after IHDR is the
            // simplest position that is always legal.
            if (!chunkWritten && type is "IHDR")
            {
                rewritten.Write(chunk);
                chunkWritten = true;
            }
        }

        return rewritten.ToArray();
    }

    /// <summary>
    /// Reads the recorded resolution from a PNG.
    /// </summary>
    /// <param name="png">A complete PNG byte stream.</param>
    /// <returns>The resolution in dots per inch, or <see langword="null"/> when none is recorded.</returns>
    public static int? ReadResolution(byte[] png)
    {
        ArgumentNullException.ThrowIfNull(png);

        var position = Signature.Length;

        while (position + 8 <= png.Length)
        {
            var dataLength = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(position, 4));
            var type = System.Text.Encoding.ASCII.GetString(png, position + 4, 4);

            if (type is "pHYs")
            {
                var pixelsPerMetre = BinaryPrimitives.ReadUInt32BigEndian(png.AsSpan(position + 8, 4));

                return ToDotsPerInch(pixelsPerMetre);
            }

            position += 12 + dataLength;
        }

        return null;
    }

    private static uint ToPixelsPerMetre(int dotsPerInch) =>
        (uint)Math.Round(dotsPerInch * (double)InchesPerMetreNumerator / InchesPerMetreDenominator);

    private static int ToDotsPerInch(uint pixelsPerMetre) =>
        (int)Math.Round(pixelsPerMetre * (double)InchesPerMetreDenominator / InchesPerMetreNumerator);

    private static byte[] BuildPhysChunk(uint pixelsPerMetre)
    {
        var chunk = new byte[21];

        BinaryPrimitives.WriteInt32BigEndian(chunk.AsSpan(0, 4), 9);
        "pHYs"u8.CopyTo(chunk.AsSpan(4, 4));
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8, 4), pixelsPerMetre);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(12, 4), pixelsPerMetre);
        chunk[16] = UnitIsMetre;
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(17, 4), Crc32.Compute(chunk.AsSpan(4, 13)));

        return chunk;
    }
}

/// <summary>The CRC-32 that PNG uses to checksum each chunk.</summary>
internal static class Crc32
{
    private static readonly uint[] Table = BuildTable();

    internal static uint Compute(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;

        foreach (var value in bytes)
        {
            crc = Table[(crc ^ value) & 0xFF] ^ (crc >> 8);
        }

        return crc ^ 0xFFFFFFFFu;
    }

    private static uint[] BuildTable()
    {
        var table = new uint[256];

        for (var index = 0u; index < table.Length; index++)
        {
            var value = index;

            for (var bit = 0; bit < 8; bit++)
            {
                value = (value & 1) != 0 ? 0xEDB88320u ^ (value >> 1) : value >> 1;
            }

            table[index] = value;
        }

        return table;
    }
}
