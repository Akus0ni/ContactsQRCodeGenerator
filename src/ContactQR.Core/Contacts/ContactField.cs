namespace ContactQR.Core.Contacts;

/// <summary>
/// Every field the contact editor offers, used to key the guidance shown in field tooltips.
/// </summary>
/// <remarks>
/// This is a presentation-facing enumeration rather than a shape of the stored data: several
/// members map to the same <see cref="ContactPoint"/> kind and differ only in the vCard
/// <c>TYPE</c> the operator picked. It exists so the interface can explain each field
/// individually without the UI layer holding its own copy of the vCard mapping.
/// </remarks>
public enum ContactField
{
    /// <summary>The client's given name. Required.</summary>
    GivenName,

    /// <summary>The client's family name.</summary>
    FamilyName,

    /// <summary>Trading name of the business.</summary>
    Company,

    /// <summary>Role within the business.</summary>
    JobTitle,

    /// <summary>The primary mobile number. Required.</summary>
    Mobile,

    /// <summary>A second mobile number.</summary>
    SecondMobile,

    /// <summary>A landline at the place of work.</summary>
    WorkPhone,

    /// <summary>A facsimile number.</summary>
    Fax,

    /// <summary>The work email address.</summary>
    WorkEmail,

    /// <summary>A personal email address.</summary>
    PersonalEmail,

    /// <summary>The business website.</summary>
    Website,

    /// <summary>A social or profile address.</summary>
    SocialUrl,

    /// <summary>Street line of the work address.</summary>
    Street,

    /// <summary>Locality or city of the work address.</summary>
    City,

    /// <summary>Region, state or province of the work address.</summary>
    State,

    /// <summary>Postal or ZIP code of the work address.</summary>
    PostalCode,

    /// <summary>Country of the work address.</summary>
    Country,

    /// <summary>Free text such as a tagline.</summary>
    Note,
}
