using ContactQR.Core.Scannability;
using FluentAssertions;

namespace ContactQR.Core.Tests.Scannability;

public sealed class ScannabilityCalculatorTests
{
    private readonly ScannabilityCalculator calculator = new();

    [Fact]
    public void EffectiveErrorCorrection_ForcesH_WhenALogoIsPresent()
    {
        ScannabilityCalculator.EffectiveErrorCorrection(ErrorCorrectionLevel.M, hasLogo: true)
            .Should().Be(ErrorCorrectionLevel.H);
    }

    [Fact]
    public void EffectiveErrorCorrection_KeepsTheSelectedLevel_WhenThereIsNoLogo()
    {
        ScannabilityCalculator.EffectiveErrorCorrection(ErrorCorrectionLevel.M, hasLogo: false)
            .Should().Be(ErrorCorrectionLevel.M);
    }

    [Fact]
    public void Assess_ChoosesTheSmallestVersionThatHoldsThePayload()
    {
        // Version 12 at M holds 287 bytes; version 11 holds only 251.
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 30m);

        assessment.Version.Should().Be(12);
    }

    [Fact]
    public void Assess_AddsTheQuietZoneToBothEdges()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 30m);

        // Version 12 is 65 modules per side, plus four modules of quiet zone on each edge.
        assessment.ModulesPerSide.Should().Be(65);
        assessment.TotalModulesPerSide.Should().Be(73);
    }

    [Fact]
    public void Assess_DividesPrintWidthByTotalModules()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 30m);

        assessment.ModuleSizeMillimetres.Should().BeApproximately(0.411m, 0.001m);
    }

    [Fact]
    public void Assess_ReportsSafe_WhenModuleSizeReachesTheSafeThreshold()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 30m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.Safe);
    }

    [Fact]
    public void Assess_ReportsMarginal_BetweenTheFloorAndTheSafeThreshold()
    {
        // 73 total modules at 25mm gives about 0.342mm per module.
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 25m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.Marginal);
    }

    [Fact]
    public void Assess_ReportsWillFail_BelowTheFloor()
    {
        // 73 total modules at 20mm gives about 0.274mm per module.
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 20m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.WillFail);
    }

    [Fact]
    public void Assess_BlocksExport_WhenTheVerdictIsWillFail()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 20m);

        assessment.BlocksExport.Should().BeTrue();
    }

    [Fact]
    public void Assess_DoesNotBlockExport_WhenTheVerdictIsMarginal()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 25m);

        assessment.BlocksExport.Should().BeFalse();
    }

    [Fact]
    public void Assess_ReportsAMinimumSafeWidthThatActuallyReachesSafe()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 20m);

        var atMinimum = calculator.Assess(
            260,
            ErrorCorrectionLevel.M,
            assessment.MinimumSafeWidthMillimetres);

        atMinimum.Verdict.Should().Be(ScannabilityVerdict.Safe);
    }

    [Fact]
    public void Assess_ReportsExceedsCapacity_WhenThePayloadDoesNotFitVersion40()
    {
        var assessment = calculator.Assess(1_400, ErrorCorrectionLevel.H, printWidthMillimetres: 30m);

        assessment.Verdict.Should().Be(ScannabilityVerdict.ExceedsCapacity);
    }

    [Fact]
    public void Assess_ReportsOverflowMagnitude_SoTheInterfaceCanShowHowFarOverBudgetItIs()
    {
        var assessment = calculator.Assess(1_400, ErrorCorrectionLevel.H, printWidthMillimetres: 30m);

        // Version 40 at H holds 1273 bytes.
        assessment.OverflowBytes.Should().Be(127);
    }

    [Fact]
    public void Assess_ReportsNoOverflow_WhenThePayloadFits()
    {
        var assessment = calculator.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 30m);

        assessment.OverflowBytes.Should().Be(0);
    }

    [Fact]
    public void Assess_ShowsThatRemovingALogoIsTheLargestSingleLever()
    {
        const int payloadBytes = 250;
        const decimal widthMillimetres = 25m;

        var withLogo = calculator.Assess(payloadBytes, ErrorCorrectionLevel.H, widthMillimetres);
        var withoutLogo = calculator.Assess(payloadBytes, ErrorCorrectionLevel.M, widthMillimetres);

        withoutLogo.ModuleSizeMillimetres.Should().BeGreaterThan(withLogo.ModuleSizeMillimetres);
        withoutLogo.Version.Should().BeLessThan(withLogo.Version);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(3)]
    public void Assess_Throws_WhenQuietZoneIsBelowTheSpecificationMinimum(int quietZoneModules)
    {
        var assess = () => calculator.Assess(250, ErrorCorrectionLevel.M, 25m, quietZoneModules);

        assess.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Assess_Throws_WhenPrintWidthIsNotPositive(decimal widthMillimetres)
    {
        var assess = () => calculator.Assess(250, ErrorCorrectionLevel.M, widthMillimetres);

        assess.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Assess_UsesInjectedThresholds_SoCalibrationIsAConfigurationChange()
    {
        var lenient = new ScannabilityCalculator(new ScannabilityThresholds
        {
            SafeMillimetresPerModule = 0.20m,
            FloorMillimetresPerModule = 0.10m,
        });

        // This width is WillFail under the default thresholds.
        lenient.Assess(260, ErrorCorrectionLevel.M, printWidthMillimetres: 20m)
            .Verdict.Should().Be(ScannabilityVerdict.Safe);
    }
}
