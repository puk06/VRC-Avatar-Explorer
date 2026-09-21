using System.Globalization;
using Avalonia.Data.Converters;

namespace AvatarExplorer.UI.Converters;

public class StartsWithConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string text && parameter is string prefix)
            return text.StartsWith(prefix, StringComparison.Ordinal);
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
