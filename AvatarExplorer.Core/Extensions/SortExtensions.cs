using System.Text.RegularExpressions;

namespace AvatarExplorer.Core.Extensions;

public static class SortExtensions
{
    private static readonly NaturalStringComparer NaturalComparer = new();

    public static IEnumerable<T> NaturalSort<T>(this IEnumerable<T> source, Func<T, string> keySelector)
    {
        return source.OrderBy(keySelector, NaturalComparer);
    }
}

public sealed partial class NaturalStringComparer : IComparer<string>
{
    public static readonly NaturalStringComparer Instance = new();

    private static readonly Regex NumberRegex = NatualSortNumberRegex();

    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xMatches = NumberRegex.Matches(x);
        var yMatches = NumberRegex.Matches(y);

        var max = Math.Max(xMatches.Count, yMatches.Count);
        var lastXEnd = 0;
        var lastYEnd = 0;

        for (var i = 0; i < max; i++)
        {
            var xMatch = i < xMatches.Count ? xMatches[i] : null;
            var yMatch = i < yMatches.Count ? yMatches[i] : null;

            var xStart = xMatch?.Index ?? x.Length;
            var yStart = yMatch?.Index ?? y.Length;

            var xText = x[lastXEnd..xStart];
            var yText = y[lastYEnd..yStart];
            var textCmp = string.Compare(xText, yText, StringComparison.OrdinalIgnoreCase);
            if (textCmp != 0) return textCmp;

            if (xMatch is null) return -1;
            if (yMatch is null) return 1;

            var xNum = long.Parse(xMatch.Value);
            var yNum = long.Parse(yMatch.Value);
            if (xNum != yNum) return xNum.CompareTo(yNum);

            lastXEnd = xMatch.Index + xMatch.Length;
            lastYEnd = yMatch.Index + yMatch.Length;
        }

        var xTail = x[lastXEnd..];
        var yTail = y[lastYEnd..];
        return string.Compare(xTail, yTail, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
    private static partial Regex NatualSortNumberRegex();
}
