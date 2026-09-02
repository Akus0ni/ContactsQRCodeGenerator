using ContactQR.Core.Scannability;
using SkiaSharp;

namespace ContactQR.Rendering;

/// <summary>What to put on a physical scan-test sheet.</summary>
public sealed record TestSheetRequest
{
    /// <summary>The payload to encode, typically a vCard.</summary>
    public required string Payload { get; init; }

    /// <summary>The effective error-correction level, after any logo has forced H.</summary>
    public required ErrorCorrectionLevel ErrorCorrection { get; init; }

    /// <summary>The widths to print, in millimetres.</summary>
    public IReadOnlyList<decimal> WidthsMillimetres { get; init; } = [20m, 25m, 30m, 40m, 50m];

    /// <summary>Sheet resolution. 300 is the print standard.</summary>
    public int DotsPerInch { get; init; } = 300;

    /// <summary>Shown in the sheet heading so a printed proof can be traced back to a client.</summary>
    public string? ClientName { get; init; }

    /// <summary>Colours and optional logo, matching what will actually be exported.</summary>
    public QrRenderOptions Render { get; init; } = new();
}

/// <summary>One code on the sheet, with what the application predicted for it.</summary>
/// <param name="RequestedWidthMillimetres">The width asked for.</param>
/// <param name="ActualWidthMillimetres">The width produced after module sizes were rounded to whole pixels.</param>
/// <param name="ModuleSizeMillimetres">The module size at the actual width.</param>
/// <param name="Verdict">What the application predicts will happen.</param>
/// <param name="SelfTestPassed">Whether the rendered tile decoded back to the payload.</param>
public readonly record struct TestSheetTile(
    decimal RequestedWidthMillimetres,
    decimal ActualWidthMillimetres,
    decimal ModuleSizeMillimetres,
    ScannabilityVerdict Verdict,
    bool SelfTestPassed);

/// <summary>A composed sheet, ready to print or save.</summary>
public sealed record TestSheetResult
{
    /// <summary>The sheet as a PNG carrying its resolution.</summary>
    public required byte[] Png { get; init; }

    /// <summary>What was placed on the sheet, in order.</summary>
    public required IReadOnlyList<TestSheetTile> Tiles { get; init; }

    /// <summary>Sheet width in pixels.</summary>
    public required int WidthPixels { get; init; }

    /// <summary>Sheet height in pixels.</summary>
    public required int HeightPixels { get; init; }
}

/// <summary>
/// Composes a single printable page carrying the same code at several physical sizes.
/// </summary>
/// <remarks>
/// <para>
/// PRD FR-4.6. This is what converts the module-size argument from an assertion into something
/// the operator can verify with his own phone in thirty seconds, before committing a press run.
/// </para>
/// <para>
/// It is also how the thresholds get calibrated. PRD M1b requires the 0.40 and 0.30 mm figures
/// to be corrected by measurement rather than taken from published guidance, and every printed
/// sheet is one run of that experiment — which is why each tile is labelled with the prediction
/// the application made, not merely with its size.
/// </para>
/// </remarks>
public sealed class TestSheetComposer
{
    private const decimal MillimetresPerInch = 25.4m;
    private const decimal A4WidthMillimetres = 210m;
    private const decimal A4HeightMillimetres = 297m;
    private const decimal MarginMillimetres = 15m;

    private readonly ScannabilityCalculator calculator;

    /// <summary>Creates a composer using the uncalibrated default thresholds.</summary>
    public TestSheetComposer()
        : this(new ScannabilityCalculator())
    {
    }

    /// <summary>Creates a composer using an explicit calculator.</summary>
    /// <param name="calculator">The calculator whose predictions the sheet reports.</param>
    public TestSheetComposer(ScannabilityCalculator calculator)
    {
        ArgumentNullException.ThrowIfNull(calculator);
        this.calculator = calculator;
    }

    /// <summary>
    /// Composes an A4 sheet with the payload rendered at each requested width.
    /// </summary>
    /// <param name="request">What to place on the sheet.</param>
    /// <returns>The sheet and the per-tile predictions.</returns>
    /// <exception cref="ArgumentException">No widths were requested.</exception>
    public TestSheetResult Compose(TestSheetRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.WidthsMillimetres.Count is 0)
        {
            throw new ArgumentException("A test sheet needs at least one width.", nameof(request));
        }

        var pageWidth = ToPixels(A4WidthMillimetres, request.DotsPerInch);
        var pageHeight = ToPixels(A4HeightMillimetres, request.DotsPerInch);
        var margin = ToPixels(MarginMillimetres, request.DotsPerInch);

        using var surface = SKSurface.Create(new SKImageInfo(pageWidth, pageHeight));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.White);

        var scale = request.DotsPerInch / 300f;
        using var headingFont = new SKFont(SKTypeface.Default, 30 * scale);
        using var captionFont = new SKFont(SKTypeface.Default, 15 * scale);
        using var sizeFont = new SKFont(SKTypeface.Default, 22 * scale);
        using var detailFont = new SKFont(SKTypeface.Default, 14 * scale);
        using var ink = new SKPaint { Color = SKColors.Black, IsAntialias = true };
        using var greyInk = new SKPaint { Color = new SKColor(0x55, 0x55, 0x55), IsAntialias = true };

        var cursorY = margin + headingFont.Size;
        canvas.DrawText(HeadingFor(request), margin, cursorY, SKTextAlign.Left, headingFont, ink);

        cursorY += captionFont.Size * 1.8f;
        foreach (var line in CaptionLines())
        {
            canvas.DrawText(line, margin, cursorY, SKTextAlign.Left, captionFont, greyInk);
            cursorY += captionFont.Size * 1.5f;
        }

        cursorY += captionFont.Size;

        var tiles = new List<TestSheetTile>();
        var gap = ToPixels(6m, request.DotsPerInch);
        var labelX = margin + ToPixels(55m, request.DotsPerInch);

        foreach (var width in request.WidthsMillimetres.OrderBy(width => width))
        {
            var tile = RenderTile(request, width, out var bitmap);

            using (bitmap)
            {
                canvas.DrawBitmap(
                    bitmap,
                    margin,
                    cursorY,
                    new SKSamplingOptions(SKFilterMode.Nearest, SKMipmapMode.None),
                    null);

                // The label sits beside the code and centred on it, so a row reads as one fact
                // rather than as a caption hunting for its picture.
                var centreY = cursorY + (bitmap.Height / 2f);

                canvas.DrawText(
                    $"{tile.ActualWidthMillimetres:0.0} mm",
                    labelX,
                    centreY,
                    SKTextAlign.Left,
                    sizeFont,
                    ink);

                canvas.DrawText(
                    $"{tile.ModuleSizeMillimetres:0.00} mm per module  ·  {WordFor(tile.Verdict)}",
                    labelX,
                    centreY + (detailFont.Size * 1.6f),
                    SKTextAlign.Left,
                    detailFont,
                    greyInk);

                canvas.DrawText(
                    "Scanned:      yes  /  no",
                    labelX,
                    centreY + (detailFont.Size * 3.2f),
                    SKTextAlign.Left,
                    detailFont,
                    greyInk);

                cursorY += Math.Max(bitmap.Height, detailFont.Size * 5) + gap;
            }

            tiles.Add(tile);
        }

        DrawFooter(canvas, request, pageHeight, margin, captionFont, greyInk);

        using var image = surface.Snapshot();
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

        return new TestSheetResult
        {
            Png = PngDensityWriter.WithResolution(encoded.ToArray(), request.DotsPerInch),
            Tiles = tiles,
            WidthPixels = pageWidth,
            HeightPixels = pageHeight,
        };
    }

    private TestSheetTile RenderTile(
        TestSheetRequest request,
        decimal widthMillimetres,
        out SKBitmap bitmap)
    {
        var payloadBytes = System.Text.Encoding.UTF8.GetByteCount(request.Payload);
        var symbol = QrEncoder.Encode(request.Payload, request.ErrorCorrection);

        // Every tile is drawn at the sheet's own resolution so they share one scale, but the
        // module size is chosen for this width. See PhysicalSizing for why that is not a floor.
        var sizing = PhysicalSizing.Fit(symbol.TotalModulesPerSide, widthMillimetres, request.DotsPerInch);
        var scaled = Math.Max(1, (int)Math.Round(
            (double)(widthMillimetres / MillimetresPerInch * request.DotsPerInch) / symbol.TotalModulesPerSide));

        bitmap = QrImageRenderer.Render(symbol, scaled, request.Render);

        var actualWidth = bitmap.Width * MillimetresPerInch / request.DotsPerInch;
        var actualAssessment = calculator.Assess(payloadBytes, request.ErrorCorrection, actualWidth);

        return new TestSheetTile(
            widthMillimetres,
            actualWidth,
            actualAssessment.ModuleSizeMillimetres,
            actualAssessment.Verdict,
            QrSelfTest.Verify(bitmap, request.Payload).Passed);
    }

    private static string HeadingFor(TestSheetRequest request) =>
        string.IsNullOrWhiteSpace(request.ClientName)
            ? "QR scan test sheet"
            : $"QR scan test sheet — {request.ClientName}";

    private static IEnumerable<string> CaptionLines() =>
    [
        "Print this at 100% — no scaling, no fit-to-page — then scan each code with a stock phone camera.",
        "Hold the phone about 20 cm away, indoors, and give each one three seconds before moving on.",
        "Note which sizes worked. Where reality and the prediction below disagree, reality wins.",
    ];

    private static void DrawFooter(
        SKCanvas canvas,
        TestSheetRequest request,
        int pageHeight,
        int margin,
        SKFont font,
        SKPaint paint)
    {
        var thresholds = ScannabilityThresholds.Default;
        var footerY = pageHeight - margin;

        canvas.DrawText(
            $"Predicted thresholds: Safe at or above {thresholds.SafeMillimetresPerModule:0.00} mm per module, "
            + $"failing below {thresholds.FloorMillimetresPerModule:0.00} mm.",
            margin,
            footerY - (font.Size * 1.5f),
            SKTextAlign.Left,
            font,
            paint);

        canvas.DrawText(
            $"Error correction {request.ErrorCorrection} · {request.DotsPerInch} dpi · "
            + "these thresholds are an estimate awaiting calibration against real devices.",
            margin,
            footerY,
            SKTextAlign.Left,
            font,
            paint);
    }

    private static string WordFor(ScannabilityVerdict verdict) => verdict switch
    {
        ScannabilityVerdict.Safe => "predicted Safe",
        ScannabilityVerdict.Marginal => "predicted Marginal",
        ScannabilityVerdict.WillFail => "predicted to fail",
        _ => "too much data",
    };

    private static int ToPixels(decimal millimetres, int dotsPerInch) =>
        (int)Math.Round(millimetres / MillimetresPerInch * dotsPerInch);
}
