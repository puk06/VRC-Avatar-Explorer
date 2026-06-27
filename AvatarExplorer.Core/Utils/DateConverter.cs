namespace AvatarExplorer.Core.Utils;

public static class DateConverter
{
    public static DateTime Convert(string dateString)
    {
        try
        {
            return DateTime.ParseExact(dateString, "yyyy-MM-dd", null);
        }
        catch (FormatException)
        {
            return default;
        }
    }

    public static bool IsBefore(string dateString, string compareDate)
    {
        var date1 = Convert(dateString);
        if (date1 == default)
        {
            throw new ArgumentException($"Invalid date string: {dateString}");
        }

        var date2 = Convert(compareDate);
        return date1 < date2;
    }

    public static bool IsAfter(string dateString, string compareDate)
    {
        var date1 = Convert(dateString);
        if (date1 == default)
        {
            throw new ArgumentException($"Invalid date string: {dateString}");
        }

        var date2 = Convert(compareDate);
        return date1 > date2;
    }

    public static bool IsSameDay(string dateString, string compareDate)
    {
        var date1 = Convert(dateString);
        if (date1 == default)
        {
            throw new ArgumentException($"Invalid date string: {dateString}");
        }

        var date2 = Convert(compareDate);
        return date1.Date == date2.Date;
    }

    public static bool IsBetween(string dateString, string fromCompareDate, string toCompareDate)
    {
        if (!IsSameDay(dateString, fromCompareDate) || !IsSameDay(dateString, toCompareDate))
        {
            throw new ArgumentException("Dates must be on the same day");
        }

        return IsAfter(dateString, fromCompareDate) && IsBefore(dateString, toCompareDate);
    }

}
