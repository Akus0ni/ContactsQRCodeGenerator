namespace ContactQR.Core.Contacts;

/// <summary>
/// A work postal address, mapping to the seven structured components of a vCard <c>ADR</c>.
/// </summary>
/// <remarks>
/// Every component is optional. The encoder emits all seven positions regardless, preserving
/// empty ones as consecutive semicolons, because dropping a leading empty component shifts
/// every later value into the wrong field on the scanning phone (PRD FR-2.7).
/// </remarks>
public sealed record PostalAddress
{
    /// <summary>Street address. vCard <c>ADR</c> component 3.</summary>
    public string? Street { get; init; }

    /// <summary>Locality or city. vCard <c>ADR</c> component 4.</summary>
    public string? City { get; init; }

    /// <summary>Region, state or province. vCard <c>ADR</c> component 5.</summary>
    public string? State { get; init; }

    /// <summary>Postal or ZIP code. vCard <c>ADR</c> component 6.</summary>
    public string? PostalCode { get; init; }

    /// <summary>Country name. vCard <c>ADR</c> component 7.</summary>
    public string? Country { get; init; }

    /// <summary>
    /// Whether every component is empty, in which case the encoder omits the property rather
    /// than emitting a bare run of semicolons (PRD FR-1.2).
    /// </summary>
    public bool IsEmpty =>
        string.IsNullOrWhiteSpace(Street)
        && string.IsNullOrWhiteSpace(City)
        && string.IsNullOrWhiteSpace(State)
        && string.IsNullOrWhiteSpace(PostalCode)
        && string.IsNullOrWhiteSpace(Country);
}
