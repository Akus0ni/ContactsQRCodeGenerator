namespace ContactQR.Rendering;

/// <summary>
/// How many pixels a module gets, and what resolution makes the result print at the requested
/// physical width.
/// </summary>
/// <param name="ModulePixels">The size of one module in the output bitmap. Always a whole number.</param>
/// <param name="SidePixels">The bitmap side length.</param>
/// <param name="DotsPerInch">The resolution to record so the bitmap prints at the requested width.</param>
public readonly record struct PhysicalSize(int ModulePixels, int SidePixels, int DotsPerInch)
{
    /// <summary>The width this will actually print at.</summary>
    public decimal WidthMillimetres => SidePixels * 25.4m / DotsPerInch;
}

/// <summary>
/// Reconciles two requirements that pull against each other: modules must be a whole number of
/// pixels, and the code must print at the width the operator asked for.
/// </summary>
/// <remarks>
/// <para>
/// Whole-pixel modules are non-negotiable — fractional ones produce grey edges that
/// misrepresent print quality (PRD FR-3.7). But rounding module size to a whole number changes
/// the physical width by up to one module per side, which at card sizes is several percent.
/// Flooring a 20 mm request with 61 total modules at 300 dpi yields 3 px modules and a 15.5 mm
/// code — 23% narrower than asked for, which silently breaks a card layout.
/// </para>
/// <para>
/// The resolution recorded in the PNG is what actually decides printed size, and nothing
/// requires it to be exactly 300. So the module size rounds <em>up</em>, and the recorded
/// resolution is then derived to make the physical width exact. The operator gets the width
/// they asked for, modules stay whole pixels, nothing is resampled, and the effective
/// resolution is never below the one requested.
/// </para>
/// </remarks>
public static class PhysicalSizing
{
    private const decimal MillimetresPerInch = 25.4m;

    /// <summary>
    /// Chooses a module size and resolution for a symbol at a requested physical width.
    /// </summary>
    /// <param name="totalModulesPerSide">Modules per side including the quiet zone.</param>
    /// <param name="widthMillimetres">The width the code must print at.</param>
    /// <param name="minimumDotsPerInch">The lowest acceptable output resolution.</param>
    /// <returns>The module size, bitmap size and the resolution to record.</returns>
    public static PhysicalSize Fit(int totalModulesPerSide, decimal widthMillimetres, int minimumDotsPerInch)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(totalModulesPerSide);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(widthMillimetres);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(minimumDotsPerInch);

        var targetPixels = widthMillimetres / MillimetresPerInch * minimumDotsPerInch;

        // Round up, never down: the resulting resolution is then at or above the requested one,
        // so print quality is never quietly reduced to hit a width.
        var modulePixels = Math.Max(1, (int)Math.Ceiling(targetPixels / totalModulesPerSide));
        var sidePixels = modulePixels * totalModulesPerSide;
        var dotsPerInch = (int)Math.Round(sidePixels * MillimetresPerInch / widthMillimetres);

        return new PhysicalSize(modulePixels, sidePixels, dotsPerInch);
    }
}
