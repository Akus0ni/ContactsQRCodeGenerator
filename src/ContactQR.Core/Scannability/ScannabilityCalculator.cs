namespace ContactQR.Core.Scannability;

/// <summary>
/// Computes whether a QR code will survive being scanned off print at a given physical size.
/// </summary>
/// <remarks>
/// This is the differentiating calculation described in PRD F4. A QR of version <c>V</c> has
/// <c>17 + 4V</c> modules per side; adding the quiet zone on both edges gives the total. The
/// printed width divided by that total is the module size, and the module size is what decides
/// whether a phone camera can resolve the code off paper in one attempt.
/// </remarks>
public sealed class ScannabilityCalculator
{
    /// <summary>The quiet zone required by the QR specification, in modules per edge.</summary>
    public const int MinimumQuietZoneModules = 4;

    private readonly ScannabilityThresholds thresholds;

    /// <summary>Creates a calculator using the uncalibrated default thresholds.</summary>
    public ScannabilityCalculator()
        : this(ScannabilityThresholds.Default)
    {
    }

    /// <summary>Creates a calculator using explicit thresholds.</summary>
    /// <param name="thresholds">The module-size thresholds that decide a verdict.</param>
    /// <exception cref="ArgumentNullException"><paramref name="thresholds"/> is <see langword="null"/>.</exception>
    public ScannabilityCalculator(ScannabilityThresholds thresholds)
    {
        ArgumentNullException.ThrowIfNull(thresholds);
        this.thresholds = thresholds;
    }

    /// <summary>
    /// The correction level actually in force. A centre logo forces H regardless of what the
    /// operator selected, because H is what makes a centre occlusion survivable (PRD FR-5.1).
    /// </summary>
    /// <param name="selected">The level the operator chose for the no-logo case.</param>
    /// <param name="hasLogo">Whether a centre logo is present.</param>
    /// <returns>The effective correction level.</returns>
    public static ErrorCorrectionLevel EffectiveErrorCorrection(ErrorCorrectionLevel selected, bool hasLogo) =>
        hasLogo ? ErrorCorrectionLevel.H : selected;

    /// <summary>
    /// Assesses a payload at an intended print width.
    /// </summary>
    /// <param name="payloadBytes">Payload size in UTF-8 bytes, never characters.</param>
    /// <param name="level">The effective correction level, from <see cref="EffectiveErrorCorrection"/>.</param>
    /// <param name="printWidthMillimetres">The intended printed width of the whole symbol including its quiet zone.</param>
    /// <param name="quietZoneModules">Quiet zone per edge. Defaults to, and may not be below, the specification minimum.</param>
    /// <returns>The full assessment, including the verdict and the minimum safe width.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The payload is negative, the width is not positive, or the quiet zone is below the
    /// specification minimum of four modules (PRD FR-6.5).
    /// </exception>
    public ScannabilityAssessment Assess(
        int payloadBytes,
        ErrorCorrectionLevel level,
        decimal printWidthMillimetres,
        int quietZoneModules = MinimumQuietZoneModules)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(payloadBytes);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(printWidthMillimetres);
        ArgumentOutOfRangeException.ThrowIfLessThan(quietZoneModules, MinimumQuietZoneModules);

        if (!QrCapacityTable.TryFindSmallestVersion(payloadBytes, level, out var version))
        {
            return OverCapacity(payloadBytes, level, printWidthMillimetres);
        }

        var modulesPerSide = QrCapacityTable.ModulesPerSide(version);
        var totalModules = modulesPerSide + (2 * quietZoneModules);
        var moduleSize = printWidthMillimetres / totalModules;

        return new ScannabilityAssessment
        {
            PayloadBytes = payloadBytes,
            ErrorCorrection = level,
            CapacityBytes = QrCapacityTable.CapacityFor(version, level),
            Version = version,
            ModulesPerSide = modulesPerSide,
            TotalModulesPerSide = totalModules,
            PrintWidthMillimetres = printWidthMillimetres,
            ModuleSizeMillimetres = moduleSize,
            MinimumSafeWidthMillimetres = thresholds.SafeMillimetresPerModule * totalModules,
            Verdict = VerdictFor(moduleSize),
        };
    }

    private ScannabilityVerdict VerdictFor(decimal moduleSizeMillimetres)
    {
        if (moduleSizeMillimetres >= thresholds.SafeMillimetresPerModule)
        {
            return ScannabilityVerdict.Safe;
        }

        return moduleSizeMillimetres >= thresholds.FloorMillimetresPerModule
            ? ScannabilityVerdict.Marginal
            : ScannabilityVerdict.WillFail;
    }

    private static ScannabilityAssessment OverCapacity(
        int payloadBytes,
        ErrorCorrectionLevel level,
        decimal printWidthMillimetres)
    {
        var maximumCapacity = QrCapacityTable.MaximumCapacityFor(level);

        return new ScannabilityAssessment
        {
            PayloadBytes = payloadBytes,
            ErrorCorrection = level,
            CapacityBytes = maximumCapacity,
            Version = 0,
            ModulesPerSide = 0,
            TotalModulesPerSide = 0,
            PrintWidthMillimetres = printWidthMillimetres,
            ModuleSizeMillimetres = 0m,
            MinimumSafeWidthMillimetres = 0m,
            Verdict = ScannabilityVerdict.ExceedsCapacity,
            OverflowBytes = payloadBytes - maximumCapacity,
        };
    }
}
