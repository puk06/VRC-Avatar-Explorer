using System.Globalization;

namespace AvatarExplorer.Core.Utils;

public static class DatetimeUtils
{
    public static string GetDateStringFromUnixTime(string unixTime)
    {
        if (string.IsNullOrEmpty(unixTime)) return "Invalid Date";

        if (long.TryParse(unixTime, out long unixTimeLong))
        {
            DateTime dateTime = DateTimeOffset.FromUnixTimeMilliseconds(unixTimeLong).ToLocalTime().DateTime;
            return dateTime.ToString("yyyy/MM/dd HH:mm:ss", CultureInfo.InvariantCulture);
        }

        return "Invalid Date";
    }

    public static string GetCurrentUnixTime() => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
}
