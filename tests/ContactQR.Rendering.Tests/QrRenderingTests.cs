using ContactQR.Core.Scannability;
using ContactQR.Rendering;
using FluentAssertions;
using SkiaSharp;

namespace ContactQR.Rendering.Tests;

public sealed class QrRenderingTests
{
    private const string SampleVCard =
        "BEGIN:VCARD\r\nVERSION:3.0\r\nN:D'Souza;Meera;;;\r\nFN:Meera D'Souza\r\n"
        + "ORG:Sunrise Physiotherapy\r\nTEL;TYPE=CELL:+919876543210\r\nEND:VCARD\r\n";

    [Fact]
    public void Render_ProducesWholePixelModules()
    {
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.M);

        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 8, new QrRenderOptions());

        bitmap.Width.Should().Be(symbol.TotalModulesPerSide * 8);
        bitmap.Width.Should().Be(bitmap.Height);
    }

    [Fact]
    public void Render_ProducesNoIntermediateTones_SoModuleEdgesStaySharp()
    {
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.M);

        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 6, new QrRenderOptions());

        var distinctColours = bitmap.Pixels.Select(pixel => (uint)pixel).Distinct().ToArray();

        distinctColours.Should().HaveCount(
            2,
            "anti-aliased edges would misrepresent print quality in the direction that causes reprints");
    }

    [Fact]
    public void Render_LeavesTheQuietZoneClear()
    {
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.M);

        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 4, new QrRenderOptions());

        bitmap.GetPixel(0, 0).Should().Be(SKColors.White);
        bitmap.GetPixel(bitmap.Width - 1, bitmap.Height - 1).Should().Be(SKColors.White);
    }

    [Fact]
    public void Render_AppliesCustomColours()
    {
        var options = new QrRenderOptions
        {
            Foreground = new SKColor(0x1A, 0x1A, 0x1A),
            Background = new SKColor(0xF5, 0xF5, 0xF5),
        };
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.M);

        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 4, options);

        bitmap.GetPixel(0, 0).Should().Be(options.Background);
        bitmap.Pixels.Should().Contain(options.Foreground);
    }

    [Fact]
    public void Render_Throws_WhenTheLogoExceedsTheCoverageCap()
    {
        using var logo = CreateLogo();
        var options = new QrRenderOptions { Logo = logo, LogoWidthFraction = 0.40 };
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.H);

        var render = () => QrImageRenderer.Render(symbol, modulePixels: 6, options);

        render.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void SelfTest_Passes_ForAPlainCode()
    {
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.M);
        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 8, new QrRenderOptions());

        QrSelfTest.Verify(bitmap, SampleVCard).Passed.Should().BeTrue();
    }

    [Fact]
    public void SelfTest_RoundTripsNonAsciiExactly()
    {
        const string payload = "BEGIN:VCARD\r\nVERSION:3.0\r\nN:;मीरा;;;\r\nFN:मीरा\r\nTEL;TYPE=CELL:+919876543210\r\nEND:VCARD\r\n";

        var symbol = QrEncoder.Encode(payload, ErrorCorrectionLevel.M);
        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 8, new QrRenderOptions());

        QrSelfTest.Verify(bitmap, payload).Passed.Should().BeTrue();
    }

    [Fact]
    public void SelfTest_StillPasses_WithALogoAtTheDefaultCoverageAndEccH()
    {
        using var logo = CreateLogo();
        var options = new QrRenderOptions { Logo = logo };
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.H);

        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 10, options);

        QrSelfTest.Verify(bitmap, SampleVCard).Passed.Should().BeTrue(
            "the default 18% logo must remain decodable at level H, or the default is wrong");
    }

    [Fact]
    public void SelfTest_Fails_WhenTheCodeIsDeliberatelyDamaged()
    {
        var symbol = QrEncoder.Encode(SampleVCard, ErrorCorrectionLevel.L);
        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 8, new QrRenderOptions());

        using (var canvas = new SKCanvas(bitmap))
        using (var paint = new SKPaint { Color = SKColors.White })
        {
            canvas.DrawRect(0, 0, bitmap.Width, bitmap.Height / 2f, paint);
        }

        var result = QrSelfTest.Verify(bitmap, SampleVCard);

        result.Passed.Should().BeFalse();
        result.Diagnostics.Should().NotBeNullOrWhiteSpace();
    }

    private static SKImage CreateLogo()
    {
        var info = new SKImageInfo(120, 120);
        using var surface = SKSurface.Create(info);

        surface.Canvas.Clear(new SKColor(0x00, 0x75, 0x8C));

        return surface.Snapshot();
    }
}
