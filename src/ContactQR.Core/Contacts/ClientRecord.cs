namespace ContactQR.Core.Contacts;

/// <summary>
/// A client's business contact details — everything that can be encoded into a vCard.
/// </summary>
/// <remarks>
/// Only a given name and a primary mobile number are required; every other field and the
/// logo are optional (PRD FR-1.1, FR-1.2). Optional fields left empty are omitted from the
/// vCard entirely rather than emitted as empty properties, because an empty property causes
/// visible blank fields in some Android contact applications.
/// </remarks>
public sealed record ClientRecord
{
    /// <summary>The client's given name. Required; blocks generation when absent.</summary>
    public required string GivenName { get; init; }

    /// <summary>
    /// The client's family name. Optional, so that mononyms encode correctly as
    /// <c>N:;Given;;;</c> rather than being forced into a surname (PRD EC-10).
    /// </summary>
    public string? FamilyName { get; init; }

    /// <summary>Trading name of the business. vCard <c>ORG</c>.</summary>
    public string? Company { get; init; }

    /// <summary>Role within the business. vCard <c>TITLE</c>.</summary>
    public string? JobTitle { get; init; }

    /// <summary>Work postal address. vCard <c>ADR</c>.</summary>
    public PostalAddress? Address { get; init; }

    /// <summary>Free text such as a tagline. vCard <c>NOTE</c>, and the costliest field per character.</summary>
    public string? Note { get; init; }

    /// <summary>Phones, emails and URLs belonging to this client.</summary>
    public IReadOnlyList<ContactPoint> ContactPoints { get; init; } = [];

    /// <summary>
    /// The full name for vCard <c>FN</c>, which is mandatory in vCard 3.0 — its absence makes
    /// iOS display a blank contact name (PRD FR-2.2).
    /// </summary>
    public string FullName =>
        string.IsNullOrWhiteSpace(FamilyName)
            ? GivenName.Trim()
            : $"{GivenName.Trim()} {FamilyName.Trim()}";

    /// <summary>The primary phone, or <see langword="null"/> when none has been entered.</summary>
    public ContactPoint? PrimaryPhone =>
        ContactPoints.FirstOrDefault(point => point is { Kind: ContactPointKind.Phone, IsPrimary: true });
}
