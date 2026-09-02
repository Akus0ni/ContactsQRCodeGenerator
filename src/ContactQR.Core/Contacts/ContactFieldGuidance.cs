namespace ContactQR.Core.Contacts;

/// <summary>
/// What one contact field becomes on the scanning phone: the vCard property it is encoded
/// as, where it lands in the recipient's contact record, and any known platform caveat.
/// </summary>
/// <remarks>
/// This is the content behind each field's tooltip. It lives in the domain rather than in
/// XAML so that the vCard mapping is stated once, next to the encoder that implements it,
/// and cannot drift out of step with what the encoder actually emits.
/// </remarks>
public sealed record ContactFieldGuidance
{
    /// <summary>The field this guidance describes.</summary>
    public required ContactField Field { get; init; }

    /// <summary>The field's label in the editor.</summary>
    public required string Label { get; init; }

    /// <summary>The vCard property the field is encoded as, shown verbatim.</summary>
    public required string VCardProperty { get; init; }

    /// <summary>
    /// What the recipient's phone does with it — the sentence answering "if someone scans
    /// this, what gets saved?"
    /// </summary>
    public required string WhenScanned { get; init; }

    /// <summary>
    /// A known difference between iOS and Android, or a way the field commonly disappoints.
    /// Absent when behaviour is consistent across platforms.
    /// </summary>
    public string? PlatformCaveat { get; init; }

    /// <summary>
    /// Whether this behaviour has been observed on the physical device matrix, rather than
    /// taken from the vCard specification.
    /// </summary>
    /// <remarks>
    /// PRD EC-30 requires per-platform field handling to be characterised by measurement, and
    /// PRD M1a sets field fidelity at 100% as an acceptance gate. Until that matrix has run,
    /// this is <see langword="false"/> for every field and the interface should present the
    /// guidance as expected rather than confirmed behaviour. Flipping these to
    /// <see langword="true"/> is a deliberate act that follows a device test, never a guess.
    /// </remarks>
    public bool ConfirmedOnDeviceMatrix { get; init; }
}
