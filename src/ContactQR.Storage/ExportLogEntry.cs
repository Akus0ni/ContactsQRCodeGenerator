using ContactQR.Core.Scannability;

namespace ContactQR.Storage;

/// <summary>
/// One exported PNG, recorded exactly as it was produced.
/// </summary>
/// <remarks>
/// <para>
/// This is the append-only audit trail of PRD FR-7.7. When a client rings eight months later
/// to say the QR on their card does not work, this answers the question in thirty seconds
/// instead of by guessing.
/// </para>
/// <para>
/// <see cref="VCardSnapshot"/> is the single most useful field: it reconstructs exactly what
/// was encoded at the time, after the client record has since been edited.
/// </para>
/// </remarks>
public sealed record ExportLogEntry
{
    /// <summary>The client this export belongs to.</summary>
    public required Guid ClientId { get; init; }

    /// <summary>Where the PNG was written.</summary>
    public required string FilePath { get; init; }

    /// <summary>The exact payload encoded, retained even after the client record changes.</summary>
    public required string VCardSnapshot { get; init; }

    /// <summary>Payload size in UTF-8 bytes.</summary>
    public required int PayloadBytes { get; init; }

    /// <summary>The correction level in force, after any logo forced it to H.</summary>
    public required ErrorCorrectionLevel ErrorCorrection { get; init; }

    /// <summary>The QR version produced.</summary>
    public required int Version { get; init; }

    /// <summary>The printed width requested.</summary>
    public required decimal WidthMillimetres { get; init; }

    /// <summary>The module size, which is the number that decides success.</summary>
    public required decimal ModuleSizeMillimetres { get; init; }

    /// <summary>The verdict at the time of export.</summary>
    public required ScannabilityVerdict Verdict { get; init; }

    /// <summary>
    /// Whether the operator deliberately overrode a blocking verdict (PRD FR-4.5).
    /// </summary>
    /// <remarks>
    /// Recorded separately from the verdict because "I chose this" and "this was broken" are
    /// different conditions, and the operator must be able to tell them apart later.
    /// </remarks>
    public bool UnsafeOverride { get; init; }

    /// <summary>Whether the decode-back self-test passed. An export never proceeds when it did not.</summary>
    public bool SelfTestPassed { get; init; }

    /// <summary>When the export happened.</summary>
    public DateTimeOffset ExportedAt { get; init; } = DateTimeOffset.UtcNow;
}
