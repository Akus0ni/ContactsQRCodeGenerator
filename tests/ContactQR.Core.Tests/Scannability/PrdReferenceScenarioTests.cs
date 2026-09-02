using ContactQR.Core.Contacts;
using ContactQR.Core.Scannability;
using ContactQR.Core.VCard;
using FluentAssertions;

namespace ContactQR.Core.Tests.Scannability;

/// <summary>
/// Pins the three reference payloads the PRD reasons about, and the verdicts they reach at
/// real visiting-card widths. These are the numbers the product's central claim rests on, so
/// they are asserted rather than left in prose where they can drift away from the code.
/// </summary>
public sealed class PrdReferenceScenarioTests
{
    private readonly ScannabilityCalculator calculator = new();

    /// <summary>Name, company, one mobile.</summary>
    private static ClientRecord Minimal() => new()
    {
        GivenName = "Meera",
        FamilyName = "D'Souza",
        Company = "Sunrise Physiotherapy",
        ContactPoints =
        [
            new ContactPoint
            {
                Kind = ContactPointKind.Phone,
                Subtype = ContactPointSubtype.Mobile,
                RawValue = "+919876543210",
                IsPrimary = true,
            },
        ],
    };

    /// <summary>Minimal, plus job title, work phone, email and website.</summary>
    private static ClientRecord Typical() => Minimal() with
    {
        JobTitle = "Physiotherapist",
        ContactPoints =
        [
            .. Minimal().ContactPoints,
            new ContactPoint
            {
                Kind = ContactPointKind.Phone,
                Subtype = ContactPointSubtype.Work,
                RawValue = "+912226601234",
                SortOrder = 1,
            },
            new ContactPoint
            {
                Kind = ContactPointKind.Email,
                Subtype = ContactPointSubtype.Work,
                RawValue = "meera@sunrisephysio.in",
            },
            new ContactPoint
            {
                Kind = ContactPointKind.Url,
                Subtype = ContactPointSubtype.Social,
                RawValue = "https://sunrisephysio.in",
            },
        ],
    };

    /// <summary>Typical, plus postal address and a tagline.</summary>
    private static ClientRecord Full() => Typical() with
    {
        Address = new PostalAddress
        {
            Street = "12 MG Road",
            City = "Mumbai",
            State = "MH",
            PostalCode = "400001",
            Country = "India",
        },
        Note = "Sports injury and post-operative rehabilitation",
    };

    [Fact]
    public void MinimalPayload_IsAboutOneHundredAndThirtyBytes()
    {
        VCardEncoder.MeasureBytes(Minimal()).Should().BeInRange(110, 150);
    }

    [Fact]
    public void TypicalPayload_IsAboutTwoHundredAndFiftyBytes()
    {
        VCardEncoder.MeasureBytes(Typical()).Should().BeInRange(220, 280);
    }

    [Fact]
    public void FullPayload_IsAboutFourHundredBytes()
    {
        VCardEncoder.MeasureBytes(Full()).Should().BeInRange(360, 440);
    }

    [Fact]
    public void FullPayloadWithLogo_FailsAtVisitingCardWidth()
    {
        var assessment = Assess(Full(), hasLogo: true, widthMillimetres: 22m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.WillFail);
    }

    [Fact]
    public void FullPayloadWithoutLogo_StillFailsAtVisitingCardWidth()
    {
        // The logo is the largest single lever, but dropping it does not rescue a full
        // payload at 22mm. That is the constraint the operator has to take to the client.
        var assessment = Assess(Full(), hasLogo: false, widthMillimetres: 22m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.WillFail);
    }

    [Fact]
    public void MinimalPayloadWithoutLogo_IsOnlyMarginalAtTwentyTwoMillimetres()
    {
        // Measured, not assumed. The minimal vCard is 131 bytes, which needs version 8 at M
        // (version 7 holds only 122). Version 8 is 49 modules, 57 including the quiet zone,
        // giving 0.386mm at 22mm — below the 0.40mm safe threshold.
        //
        // This corrects the design brief, which projected a version 7 symbol and therefore
        // called 22mm safe for a minimal payload. Even name, company and one phone number is
        // Marginal on a 22mm card.
        var assessment = Assess(Minimal(), hasLogo: false, widthMillimetres: 22m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.Marginal);
        assessment.Version.Should().Be(8);
        assessment.TotalModulesPerSide.Should().Be(57);
    }

    [Fact]
    public void MinimalPayloadWithoutLogo_NeedsAboutTwentyThreeMillimetresToBeSafe()
    {
        var assessment = Assess(Minimal(), hasLogo: false, widthMillimetres: 22m);

        assessment.MinimumSafeWidthMillimetres.Should().BeApproximately(22.8m, 0.05m);
    }

    [Fact]
    public void MinimalPayloadWithoutLogo_IsSafeAtTwentyFiveMillimetres()
    {
        var assessment = Assess(Minimal(), hasLogo: false, widthMillimetres: 25m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.Safe);
    }

    [Fact]
    public void RemovingTheLogo_RecoversMoreThanAnyFieldRemoval()
    {
        const decimal width = 25m;

        var withLogo = Assess(Full(), hasLogo: true, width);
        var withoutLogo = Assess(Full(), hasLogo: false, width);
        var withoutNoteAndAddress = Assess(
            Full() with { Note = null, Address = null },
            hasLogo: true,
            width);

        withoutLogo.ModuleSizeMillimetres
            .Should().BeGreaterThan(withoutNoteAndAddress.ModuleSizeMillimetres);

        withLogo.ModuleSizeMillimetres
            .Should().BeLessThan(withoutLogo.ModuleSizeMillimetres);
    }

    [Fact]
    public void TypicalPayloadWithoutLogo_IsSafeAtThirtyMillimetres()
    {
        var assessment = Assess(Typical(), hasLogo: false, widthMillimetres: 30m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.Safe);
    }

    private ScannabilityAssessment Assess(ClientRecord client, bool hasLogo, decimal widthMillimetres) =>
        calculator.Assess(
            VCardEncoder.MeasureBytes(client),
            ScannabilityCalculator.EffectiveErrorCorrection(ErrorCorrectionLevel.M, hasLogo),
            widthMillimetres);
}
