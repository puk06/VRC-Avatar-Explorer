using System.Text.RegularExpressions;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// 自然順ソート（数値を含む文字列を人間にとって自然な順序で並べ替える）の拡張メソッドを提供します。
/// </summary>
public static class SortExtensions
{
    private static readonly NaturalStringComparer NaturalComparer = new();

    /// <summary>
    /// 指定したキーセレクタに基づき、要素を自然順（例: "file2" が "file10" より先）でソートします。
    /// </summary>
    /// <typeparam name="T">要素の型。</typeparam>
    /// <param name="source">ソート対象のシーケンス。</param>
    /// <param name="keySelector">各要素からソートキーとなる文字列を取得する関数。</param>
    /// <returns>自然順にソートされたシーケンス。</returns>
    public static IEnumerable<T> NaturalSort<T>(this IEnumerable<T> source, Func<T, string> keySelector)
    {
        return source.OrderBy(keySelector, NaturalComparer);
    }
}

/// <summary>
/// 文字列を自然順（数値部分を数値として比較）で比較する <see cref="IComparer{T}"/> の実装です。
/// </summary>
public sealed partial class NaturalStringComparer : IComparer<string>
{
    /// <summary>自然順比較の共有インスタンス。</summary>
    public static readonly NaturalStringComparer Instance = new();

    private static readonly Regex NumberRegex = NatualSortNumberRegex();

    private readonly record struct Segment
    {
        public bool IsText { get; init; }
        public bool IsNumber { get; init; }
        public string Text { get; init; }
        public long Number { get; init; }

        public static Segment CreateText(string text) => new() { IsText = true, Text = text };
        public static Segment CreateNumber(long number) => new() { IsNumber = true, Number = number, Text = number.ToString() };
    }

    /// <summary>
    /// 2 つの文字列を自然順で比較します。null は空文字より小さく扱われます。
    /// </summary>
    /// <param name="x">比較する最初の文字列。</param>
    /// <param name="y">比較する 2 番目の文字列。</param>
    /// <returns>x が y より小さい場合は負の値、等しい場合は 0、大きい場合は正の値。</returns>
    public int Compare(string? x, string? y)
    {
        if (ReferenceEquals(x, y)) return 0;
        if (x is null) return -1;
        if (y is null) return 1;

        var xSegments = SplitIntoSegments(x);
        var ySegments = SplitIntoSegments(y);

        return CompareSegments(xSegments, ySegments);
    }

    private static List<Segment> SplitIntoSegments(string value)
    {
        var segments = new List<Segment>();
        var lastEnd = 0;

        foreach (Match match in NumberRegex.Matches(value))
        {
            if (match.Index > lastEnd)
                segments.Add(Segment.CreateText(value[lastEnd..match.Index]));

            segments.Add(Segment.CreateNumber(long.Parse(match.Value)));
            lastEnd = match.Index + match.Length;
        }

        if (lastEnd < value.Length)
            segments.Add(Segment.CreateText(value[lastEnd..]));

        return segments;
    }

    private static int CompareSegments(List<Segment> x, List<Segment> y)
    {
        var max = Math.Max(x.Count, y.Count);
        for (var i = 0; i < max; i++)
        {
            if (i >= x.Count) return -1;
            if (i >= y.Count) return 1;

            var cmp = CompareSegment(x[i], y[i]);
            if (cmp != 0) return cmp;
        }
        return 0;
    }

    private static int CompareSegment(Segment x, Segment y)
    {
        if (x.IsText && y.IsText)
            return string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);

        if (x.IsNumber && y.IsNumber)
            return x.Number.CompareTo(y.Number);

        return string.Compare(x.Text, y.Text, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\d+", RegexOptions.Compiled)]
    private static partial Regex NatualSortNumberRegex();
}
