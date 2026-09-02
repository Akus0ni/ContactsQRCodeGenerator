using ContactQR.Core.Contacts;
using FluentAssertions;

namespace ContactQR.Core.Tests.Contacts;

public sealed class ContactFieldGuidanceCatalogueTests
{
    public static TheoryData<ContactField> EveryField { get; } = [.. Enum.GetValues<ContactField>()];

    [Theory]
    [MemberData(nameof(EveryField))]
    public void For_ReturnsGuidance_ForEveryFieldTheEditorOffers(ContactField field)
    {
        var guidance = ContactFieldGuidanceCatalogue.For(field);

        guidance.Field.Should().Be(field);
    }

    [Theory]
    [MemberData(nameof(EveryField))]
    public void For_ExplainsWhatTheFieldBecomesOnTheScanningPhone(ContactField field)
    {
        ContactFieldGuidanceCatalogue.For(field).WhenScanned.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(EveryField))]
    public void For_NamesTheVCardPropertyTheFieldIsEncodedAs(ContactField field)
    {
        ContactFieldGuidanceCatalogue.For(field).VCardProperty.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [MemberData(nameof(EveryField))]
    public void For_GivesTheFieldALabel(ContactField field)
    {
        ContactFieldGuidanceCatalogue.For(field).Label.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void All_CoversEveryFieldExactlyOnce()
    {
        ContactFieldGuidanceCatalogue.All
            .Select(guidance => guidance.Field)
            .Should().BeEquivalentTo(Enum.GetValues<ContactField>());
    }

    [Theory]
    [MemberData(nameof(EveryField))]
    public void For_DoesNotClaimDeviceConfirmation_BeforeTheMatrixHasRun(ContactField field)
    {
        // PRD EC-30 and M1a: per-platform behaviour is characterised by measurement, not by
        // reading the specification. Flipping any of these to true is a deliberate act that
        // follows a device test. This assertion is expected to be revisited then, not deleted.
        ContactFieldGuidanceCatalogue.For(field).ConfirmedOnDeviceMatrix.Should().BeFalse();
    }

    [Fact]
    public void SocialLink_WarnsThatItDoesNotBecomeALinkedProfile()
    {
        // The most common false expectation an operator will carry into a client conversation.
        var guidance = ContactFieldGuidanceCatalogue.For(ContactField.SocialUrl);

        guidance.PlatformCaveat.Should().NotBeNullOrWhiteSpace();
        guidance.PlatformCaveat.Should().Contain("plain web link");
    }

    [Fact]
    public void Mobile_WarnsAboutTheMissingCountryCode()
    {
        var guidance = ContactFieldGuidanceCatalogue.For(ContactField.Mobile);

        guidance.PlatformCaveat.Should().Contain("country code");
    }

    [Fact]
    public void For_Throws_ForAFieldOutsideTheEnumeration()
    {
        var lookup = () => ContactFieldGuidanceCatalogue.For((ContactField)9_999);

        lookup.Should().Throw<ArgumentOutOfRangeException>();
    }
}
