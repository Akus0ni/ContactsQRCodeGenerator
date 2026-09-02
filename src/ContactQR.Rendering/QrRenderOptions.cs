using SkiaSharp;

namespace ContactQR.Rendering;

/// <summary>
/// How a <see cref="QrSymbol"/> should be drawn: colours, optional centre logo, and size.
/// </summary>
public sealed record QrRenderOptions
{
    /// <summary>The default logo width as a fraction of the symbol width (PRD FR-5.3).</summary>
    public const double DefaultLogoWidthFraction = 0.18;

    /// <summary>
    /// The largest logo width permitted, as a fraction of the symbol width.
    /// </summary>
    /// <remarks>
    /// Deliberately more conservative than the "up to 30%" quoted by online generators. Level
    /// H tolerates roughly 30% codeword damage, but that is a <em>total</em> budget shared with
    /// print defects, ink spread, lighting, camera angle and card wear. Spending most of it on
    /// a logo leaves nothing for the real world (PRD FR-5.3).
    /// </remarks>
    public const double MaximumLogoWidthFraction = 0.25;

    /// <summary>The colour of the dark modules.</summary>
    public SKColor Foreground { get; init; } = SKColors.Black;

    /// <summary>The colour behind the modules. Never transparent (PRD FR-5.9).</summary>
    public SKColor Background { get; init; } = SKColors.White;

    /// <summary>An optional centre logo. When present, the caller must already have forced ECC H.</summary>
    public SKImage? Logo { get; init; }

    /// <summary>The logo's width as a fraction of the symbol width.</summary>
    public double LogoWidthFraction { get; init; } = DefaultLogoWidthFraction;

    /// <summary>Whether a logo has been supplied.</summary>
    public bool HasLogo => Logo is not null;
}
