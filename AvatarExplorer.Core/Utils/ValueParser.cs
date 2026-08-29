using System.Globalization;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// 文字列を各種の値型に安全に変換するユーティリティを提供します。変換に失敗した場合は既定値を返します。
/// </summary>
public static class ValueParser
{
    /// <summary>文字列を int に変換します。空/null または変換失敗時は既定値を返します。</summary>
    /// <param name="value">変換対象の文字列。</param>
    /// <param name="defaultValue">変換失敗時に返す既定値。</param>
    /// <returns>変換後の整数値。</returns>
    public static int Int(string? value, int defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return int.TryParse(value, out int v) ? v : defaultValue;
    }

    /// <summary>文字列を long に変換します。空/null または変換失敗時は既定値を返します。</summary>
    /// <param name="value">変換対象の文字列。</param>
    /// <param name="defaultValue">変換失敗時に返す既定値。</param>
    /// <returns>変換後の長整数値。</returns>
    public static long Long(string? value, long defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return long.TryParse(value, out long v) ? v : defaultValue;
    }

    /// <summary>文字列を double に変換します。空/null または変換失敗時は既定値を返します。</summary>
    /// <param name="value">変換対象の文字列。</param>
    /// <param name="defaultValue">変換失敗時に返す既定値。</param>
    /// <returns>変換後の浮動小数点値。</returns>
    public static double Double(string? value, double defaultValue = 0)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return double.TryParse(value, out double v) ? v : defaultValue;
    }

    /// <summary>文字列を DateTime に変換します。空/null または変換失敗時は DateTime.MinValue を返します。</summary>
    /// <param name="value">変換対象の文字列。</param>
    /// <returns>変換後の日時。失敗時は DateTime.MinValue。</returns>
    public static DateTime DateTime(string? value)
    {
        if (string.IsNullOrEmpty(value)) return System.DateTime.MinValue;
        return System.DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ? d : System.DateTime.MinValue;
    }

    /// <summary>文字列を bool に変換します。値が "1" の場合のみ true とみなします。空/null の場合は既定値を返します。</summary>
    /// <param name="value">変換対象の文字列。</param>
    /// <param name="defaultValue">変換失敗時に返す既定値。</param>
    /// <returns>変換後の真偽値。</returns>
    public static bool Boolean(string? value, bool defaultValue = false)
    {
        if (string.IsNullOrEmpty(value)) return defaultValue;
        return value == "1";
    }
}
