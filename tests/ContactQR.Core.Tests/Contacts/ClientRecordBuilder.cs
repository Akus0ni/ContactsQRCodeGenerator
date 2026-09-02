using ContactQR.Core.Contacts;

namespace ContactQR.Core.Tests.Contacts;

/// <summary>
/// Builds <see cref="ClientRecord"/> instances for tests, defaulting to the minimum valid
/// record so each test states only the field it is actually about.
/// </summary>
internal sealed class ClientRecordBuilder
{
    private readonly List<ContactPoint> contactPoints = [];
    private string givenName = "Meera";
    private string? familyName = "D'Souza";
    private string? company;
    private string? jobTitle;
    private string? note;
    private PostalAddress? address;
    private bool hasPrimaryPhone = true;

    public ClientRecordBuilder WithGivenName(string value)
    {
        givenName = value;
        return this;
    }

    public ClientRecordBuilder WithFamilyName(string? value)
    {
        familyName = value;
        return this;
    }

    public ClientRecordBuilder WithCompany(string? value)
    {
        company = value;
        return this;
    }

    public ClientRecordBuilder WithJobTitle(string? value)
    {
        jobTitle = value;
        return this;
    }

    public ClientRecordBuilder WithNote(string? value)
    {
        note = value;
        return this;
    }

    public ClientRecordBuilder WithAddress(PostalAddress? value)
    {
        address = value;
        return this;
    }

    public ClientRecordBuilder WithoutPrimaryPhone()
    {
        hasPrimaryPhone = false;
        return this;
    }

    public ClientRecordBuilder With(ContactPoint point)
    {
        contactPoints.Add(point);
        return this;
    }

    public ClientRecord Build()
    {
        var points = new List<ContactPoint>();

        if (hasPrimaryPhone)
        {
            points.Add(new ContactPoint
            {
                Kind = ContactPointKind.Phone,
                Subtype = ContactPointSubtype.Mobile,
                RawValue = "+919876543210",
                IsPrimary = true,
            });
        }

        points.AddRange(contactPoints);

        return new ClientRecord
        {
            GivenName = givenName,
            FamilyName = familyName,
            Company = company,
            JobTitle = jobTitle,
            Note = note,
            Address = address,
            ContactPoints = points,
        };
    }
}
