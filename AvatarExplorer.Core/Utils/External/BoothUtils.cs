using System.Text.RegularExpressions;

namespace AvatarExplorer.Core.Utils;

internal static partial class BoothUtils
{
    [GeneratedRegex(@"https://(.*)\.booth\.pm/")]
    private static partial Regex BoothAuthorURLRegex();
    
    internal static string GetAuthorIdFromUrl(string url)
    {
        Match match = BoothAuthorURLRegex().Match(url);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
