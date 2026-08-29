using System.Globalization;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// Unix 時間（ミリ秒）や日付文字列を扱うためのユーティリティを提供します。
/// </summary>
public static class DatetimeUtils
{
    /// <summary>
    /// Unix 時間（ミリ秒）を表す文字列を、ローカル日時の文字列（yyyy/MM/dd HH:mm:ss）に変換します。
    /// </summary>
    /// <param name="unixTime">Unix 時間（ミリ秒）を表す文字列。</param>
    /// <returns>変換後の日時文字列。変換に失敗した場合は "Invalid Date"。</returns>
    public static string GetDateStringFromUnixTime(string unixTime)
    {
        if (string.IsNullOrEmpty(unixTime)) return "Invalid Date";

        if (long.TryParse(unixTime, out long unixTimeLong))
        {
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeLong).ToLocalTime().DateTime;
            return dateTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return "Invalid Date";
    }

    /// <summary>
    /// 現在の UTC 時刻を Unix 時間（ミリ秒）の文字列として取得します。
    /// </summary>
    /// <returns>現在の Unix 時間（ミリ秒）を表す文字列。</returns>
    public static string GetCurrentUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
}
