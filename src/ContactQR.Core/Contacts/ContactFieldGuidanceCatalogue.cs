namespace ContactQR.Core.Contacts;

/// <summary>
/// The tooltip content for every field in the contact editor, explaining what each one
/// becomes when the code is scanned on iOS and Android.
/// </summary>
/// <remarks>
/// <para>
/// Written in the interface's voice: plain, active, from the operator's side of the screen.
/// The operator has to be able to repeat these sentences to a client who asks "what will
/// people actually get when they scan my card?", so they describe outcomes on the recipient's
/// phone rather than vCard mechanics.
/// </para>
/// <para>
/// Nothing here is confirmed on real devices yet — see
/// <see cref="ContactFieldGuidance.ConfirmedOnDeviceMatrix"/>. Where iOS and Android are known
/// to differ, or where a field commonly disappoints, that is stated in the caveat rather than
/// smoothed over.
/// </para>
/// </remarks>
public static class ContactFieldGuidanceCatalogue
{
    private static readonly Dictionary<ContactField, ContactFieldGuidance> Entries =
        BuildEntries().ToDictionary(guidance => guidance.Field);

    /// <summary>Guidance for every field, in editor order.</summary>
    public static IReadOnlyList<ContactFieldGuidance> All { get; } = [.. BuildEntries()];

    /// <summary>Returns the guidance for one field.</summary>
    /// <param name="field">The field to describe.</param>
    /// <returns>The guidance for that field.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The field has no guidance entry.</exception>
    public static ContactFieldGuidance For(ContactField field) =>
        Entries.TryGetValue(field, out var guidance)
            ? guidance
            : throw new ArgumentOutOfRangeException(
                nameof(field),
                field,
                "No tooltip guidance is defined for this field. Every field the editor offers must explain itself.");

    private static IEnumerable<ContactFieldGuidance> BuildEntries() =>
    [
        new()
        {
            Field = ContactField.GivenName,
            Label = "Given name",
            VCardProperty = "N, FN",
            WhenScanned = "Saved as the contact's first name, and used for the name shown at the top of the contact card.",
            PlatformCaveat = "Required. Without a name, iOS shows a blank contact and the entry is hard to find later.",
        },
        new()
        {
            Field = ContactField.FamilyName,
            Label = "Family name",
            VCardProperty = "N, FN",
            WhenScanned = "Saved as the contact's last name, and used for sorting in the phone's contact list.",
            PlatformCaveat = "Optional. Leave it empty for a client who uses one name — the code stays correct.",
        },
        new()
        {
            Field = ContactField.Company,
            Label = "Company",
            VCardProperty = "ORG",
            WhenScanned = "Saved as the company. On both phones it appears under the name, and it is what people search when they forget the person's name.",
        },
        new()
        {
            Field = ContactField.JobTitle,
            Label = "Job title",
            VCardProperty = "TITLE",
            WhenScanned = "Saved as the job title, shown beneath the name alongside the company.",
            PlatformCaveat = "Placement varies between Android contact apps. Some show it on the contact card, others only when editing.",
        },
        new()
        {
            Field = ContactField.Mobile,
            Label = "Mobile",
            VCardProperty = "TEL;TYPE=CELL",
            WhenScanned = "Saved as a mobile number and can be called or texted straight from the contact card.",
            PlatformCaveat = "Required. Include the country code — a number saved without one fails when the caller is abroad or on a foreign SIM.",
        },
        new()
        {
            Field = ContactField.SecondMobile,
            Label = "Second mobile",
            VCardProperty = "TEL;TYPE=CELL",
            WhenScanned = "Saved as a second mobile number. Both appear on the contact card labelled mobile.",
            PlatformCaveat = "Two numbers with the same label are valid, but some contact apps show them without distinguishing which is which.",
        },
        new()
        {
            Field = ContactField.WorkPhone,
            Label = "Work phone",
            VCardProperty = "TEL;TYPE=WORK,VOICE",
            WhenScanned = "Saved as a work number, labelled work on the contact card.",
        },
        new()
        {
            Field = ContactField.Fax,
            Label = "Fax",
            VCardProperty = "TEL;TYPE=WORK,FAX",
            WhenScanned = "Saved as a work fax number.",
            PlatformCaveat = "Rarely used and costs about 30 bytes. On a tight card this is usually the first field worth dropping.",
        },
        new()
        {
            Field = ContactField.WorkEmail,
            Label = "Work email",
            VCardProperty = "EMAIL;TYPE=INTERNET,WORK",
            WhenScanned = "Saved as a work email address and can be tapped to start a message.",
        },
        new()
        {
            Field = ContactField.PersonalEmail,
            Label = "Personal email",
            VCardProperty = "EMAIL;TYPE=INTERNET,HOME",
            WhenScanned = "Saved as a home email address, labelled separately from the work one.",
        },
        new()
        {
            Field = ContactField.Website,
            Label = "Website",
            VCardProperty = "URL",
            WhenScanned = "Saved as a website and can be tapped to open in the phone's browser.",
        },
        new()
        {
            Field = ContactField.SocialUrl,
            Label = "Social or profile link",
            VCardProperty = "URL",
            WhenScanned = "Saved as another website on the contact card.",
            PlatformCaveat = "It does not become a linked social profile. Neither phone recognises a LinkedIn or Instagram address as anything but a plain web link, so it looks the same as the website field.",
        },
        new()
        {
            Field = ContactField.Street,
            Label = "Street",
            VCardProperty = "ADR",
            WhenScanned = "Saved as the first line of a work address, which can be tapped to open in maps.",
            PlatformCaveat = "The full address adds roughly 80 bytes, which is often what pushes a card-sized code past the point of scanning reliably.",
        },
        new()
        {
            Field = ContactField.City,
            Label = "City",
            VCardProperty = "ADR",
            WhenScanned = "Saved as the city of the work address.",
        },
        new()
        {
            Field = ContactField.State,
            Label = "State or region",
            VCardProperty = "ADR",
            WhenScanned = "Saved as the state or region of the work address.",
        },
        new()
        {
            Field = ContactField.PostalCode,
            Label = "Postal code",
            VCardProperty = "ADR",
            WhenScanned = "Saved as the postal code of the work address.",
        },
        new()
        {
            Field = ContactField.Country,
            Label = "Country",
            VCardProperty = "ADR",
            WhenScanned = "Saved as the country of the work address.",
        },
        new()
        {
            Field = ContactField.Note,
            Label = "Note or tagline",
            VCardProperty = "NOTE",
            WhenScanned = "Saved into the contact's notes, where it is visible only after opening the contact.",
            PlatformCaveat = "The costliest field per character, and the least visible to the person who scanned. Usually the first thing to remove when a code is over budget.",
        },
    ];
}
