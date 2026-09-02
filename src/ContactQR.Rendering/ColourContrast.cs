using SkiaSharp;

namespace ContactQR.Rendering;

/// <summary>Why a colour pair was rejected, or that it was accepted.</summary>
public enum ColourPairVerdict
{
    /// <summary>Comfortably above the contrast threshold.</summary>
    Acceptable,

    /// <summary>Above the block threshold but below the clean-pass target.</summary>
    Marginal,

    /// <summary>Below the contrast threshold. Export is blocked (PRD FR-5.7).</summary>
    InsufficientContrast,

    /// <summary>
    /// Light modules on a dark background. Blocked outright rather than warned (PRD FR-5.8).
    /// </summary>
    Inverted,
}

/// <summary>The measured result of checking a foreground and background pair.</summary>
/// <param name="Verdict">Whether the pair may be used.</param>
/// <param name="Ratio">The measured luminance contrast ratio, between 1 and 21.</param>
public readonly record struct ColourPairAssessment(ColourPairVerdict Verdict, double Ratio)
{
    /// <summary>Whether this pair must block export.</summary>
    public bool BlocksExport =>
        Verdict is ColourPairVerdict.InsufficientContrast or ColourPairVerdict.Inverted;
}

/// <summary>
/// Measures whether a client's brand colours will still scan.
/// </summary>
/// <remarks>
/// The app's job is to measure the client's colours and block what will not scan, not to
/// restyle them. A numeric ratio is also the accessible answer for an operator who cannot
/// judge the pair by eye.
/// </remarks>
public static class ColourContrast
{
    /// <summary>Below this ratio, export is blocked.</summary>
    public const double BlockThreshold = 7.0;

    /// <summary>At or above this ratio, the pair passes cleanly.</summary>
    public const double CleanPassThreshold = 10.0;

    /// <summary>
    /// Assesses a foreground and background pair.
    /// </summary>
    /// <param name="foreground">The colour of the dark modules.</param>
    /// <param name="background">The colour behind them.</param>
    /// <returns>The verdict and the measured ratio.</returns>
    /// <remarks>
    /// Inversion is checked before contrast, because a light-on-dark pair can have excellent
    /// contrast and still fail: a meaningful share of decoders, including some stock camera
    /// implementations, assume dark-on-light and never attempt inversion. There is no partial
    /// credit — the code either scans on the recipient's phone or it does not.
    /// </remarks>
    public static ColourPairAssessment Assess(SKColor foreground, SKColor background)
    {
        var foregroundLuminance = RelativeLuminance(foreground);
        var backgroundLuminance = RelativeLuminance(background);
        var ratio = ContrastRatio(foregroundLuminance, backgroundLuminance);

        if (foregroundLuminance > backgroundLuminance)
        {
            return new ColourPairAssessment(ColourPairVerdict.Inverted, ratio);
        }

        var verdict = ratio switch
        {
            >= CleanPassThreshold => ColourPairVerdict.Acceptable,
            >= BlockThreshold => ColourPairVerdict.Marginal,
            _ => ColourPairVerdict.InsufficientContrast,
        };

        return new ColourPairAssessment(verdict, ratio);
    }

    /// <summary>The WCAG contrast ratio between two relative luminances.</summary>
    /// <param name="first">One relative luminance.</param>
    /// <param name="second">The other relative luminance.</param>
    /// <returns>A ratio between 1 and 21.</returns>
    public static double ContrastRatio(double first, double second)
    {
        var lighter = Math.Max(first, second);
        var darker = Math.Min(first, second);

        return (lighter + 0.05) / (darker + 0.05);
    }

    /// <summary>The relative luminance of a colour, per the sRGB definition.</summary>
    /// <param name="colour">The colour to measure.</param>
    /// <returns>Relative luminance between 0 and 1.</returns>
    public static double RelativeLuminance(SKColor colour) =>
        (0.2126 * Linearise(colour.Red))
        + (0.7152 * Linearise(colour.Green))
        + (0.0722 * Linearise(colour.Blue));

    private static double Linearise(byte channel)
    {
        var value = channel / 255.0;

        return value <= 0.03928
            ? value / 12.92
            : Math.Pow((value + 0.055) / 1.055, 2.4);
    }
}
