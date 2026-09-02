using ContactQR.Core.Scannability;
using ContactQR.Rendering;
using FluentAssertions;
using SkiaSharp;

namespace ContactQR.Rendering.Tests;

public sealed class PngDensityWriterTests
{
    [Theory]
    [InlineData(300)]
    [InlineData(600)]
    [InlineData(1_200)]
    public void WithResolution_RecordsTheResolutionThatIsReadBack(int dotsPerInch)
    {
        var png = EncodePng();

        var withDensity = PngDensityWriter.WithResolution(png, dotsPerInch);

        PngDensityWriter.ReadResolution(withDensity).Should().Be(dotsPerInch);
    }

    [Fact]
    public void SkiaSharpAlone_RecordsNoResolution_WhichIsWhyThisWriterExists()
    {
        // Pins the platform gap the design brief flagged. If SkiaSharp ever starts writing
        // pHYs itself, this test fails and the writer can be reconsidered.
        PngDensityWriter.ReadResolution(EncodePng()).Should().BeNull();
    }

    [Fact]
    public void WithResolution_ProducesAPngThatStillDecodes()
    {
        var png = PngDensityWriter.WithResolution(EncodePng(), 300);

        using var decoded = SKBitmap.Decode(png);

        decoded.Should().NotBeNull();
        decoded.Width.Should().Be(EncodedSideLength());
    }

    [Fact]
    public void WithResolution_ReplacesAnExistingChunk_RatherThanAddingASecond()
    {
        var once = PngDensityWriter.WithResolution(EncodePng(), 300);
        var twice = PngDensityWriter.WithResolution(once, 600);

        PngDensityWriter.ReadResolution(twice).Should().Be(600);
        twice.Length.Should().Be(once.Length, "a second pHYs chunk would make the file invalid");
    }

    [Fact]
    public void WithResolution_Throws_ForBytesThatAreNotAPng()
    {
        var write = () => PngDensityWriter.WithResolution([1, 2, 3, 4], 300);

        write.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void WithResolution_Throws_ForANonPositiveResolution()
    {
        var write = () => PngDensityWriter.WithResolution(EncodePng(), 0);

        write.Should().Throw<ArgumentOutOfRangeException>();
    }

    private static int EncodedSideLength()
    {
        var symbol = QrEncoder.Encode("BEGIN:VCARD\r\nEND:VCARD\r\n", ErrorCorrectionLevel.M);

        return symbol.TotalModulesPerSide * 4;
    }

    private static byte[] EncodePng()
    {
        var symbol = QrEncoder.Encode("BEGIN:VCARD\r\nEND:VCARD\r\n", ErrorCorrectionLevel.M);

        using var bitmap = QrImageRenderer.Render(symbol, modulePixels: 4, new QrRenderOptions());
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

        return encoded.ToArray();
    }
}
