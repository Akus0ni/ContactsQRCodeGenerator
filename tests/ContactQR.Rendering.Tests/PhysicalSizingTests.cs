using ContactQR.Core.Scannability;
using ContactQR.Rendering;
using FluentAssertions;

namespace ContactQR.Rendering.Tests;

/// <summary>
/// Pins the physical-width defect found by printing a test sheet: flooring module size to whole
/// pixels made a 20 mm request come out at 15.5 mm, 23% narrow, which silently breaks a card
/// layout and invalidates the scannability verdict computed at the requested width.
/// </summary>
public sealed class PhysicalSizingTests
{
    [Theory]
    [InlineData(20)]
    [InlineData(25)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    public void Fit_ProducesTheRequestedWidth_ToWithinAQuarterMillimetre(int widthMillimetres)
    {
        var size = PhysicalSizing.Fit(61, widthMillimetres, 300);

        size.WidthMillimetres.Should().BeApproximately(widthMillimetres, 0.25m);
    }

    [Fact]
    public void Fit_NeverDropsBelowTheRequestedResolution()
    {
        for (var modules = 25; modules <= 185; modules += 4)
        {
            foreach (var width in new[] { 15m, 20m, 25m, 30m, 50m })
            {
                PhysicalSizing.Fit(modules, width, 300).DotsPerInch
                    .Should().BeGreaterThanOrEqualTo(300, "modules={0} width={1}", modules, width);
            }
        }
    }

    [Fact]
    public void Fit_KeepsModulesAWholeNumberOfPixels()
    {
        var size = PhysicalSizing.Fit(61, 20m, 300);

        size.SidePixels.Should().Be(size.ModulePixels * 61);
    }

    [Fact]
    public void Fit_DoesNotRepeatTheFlooringDefect()
    {
        // The exact case from the printed sheet: 61 total modules at 20 mm and 300 dpi.
        // Flooring gave 3 px modules and a 15.5 mm code.
        var size = PhysicalSizing.Fit(61, 20m, 300);

        size.ModulePixels.Should().Be(4);
        size.WidthMillimetres.Should().BeApproximately(20m, 0.1m);
    }

    [Fact]
    public void Export_HonoursTheRequestedWidth()
    {
        const string payload = "BEGIN:VCARD\r\nVERSION:3.0\r\nN:D'Souza;Meera;;;\r\nFN:Meera D'Souza\r\n"
            + "ORG:Sunrise Physiotherapy\r\nTITLE:Physiotherapist\r\nTEL;TYPE=CELL:+919876543210\r\nEND:VCARD\r\n";

        var result = new QrExporter().Export(new QrExportRequest
        {
            Payload = payload,
            ErrorCorrection = ErrorCorrectionLevel.M,
            WidthMillimetres = 25m,
        });

        result.ActualWidthMillimetres.Should().BeApproximately(25m, 0.25m);
        result.EffectiveDotsPerInch.Should().BeGreaterThanOrEqualTo(300);
        PngDensityWriter.ReadResolution(result.Png).Should().Be(result.EffectiveDotsPerInch);
    }

    [Fact]
    public void Export_RecordsAResolutionThatMakesThePngPrintAtTheRightSize()
    {
        var result = new QrExporter().Export(new QrExportRequest
        {
            Payload = "BEGIN:VCARD\r\nVERSION:3.0\r\nFN:Test\r\nTEL;TYPE=CELL:+911234567890\r\nEND:VCARD\r\n",
            ErrorCorrection = ErrorCorrectionLevel.M,
            WidthMillimetres = 30m,
        });

        var printedWidth = result.SidePixels * 25.4m / result.EffectiveDotsPerInch;

        printedWidth.Should().BeApproximately(30m, 0.25m);
    }
}
