using ContactQR.Core.Scannability;
using SkiaSharp;

namespace ContactQR.Rendering;

/// <summary>What to export, at what physical size.</summary>
public sealed record QrExportRequest
{
    /// <summary>The payload to encode, typically a vCard.</summary>
    public required string Payload { get; init; }

    /// <summary>The effective error-correction level, after any logo has forced H.</summary>
    public required ErrorCorrectionLevel ErrorCorrection { get; init; }

    /// <summary>The intended printed width in millimetres, including the quiet zone.</summary>
    public required decimal WidthMillimetres { get; init; }

    /// <summary>The output resolution. Below 300 is not print-usable.</summary>
    public int DotsPerInch { get; init; } = 300;

    /// <summary>Quiet zone per edge, never below four modules.</summary>
    public int QuietZoneModules { get; init; } = ScannabilityCalculator.MinimumQuietZoneModules;

    /// <summary>Colours and optional logo.</summary>
    public QrRenderOptions Render { get; init; } = new();
}

/// <summary>A rendered, verified export ready to be written to disk.</summary>
public sealed record QrExportResult
{
    /// <summary>The PNG bytes, carrying a correct <c>pHYs</c> resolution.</summary>
    public required byte[] Png { get; init; }

    /// <summary>The scannability assessment for this payload at this width.</summary>
    public required ScannabilityAssessment Assessment { get; init; }

    /// <summary>The decode-back self-test result.</summary>
    public required SelfTestResult SelfTest { get; init; }

    /// <summary>The size of one module in the output bitmap.</summary>
    public required int ModulePixels { get; init; }

    /// <summary>The bitmap's side length in pixels.</summary>
    public required int SidePixels { get; init; }

    /// <summary>
    /// The resolution recorded in the PNG, derived so the file prints at the requested width.
    /// At or above the requested resolution, never below it.
    /// </summary>
    public required int EffectiveDotsPerInch { get; init; }

    /// <summary>
    /// The width actually produced. Matches the request to within rounding, because the
    /// recorded resolution is derived rather than fixed — see <see cref="PhysicalSizing"/>.
    /// </summary>
    public required decimal ActualWidthMillimetres { get; init; }
}

/// <summary>
/// Turns a payload and a physical size into a verified PNG.
/// </summary>
/// <remarks>
/// This is where the two gates meet. The module-size verdict decides whether the code can
/// survive print; the decode-back self-test decides whether what was drawn still carries the
/// payload. Neither is sufficient alone, and neither may be skipped (PRD FR-6.9).
/// </remarks>
public sealed class QrExporter
{
    private readonly ScannabilityCalculator calculator;

    /// <summary>Creates an exporter using the uncalibrated default thresholds.</summary>
    public QrExporter()
        : this(new ScannabilityCalculator())
    {
    }

    /// <summary>Creates an exporter using an explicit calculator.</summary>
    /// <param name="calculator">The scannability calculator to gate exports with.</param>
    public QrExporter(ScannabilityCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(calculator);
        this.calculator = calculator;
    }

    /// <summary>
    /// Renders and verifies an export without writing it anywhere.
    /// </summary>
    /// <param name="request">What to export.</param>
    /// <returns>The PNG, its assessment, and the self-test result.</returns>
    /// <remarks>
    /// This never refuses on the strength of the verdict alone. A blocked verdict is data the
    /// caller acts on, because PRD FR-4.5 allows a deliberate, recorded override — while a
    /// failed self-test is not overridable and the caller must treat it as fatal.
    /// </remarks>
    public QrExportResult Export(QrExportRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var payloadBytes = System.Text.Encoding.UTF8.GetByteCount(request.Payload);
        var assessment = calculator.Assess(
            payloadBytes,
            request.ErrorCorrection,
            request.WidthMillimetres,
            request.QuietZoneModules);

        var symbol = QrEncoder.Encode(request.Payload, request.ErrorCorrection, request.QuietZoneModules);
        var sizing = PhysicalSizing.Fit(
            symbol.TotalModulesPerSide,
            request.WidthMillimetres,
            request.DotsPerInch);

        using var bitmap = QrImageRenderer.Render(symbol, sizing.ModulePixels, request.Render);
        var selfTest = QrSelfTest.Verify(bitmap, request.Payload);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
        var png = PngDensityWriter.WithResolution(encoded.ToArray(), sizing.DotsPerInch);

        return new QrExportResult
        {
            Png = png,
            Assessment = assessment,
            SelfTest = selfTest,
            ModulePixels = sizing.ModulePixels,
            SidePixels = sizing.SidePixels,
            EffectiveDotsPerInch = sizing.DotsPerInch,
            ActualWidthMillimetres = sizing.WidthMillimetres,
        };
    }
}
