using System.Text.RegularExpressions;

namespace AvatarExplorer.UI.Utils;

internal static partial class TextBracketsUtils
{
    [GeneratedRegex(@"\u3010[^\u3011]+\u3011")]
    private static partial Regex TextBracketsRegex();

    public static string RemoveBrackets(string value) => TextBracketsRegex().Replace(value, string.Empty);
}
