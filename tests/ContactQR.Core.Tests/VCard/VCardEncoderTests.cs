using System.Text;
using ContactQR.Core.Contacts;
using ContactQR.Core.Tests.Contacts;
using ContactQR.Core.VCard;
using FluentAssertions;

namespace ContactQR.Core.Tests.VCard;

public sealed class VCardEncoderTests
{
    [Fact]
    public void Encode_UsesCarriageReturnLineFeed_BecauseBareLineFeedFailsIntermittentlyOnIos()
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Replace("\r\n", string.Empty, StringComparison.Ordinal)
            .Should().NotContain("\n");
    }

    [Fact]
    public void Encode_OpensWithBeginThenVersion()
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Should().StartWith("BEGIN:VCARD\r\nVERSION:3.0\r\n");
    }

    [Fact]
    public void Encode_ClosesWithEnd()
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Should().EndWith("END:VCARD\r\n");
    }

    [Fact]
    public void Encode_EmitsStructuredNameWithAllFiveComponents()
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Should().Contain("N:D'Souza;Meera;;;\r\n");
    }

    [Fact]
    public void Encode_EmitsFormattedName_BecauseItsAbsenceMakesIosShowABlankContact()
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Should().Contain("FN:Meera D'Souza\r\n");
    }

    [Fact]
    public void Encode_EmitsMononymWithEmptyFamilyComponent()
    {
        var client = new ClientRecordBuilder().WithGivenName("Suharto").WithFamilyName(null).Build();

        var vcard = VCardEncoder.Encode(client);

        vcard.Should().Contain("N:;Suharto;;;\r\n").And.Contain("FN:Suharto\r\n");
    }

    [Fact]
    public void Encode_EscapesCommaInCompanyName()
    {
        var client = new ClientRecordBuilder().WithCompany("Acme Interiors Pvt Ltd, Mumbai").Build();

        var vcard = VCardEncoder.Encode(client);

        vcard.Should().Contain("ORG:Acme Interiors Pvt Ltd\\, Mumbai\r\n");
    }

    [Fact]
    public void Encode_EscapesNewlineInNote_SoTheRestOfTheCardIsNotCorrupted()
    {
        var client = new ClientRecordBuilder().WithNote("Sports injury\r\nPost-op rehab").Build();

        var vcard = VCardEncoder.Encode(client);

        vcard.Should().Contain("NOTE:Sports injury\\nPost-op rehab\r\n");
    }

    [Theory]
    [InlineData("ORG")]
    [InlineData("TITLE")]
    [InlineData("NOTE")]
    [InlineData("ADR")]
    public void Encode_OmitsEmptyProperties_RatherThanEmittingThemBare(string propertyName)
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Should().NotContain($"{propertyName}:");
    }

    [Fact]
    public void Encode_EmitsPrimaryMobileAsCellType()
    {
        var vcard = VCardEncoder.Encode(new ClientRecordBuilder().Build());

        vcard.Should().Contain("TEL;TYPE=CELL:+919876543210\r\n");
    }

    [Fact]
    public void Encode_EmitsWorkPhoneWithUnescapedTypeParameterComma()
    {
        var client = new ClientRecordBuilder()
            .With(new ContactPoint
            {
                Kind = ContactPointKind.Phone,
                Subtype = ContactPointSubtype.Work,
                RawValue = "+912212345678",
            })
            .Build();

        var vcard = VCardEncoder.Encode(client);

        // The comma in TYPE=WORK,VOICE is a parameter delimiter, not a value, so it is not escaped.
        vcard.Should().Contain("TEL;TYPE=WORK,VOICE:+912212345678\r\n");
    }

    [Fact]
    public void Encode_EmitsAllSevenAddressComponents_SoValuesDoNotShiftIntoTheWrongFields()
    {
        var client = new ClientRecordBuilder()
            .WithAddress(new PostalAddress
            {
                Street = "12 MG Road",
                City = "Mumbai",
                State = "MH",
                PostalCode = "400001",
                Country = "India",
            })
            .Build();

        var vcard = VCardEncoder.Encode(client);

        vcard.Should().Contain("ADR;TYPE=WORK:;;12 MG Road;Mumbai;MH;400001;India\r\n");
    }

    [Fact]
    public void Encode_PreservesEmptyAddressComponentsAsConsecutiveSemicolons()
    {
        var client = new ClientRecordBuilder()
            .WithAddress(new PostalAddress { City = "Mumbai", Country = "India" })
            .Build();

        var vcard = VCardEncoder.Encode(client);

        vcard.Should().Contain("ADR;TYPE=WORK:;;;Mumbai;;;India\r\n");
    }

    [Fact]
    public void Encode_OmitsAddress_WhenEveryComponentIsEmpty()
    {
        var client = new ClientRecordBuilder().WithAddress(new PostalAddress()).Build();

        VCardEncoder.Encode(client).Should().NotContain("ADR");
    }

    [Fact]
    public void Encode_EmitsPropertiesInFixedOrder_SoIdenticalInputProducesAByteIdenticalCode()
    {
        var client = new ClientRecordBuilder()
            .WithCompany("Sunrise Physiotherapy")
            .WithJobTitle("Physiotherapist")
            .WithNote("Sports injury and post-op rehab")
            .WithAddress(new PostalAddress { City = "Mumbai" })
            .With(new ContactPoint
            {
                Kind = ContactPointKind.Email,
                Subtype = ContactPointSubtype.Work,
                RawValue = "meera@sunrisephysio.in",
            })
            .With(new ContactPoint
            {
                Kind = ContactPointKind.Url,
                Subtype = ContactPointSubtype.Social,
                RawValue = "https://sunrisephysio.in",
            })
            .Build();

        var propertyNames = VCardEncoder.Encode(client)
            .Split("\r\n", StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Split(':', ';')[0])
            .ToArray();

        propertyNames.Should().Equal(
            "BEGIN", "VERSION", "N", "FN", "ORG", "TITLE", "TEL", "EMAIL", "URL", "ADR", "NOTE", "END");
    }

    [Fact]
    public void Encode_IsDeterministic_ForTheSameInput()
    {
        var client = new ClientRecordBuilder().WithCompany("Sunrise Physiotherapy").Build();

        VCardEncoder.Encode(client).Should().Be(VCardEncoder.Encode(client));
    }

    [Fact]
    public void Encode_DoesNotFoldLongLines()
    {
        var client = new ClientRecordBuilder().WithNote(new string('a', 200)).Build();

        var noteLine = VCardEncoder.Encode(client)
            .Split("\r\n")
            .Single(line => line.StartsWith("NOTE:", StringComparison.Ordinal));

        noteLine.Should().HaveLength(205);
    }

    [Fact]
    public void Encode_Throws_WhenGivenNameIsMissing()
    {
        var client = new ClientRecordBuilder().WithGivenName("  ").Build();

        var encode = () => VCardEncoder.Encode(client);

        encode.Should().Throw<ArgumentException>().WithMessage("*given name*");
    }

    [Fact]
    public void Encode_Throws_WhenPrimaryPhoneIsMissing()
    {
        var client = new ClientRecordBuilder().WithoutPrimaryPhone().Build();

        var encode = () => VCardEncoder.Encode(client);

        encode.Should().Throw<ArgumentException>().WithMessage("*primary phone*");
    }

    [Fact]
    public void MeasureBytes_CountsUtf8Bytes_NotCharacters()
    {
        var latin = new ClientRecordBuilder().WithGivenName("Meera").WithFamilyName(null).Build();
        var devanagari = new ClientRecordBuilder().WithGivenName("मीरा").WithFamilyName(null).Build();

        // Same character count, roughly triple the bytes. Counting characters would let this
        // payload cross a capacity ceiling invisibly.
        VCardEncoder.MeasureBytes(devanagari).Should().BeGreaterThan(VCardEncoder.MeasureBytes(latin));
    }

    [Fact]
    public void MeasureBytes_AgreesWithEncodedPayload()
    {
        var client = new ClientRecordBuilder().WithCompany("Sunrise Physiotherapy").Build();

        VCardEncoder.MeasureBytes(client)
            .Should().Be(Encoding.UTF8.GetByteCount(VCardEncoder.Encode(client)));
    }
}
