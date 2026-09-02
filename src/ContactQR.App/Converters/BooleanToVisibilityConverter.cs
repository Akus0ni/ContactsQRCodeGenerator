using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ContactQR.App.Converters;

/// <summary>
/// Maps a boolean to <see cref="Visibility"/>, optionally inverted.
/// </summary>
/// <remarks>
/// WPF ships a converter for this, but not an invertible one, and two views sharing a grid
/// cell need both directions from the same flag.
/// </remarks>
public sealed class BooleanToVisibilityConverter : IValueConverter
{
    /// <summary>When set, true collapses and false shows.</summary>
    public bool Invert { get; set; }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var flag = value is true;

        if (Invert)
        {
            flag = !flag;
        }

        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException("Visibility resolves in one direction only.");
}
