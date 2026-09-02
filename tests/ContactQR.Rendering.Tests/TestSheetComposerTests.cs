using ContactQR.Core.Scannability;
using ContactQR.Rendering;
using FluentAssertions;
using SkiaSharp;

namespace ContactQR.Rendering.Tests;

public sealed class TestSheetComposerTests
{
    private const string SampleVCard =
        "BEGIN:VCARD\r\nVERSION:3.0\r\nN:D'Souza;Meera;;;\r\nFN:Meera D'Souza\r\n"
        + "ORG:Sunrise Physiotherapy\r\nTEL;TYPE=CELL:+919876543210\r\nEND:VCARD\r\n";

    private readonly TestSheetComposer composer = new();

    private static TestSheetRequest Request() => new()
    {
        Payload = SampleVCard,
        ErrorCorrection = ErrorCorrectionLevel.M,
    };

    [Fact]
    public void Compose_ProducesOneTilePerRequestedWidth()
    {
        var sheet = composer.Compose(Request());

        sheet.Tiles.Should().HaveCount(5);
    }

    [Fact]
    public void Compose_ProducesAnA4PageAt300Dpi()
    {
        var sheet = composer.Compose(Request());

        // 210 x 297 mm at 300 dpi.
        sheet.WidthPixels.Should().Be(2480);
        sheet.HeightPixels.Should().Be(3508);
    }

    [Fact]
    public void Compose_RecordsTheResolutionInThePng_SoItPrintsAtTrueSize()
    {
        var sheet = composer.Compose(Request());

        PngDensityWriter.ReadResolution(sheet.Png).Should().Be(300);
    }

    [Fact]
    public void Compose_ProducesADecodablePng()
    {
        var sheet = composer.Compose(Request());

        using var decoded = SKBitmap.Decode(sheet.Png);

        decoded.Width.Should().Be(sheet.WidthPixels);
    }

    [Fact]
    public void Compose_OrdersTilesBySize()
    {
        var sheet = composer.Compose(Request() with { WidthsMillimetres = [40m, 20m, 30m] });

        sheet.Tiles.Select(tile => tile.RequestedWidthMillimetres)
            .Should().BeInAscendingOrder();
    }

    [Fact]
    public void Compose_ReportsActualWidthNotRequestedWidth()
    {
        var sheet = composer.Compose(Request() with { WidthsMillimetres = [20m] });

        var tile = sheet.Tiles.Single();

        // Module size rounds to whole pixels, so the produced width is a little under the
        // requested one. The operator scans the real thing, so the real number is reported.
        tile.ActualWidthMillimetres.Should().BeLessThanOrEqualTo(tile.RequestedWidthMillimetres);
        tile.ActualWidthMillimetres.Should().BeGreaterThan(tile.RequestedWidthMillimetres - 1m);
    }

    [Fact]
    public void Compose_PredictsAVerdictConsistentWithTheModuleSizeItReports()
    {
        var thresholds = ScannabilityThresholds.Default;
        var sheet = composer.Compose(Request());

        foreach (var tile in sheet.Tiles)
        {
            var expected = tile.ModuleSizeMillimetres >= thresholds.SafeMillimetresPerModule
                ? ScannabilityVerdict.Safe
                : tile.ModuleSizeMillimetres >= thresholds.FloorMillimetresPerModule
                    ? ScannabilityVerdict.Marginal
                    : ScannabilityVerdict.WillFail;

            tile.Verdict.Should().Be(
                expected,
                "the printed label must agree with the module size printed beside it at {0} mm",
                tile.ActualWidthMillimetres);
        }
    }

    [Fact]
    public void Compose_VerifiesEveryTileDecodesBackToThePayload()
    {
        var sheet = composer.Compose(Request());

        sheet.Tiles.Should().OnlyContain(
            tile => tile.SelfTestPassed,
            "a test sheet carrying an undecodable code would teach the operator the wrong thing");
    }

    [Fact]
    public void Compose_ShowsSmallSizesFailingAndLargeOnesSafe()
    {
        // The whole point of the sheet: the operator sees the gradient rather than a claim.
        var sheet = composer.Compose(Request() with { WidthsMillimetres = [15m, 50m] });

        sheet.Tiles[0].Verdict.Should().Be(ScannabilityVerdict.WillFail);
        sheet.Tiles[1].Verdict.Should().Be(ScannabilityVerdict.Safe);
    }

    [Fact]
    public void Compose_HonoursACustomResolution()
    {
        var sheet = composer.Compose(Request() with { DotsPerInch = 600 });

        sheet.WidthPixels.Should().Be(4961);
        PngDensityWriter.ReadResolution(sheet.Png).Should().Be(600);
    }

    [Fact]
    public void Compose_Throws_WhenNoWidthsAreRequested()
    {
        var compose = () => composer.Compose(Request() with { WidthsMillimetres = [] });

        compose.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Compose_UsesTheInjectedThresholds_SoACalibratedSheetReportsCalibratedVerdicts()
    {
        var lenient = new TestSheetComposer(new ScannabilityCalculator(new ScannabilityThresholds
        {
            SafeMillimetresPerModule = 0.20m,
            FloorMillimetresPerModule = 0.10m,
        }));

        var sheet = lenient.Compose(Request() with { WidthsMillimetres = [20m] });

        sheet.Tiles.Single().Verdict.Should().Be(ScannabilityVerdict.Safe);
    }
}
