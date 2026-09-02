using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ContactQR.App.Converters;

/// <summary>
/// Resolves a token name from the view model into the brush that token names.
/// </summary>
/// <remarks>
/// The view model decides <em>which</em> verdict applies and names the token; the theme owns
/// what that token looks like. Binding a brush directly from the view model would put colour
/// decisions in logic and break the light theme before it is written.
/// </remarks>
public sealed class BrushKeyConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string key)
        {
            return DependencyProperty.UnsetValue;
        }

        return Application.Current?.TryFindResource(key) as Brush
            ?? (object)DependencyProperty.UnsetValue;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Brush tokens resolve in one direction only.");
}
