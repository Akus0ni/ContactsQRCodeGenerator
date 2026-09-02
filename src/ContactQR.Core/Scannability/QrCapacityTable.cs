namespace ContactQR.Core.Scannability;

/// <summary>QR error-correction level, in ascending order of redundancy.</summary>
public enum ErrorCorrectionLevel
{
    /// <summary>Roughly 7% redundancy. Available, but unsuitable for anything handled or laminated.</summary>
    L,

    /// <summary>Roughly 15% redundancy. The default when no logo is present (PRD FR-5.1).</summary>
    M,

    /// <summary>Roughly 25% redundancy.</summary>
    Q,

    /// <summary>
    /// Roughly 30% redundancy. Forced when a centre logo is present, because it is what makes
    /// a centre occlusion survivable at all (PRD FR-5.1).
    /// </summary>
    H,
}

/// <summary>
/// Byte-mode data capacity for QR versions 1 to 40 at each error-correction level.
/// </summary>
/// <remarks>
/// <para>
/// vCard payloads require byte mode: alphanumeric mode cannot represent lowercase letters,
/// <c>@</c> or <c>:</c> (PRD FR-3.2).
/// </para>
/// <para>
/// <b>This table must be cross-verified against the encoder library before it gates any
/// export.</b> PRD FR-3.3 makes the encoder the authority on capacity, and published capacity
/// tables differ slightly from one another. The values here are from ISO/IEC 18004 and are
/// used for the pre-encode budget projection; once the QR encoder is integrated, the
/// integration test should assert that every entry agrees with it. See PRD open question Q8.
/// </para>
/// </remarks>
public static class QrCapacityTable
{
    /// <summary>The lowest QR version number.</summary>
    public const int MinimumVersion = 1;

    /// <summary>The highest QR version number.</summary>
    public const int MaximumVersion = 40;

    /// <summary>Indexed by version (1-based, index 0 unused), then by L, M, Q, H.</summary>
    private static readonly int[][] ByteCapacities =
    [
        [0, 0, 0, 0],
        [17, 14, 11, 7],
        [32, 26, 20, 14],
        [53, 42, 32, 24],
        [78, 62, 46, 34],
        [106, 84, 60, 44],
        [134, 106, 74, 58],
        [154, 122, 86, 64],
        [192, 152, 108, 84],
        [230, 180, 130, 98],
        [271, 213, 151, 119],
        [321, 251, 177, 137],
        [367, 287, 203, 155],
        [425, 331, 241, 177],
        [458, 362, 258, 194],
        [520, 412, 292, 220],
        [586, 450, 322, 250],
        [644, 504, 364, 280],
        [718, 560, 394, 310],
        [792, 624, 442, 338],
        [858, 666, 482, 382],
        [929, 711, 509, 403],
        [1003, 779, 565, 439],
        [1091, 857, 611, 461],
        [1171, 911, 661, 511],
        [1273, 997, 715, 535],
        [1367, 1059, 751, 593],
        [1465, 1125, 805, 625],
        [1528, 1190, 868, 658],
        [1628, 1264, 908, 698],
        [1732, 1370, 982, 742],
        [1840, 1452, 1030, 790],
        [1952, 1538, 1112, 842],
        [2068, 1628, 1168, 898],
        [2188, 1722, 1228, 958],
        [2303, 1809, 1283, 983],
        [2431, 1911, 1351, 1051],
        [2563, 1989, 1423, 1093],
        [2699, 2099, 1499, 1139],
        [2809, 2213, 1579, 1219],
        [2953, 2331, 1663, 1273],
    ];

    /// <summary>Returns the byte-mode capacity of a given version at a given correction level.</summary>
    /// <param name="version">A QR version between 1 and 40.</param>
    /// <param name="level">The error-correction level.</param>
    /// <returns>Capacity in bytes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="version"/> is outside 1 to 40.</exception>
    public static int CapacityFor(int version, ErrorCorrectionLevel level)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, MinimumVersion);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version, MaximumVersion);

        return ByteCapacities[version][(int)level];
    }

    /// <summary>The capacity of the largest QR version at a given correction level.</summary>
    /// <param name="level">The error-correction level.</param>
    /// <returns>The maximum payload in bytes that this level can carry.</returns>
    public static int MaximumCapacityFor(ErrorCorrectionLevel level) =>
        CapacityFor(MaximumVersion, level);

    /// <summary>
    /// Finds the smallest version that holds a payload. Version is never chosen by the
    /// operator; it is a computed consequence of payload and correction level (PRD FR-3.3).
    /// </summary>
    /// <param name="payloadBytes">The payload size in UTF-8 bytes.</param>
    /// <param name="level">The error-correction level.</param>
    /// <param name="version">The smallest sufficient version, when one exists.</param>
    /// <returns>
    /// <see langword="true"/> when a version can hold the payload; otherwise
    /// <see langword="false"/>, meaning the payload exceeds even version 40.
    /// </returns>
    public static bool TryFindSmallestVersion(int payloadBytes, ErrorCorrectionLevel level, out int version)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadBytes);

        for (var candidate = MinimumVersion; candidate <= MaximumVersion; candidate++)
        {
            if (CapacityFor(candidate, level) >= payloadBytes)
            {
                version = candidate;
                return true;
            }
        }

        version = 0;
        return false;
    }

    /// <summary>The number of modules along one side of a symbol, excluding the quiet zone.</summary>
    /// <param name="version">A QR version between 1 and 40.</param>
    /// <returns>The module count per side.</returns>
    public static int ModulesPerSide(int version)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(version, MinimumVersion);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(version, MaximumVersion);

        return 17 + (4 * version);
    }
}
