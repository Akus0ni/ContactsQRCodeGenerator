namespace ContactQR.Core.Scannability;

/// <summary>Whether a code is expected to survive being scanned off print.</summary>
/// <remarks>
/// These four members map one-to-one onto the four rendering states of the control strip in
/// the design brief, which is why exceeding capacity is modelled as a verdict rather than as
/// an exception: the interface must show <em>how far</em> over budget a payload is, not merely
/// that it failed.
/// </remarks>
public enum ScannabilityVerdict
{
    /// <summary>Comfortably above the module-size floor. Export proceeds.</summary>
    Safe,

    /// <summary>Between the floor and the safe target. Export proceeds with acknowledgement.</summary>
    Marginal,

    /// <summary>Below the module-size floor. Export is blocked unless deliberately overridden (PRD FR-4.5).</summary>
    WillFail,

    /// <summary>The payload does not fit even version 40 at this correction level (PRD EC-1).</summary>
    ExceedsCapacity,
}

/// <summary>
/// The module-size thresholds that decide a verdict, in millimetres per module.
/// </summary>
/// <remarks>
/// <b>These are a starting hypothesis, not settled fact.</b> They come from published
/// print-QR guidance rather than from this product's own measurements, and PRD M1b requires
/// them to be calibrated against the physical device matrix and corrected if measurement
/// disagrees. They are expressed as an injectable record precisely so that calibration is a
/// configuration change rather than a code change.
/// </remarks>
public sealed record ScannabilityThresholds
{
    /// <summary>At or above this, a code is <see cref="ScannabilityVerdict.Safe"/>.</summary>
    public decimal SafeMillimetresPerModule { get; init; } = 0.40m;

    /// <summary>Below this, a code <see cref="ScannabilityVerdict.WillFail"/>.</summary>
    public decimal FloorMillimetresPerModule { get; init; } = 0.30m;

    /// <summary>The uncalibrated defaults described above.</summary>
    public static ScannabilityThresholds Default { get; } = new();
}

/// <summary>
/// The complete scannability picture for one payload at one physical size — everything the
/// Scannability Budget panel displays (PRD FR-4.1).
/// </summary>
public sealed record ScannabilityAssessment
{
    /// <summary>Payload size in UTF-8 bytes.</summary>
    public required int PayloadBytes { get; init; }

    /// <summary>The correction level in force, after any logo has forced it to H.</summary>
    public required ErrorCorrectionLevel ErrorCorrection { get; init; }

    /// <summary>Capacity available at this correction level for the selected version.</summary>
    public required int CapacityBytes { get; init; }

    /// <summary>The computed QR version, or 0 when the payload exceeds capacity.</summary>
    public required int Version { get; init; }

    /// <summary>Modules per side excluding the quiet zone, or 0 when over capacity.</summary>
    public required int ModulesPerSide { get; init; }

    /// <summary>Modules per side including the quiet zone on both edges, or 0 when over capacity.</summary>
    public required int TotalModulesPerSide { get; init; }

    /// <summary>The intended printed width in millimetres.</summary>
    public required decimal PrintWidthMillimetres { get; init; }

    /// <summary>The number that decides success: printed width divided by total modules.</summary>
    public required decimal ModuleSizeMillimetres { get; init; }

    /// <summary>The narrowest width at which this payload reaches <see cref="ScannabilityVerdict.Safe"/>.</summary>
    public required decimal MinimumSafeWidthMillimetres { get; init; }

    /// <summary>The verdict for this payload at this width.</summary>
    public required ScannabilityVerdict Verdict { get; init; }

    /// <summary>
    /// How many bytes the payload is over the maximum capacity, or 0 when it fits. Lets the
    /// interface show the magnitude of an overflow rather than only its existence.
    /// </summary>
    public int OverflowBytes { get; init; }

    /// <summary>Whether export must be blocked absent a deliberate override (PRD FR-4.5).</summary>
    public bool BlocksExport =>
        Verdict is ScannabilityVerdict.WillFail or ScannabilityVerdict.ExceedsCapacity;
}
