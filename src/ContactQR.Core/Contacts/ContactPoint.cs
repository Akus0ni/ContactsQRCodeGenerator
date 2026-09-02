namespace ContactQR.Core.Contacts;

/// <summary>The kind of reachable endpoint a <see cref="ContactPoint"/> represents.</summary>
public enum ContactPointKind
{
    /// <summary>A telephone number, emitted as a vCard <c>TEL</c> property.</summary>
    Phone,

    /// <summary>An email address, emitted as a vCard <c>EMAIL</c> property.</summary>
    Email,

    /// <summary>A web address, emitted as a vCard <c>URL</c> property.</summary>
    Url,
}

/// <summary>The vCard <c>TYPE</c> qualifier applied to a <see cref="ContactPoint"/>.</summary>
public enum ContactPointSubtype
{
    /// <summary>A mobile number. Emitted as <c>TYPE=CELL</c>.</summary>
    Mobile,

    /// <summary>A landline at the place of work. Emitted as <c>TYPE=WORK,VOICE</c>.</summary>
    Work,

    /// <summary>A facsimile number. Emitted as <c>TYPE=WORK,FAX</c>.</summary>
    Fax,

    /// <summary>A personal or home endpoint. Emitted as <c>TYPE=HOME</c>.</summary>
    Home,

    /// <summary>A social or profile address. Emitted as a bare <c>URL</c>.</summary>
    Social,
}

/// <summary>
/// One reachable endpoint belonging to a client: a phone number, an email address or a URL.
/// </summary>
/// <remarks>
/// Both the operator's raw input and the normalised form are retained. PRD FR-1.4 forbids
/// silently guessing a country code, so the operator must always be able to see what they
/// typed alongside what the application made of it, and a normalisation defect must never
/// destroy the original.
/// </remarks>
public sealed record ContactPoint
{
    /// <summary>Whether this endpoint is a phone number, an email address or a URL.</summary>
    public required ContactPointKind Kind { get; init; }

    /// <summary>The vCard <c>TYPE</c> qualifier for this endpoint.</summary>
    public required ContactPointSubtype Subtype { get; init; }

    /// <summary>The value exactly as the operator entered it.</summary>
    public required string RawValue { get; init; }

    /// <summary>
    /// The value after normalisation — E.164 for phones, scheme-prefixed for URLs. Falls back
    /// to <see cref="RawValue"/> when the input could not be normalised.
    /// </summary>
    public string? NormalisedValue { get; init; }

    /// <summary>
    /// Whether this is the client's primary phone. Exactly one phone must be primary; it is
    /// one of only two fields that block generation (PRD FR-1.2).
    /// </summary>
    public bool IsPrimary { get; init; }

    /// <summary>
    /// Controls emission order within a property group, so that identical input always
    /// produces a byte-identical vCard (PRD FR-2.8).
    /// </summary>
    public int SortOrder { get; init; }

    /// <summary>The value that should be encoded into the vCard.</summary>
    public string ValueToEncode => NormalisedValue ?? RawValue;
}
