using SkiaSharp;

namespace ContactQR.Rendering;

/// <summary>
/// Draws a <see cref="QrSymbol"/> to a bitmap.
/// </summary>
/// <remarks>
/// Module edges must stay sharp. Every module is a whole number of pixels and nothing is
/// anti-aliased, because grey edge pixels misrepresent print quality in exactly the direction
/// that causes reprints (PRD FR-3.7, design principle P3).
/// </remarks>
public static class QrImageRenderer
{
    /// <summary>
    /// Renders a symbol at a whole number of pixels per module.
    /// </summary>
    /// <param name="symbol">The symbol to draw.</param>
    /// <param name="modulePixels">The size of one module in pixels. Must be at least 1.</param>
    /// <param name="options">Colours and optional logo.</param>
    /// <returns>The rendered bitmap. The caller owns it.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// The module size is below 1, or the logo exceeds <see cref="QrRenderOptions.MaximumLogoWidthFraction"/>.
    /// </exception>
    public static SKBitmap Render(QrSymbol symbol, int modulePixels, QrRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfLessThan(modulePixels, 1);

        if (options.HasLogo && options.LogoWidthFraction > QrRenderOptions.MaximumLogoWidthFraction)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options),
                options.LogoWidthFraction,
                $"A logo may not exceed {QrRenderOptions.MaximumLogoWidthFraction:P0} of the symbol width. "
                + "Beyond that it consumes error-correction budget that print defects, lighting and card wear need.");
        }

        var sidePixels = symbol.TotalModulesPerSide * modulePixels;
        var bitmap = new SKBitmap(sidePixels, sidePixels, SKColorType.Rgba8888, SKAlphaType.Opaque);

        using (var canvas = new SKCanvas(bitmap))
        {
            canvas.Clear(options.Background);
            DrawModules(canvas, symbol, modulePixels, options.Foreground);

            if (options.Logo is not null)
            {
                DrawLogo(canvas, symbol, modulePixels, options);
            }
        }

        return bitmap;
    }

    /// <summary>Renders a symbol and encodes it as a PNG carrying its physical resolution.</summary>
    /// <param name="symbol">The symbol to draw.</param>
    /// <param name="modulePixels">The size of one module in pixels.</param>
    /// <param name="options">Colours and optional logo.</param>
    /// <param name="dotsPerInch">The resolution to record in the PNG.</param>
    /// <returns>PNG bytes with a correct <c>pHYs</c> chunk.</returns>
    public static byte[] RenderPng(QrSymbol symbol, int modulePixels, QrRenderOptions options, int dotsPerInch)
    {
        using var bitmap = Render(symbol, modulePixels, options);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);

        return PngDensityWriter.WithResolution(encoded.ToArray(), dotsPerInch);
    }

    private static void DrawModules(SKCanvas canvas, QrSymbol symbol, int modulePixels, SKColor foreground)
    {
        using var paint = new SKPaint
        {
            Color = foreground,
            Style = SKPaintStyle.Fill,
            IsAntialias = false,
        };

        for (var y = 0; y < symbol.TotalModulesPerSide; y++)
        {
            for (var x = 0; x < symbol.TotalModulesPerSide; x++)
            {
                if (!symbol.IsDark(x, y))
                {
                    continue;
                }

                canvas.DrawRect(
                    x * modulePixels,
                    y * modulePixels,
                    modulePixels,
                    modulePixels,
                    paint);
            }
        }
    }

    /// <summary>
    /// Composites the logo over an opaque pad rather than alpha-blending it.
    /// </summary>
    /// <remarks>
    /// Alpha-blending a logo over modules produces mid-tone pixels the decoder must resolve as
    /// light or dark, which is worse than a clean occlusion the error correction can simply
    /// repair (PRD FR-5.4).
    /// </remarks>
    private static void DrawLogo(SKCanvas canvas, QrSymbol symbol, int modulePixels, QrRenderOptions options)
    {
        var symbolPixels = symbol.ModulesPerSide * modulePixels;
        var logoSide = (float)(symbolPixels * options.LogoWidthFraction);
        var padding = modulePixels;
        var centre = symbol.TotalModulesPerSide * modulePixels / 2f;

        var padRect = SKRect.Create(
            centre - (logoSide / 2f) - padding,
            centre - (logoSide / 2f) - padding,
            logoSide + (2 * padding),
            logoSide + (2 * padding));

        using (var padPaint = new SKPaint { Color = options.Background, IsAntialias = false })
        {
            canvas.DrawRect(padRect, padPaint);
        }

        var logoRect = SKRect.Create(
            centre - (logoSide / 2f),
            centre - (logoSide / 2f),
            logoSide,
            logoSide);

        var sampling = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.Linear);
        canvas.DrawImage(options.Logo, logoRect, sampling);
    }
}
