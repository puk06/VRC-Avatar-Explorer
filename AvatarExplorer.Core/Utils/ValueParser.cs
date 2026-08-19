using System.Globalization;

namespace AvatarExplorer.Core.Utils;

public static class ValueParser
{
    public static int Int(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return int.TryParse(value, out int v) ? v : defaultValue;
    }

    public static long Long(string? value, long defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return long.TryParse(value, out long v) ? v : defaultValue;
    }

    public static double Double(string? value, double defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return double.TryParse(value, out double v) ? v : defaultValue;
    }

    public static DateTime DateTime(string? value)
    {
        if (string.IsNullOrEmpty(value)) return System.DateTime.MinValue;
        return System.DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ? d : System.DateTime.MinValue;
    }

    public static bool Boolean(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return value == "1";
    }
}
