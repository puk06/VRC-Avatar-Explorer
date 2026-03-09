using System.Text.RegularExpressions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

public static partial class SearchFilterBuilder
{
    [GeneratedRegex(@"(?<key>Title|Author|Booth|Avatar|Category|Memo|Folder|File|Implemented|NotImplemented|Tag|Common|OR|BrokenItems)=(?:""(?<value>.*?)""|(?<value>[^\s]+))|(?<word>[^\s]+)")]
    private static partial Regex SearchFilterTextRegex();

    private sealed class RawSearchToken
    {
        internal string Key { get; set; } = string.Empty;
        internal string Value { get; set; } = string.Empty;
    }

    private static List<RawSearchToken> Parse(string text)
    {
        MatchCollection matches = SearchFilterTextRegex().Matches(text);
        List<RawSearchToken> rawSearchTokens = new();

        foreach (GroupCollection groupCollection in matches.Select(m => m.Groups))
        {
            if (groupCollection["key"].Success && groupCollection["value"].Success)
            {
                rawSearchTokens.Add(new RawSearchToken
                {
                    Key = groupCollection["key"].Value,
                    Value = groupCollection["value"].Value
                });
            }
            else if (groupCollection["word"].Success)
            {
                rawSearchTokens.Add(new RawSearchToken
                {
                    Key = "FreeWord",
                    Value = groupCollection["word"].Value
                });
            }
        }

        return rawSearchTokens;
    }

    public static SearchFilter Build(string searchText, Func<string, string>? toLocalizationKey = null)
    {
        List<RawSearchToken> rawSearchTokens = Parse(searchText);
        SearchFilter filter = new();

        foreach (RawSearchToken token in rawSearchTokens)
        {
            switch (token.Key)
            {
                case "Title":
                    filter.Titles.Add(token.Value);
                    break;
                case "Author":
                    filter.Authors.Add(token.Value);
                    break;
                case "Booth":
                    filter.BoothIds.Add(token.Value);
                    break;
                case "Avatar":
                    filter.SupportedAvatars.Add(token.Value);
                    break;
                case "Category":
                    filter.Categories.Add(toLocalizationKey == null ? token.Value : toLocalizationKey(token.Value));
                    break;
                case "Memo":
                    filter.ItemMemos.Add(token.Value);
                    break;
                case "Folder":
                    filter.FolderNames.Add(token.Value);
                    break;
                case "File":
                    filter.FileNames.Add(token.Value);
                    break;
                case "Implemented":
                    filter.ImplementedAvatars.Add(token.Value);
                    break;
                case "NotImplemented":
                    filter.NotImplementedAvatars.Add(token.Value);
                    break;
                case "Tag":
                    filter.Tags.Add(token.Value);
                    break;
                case "Common":
                    filter.CommonAvatars.Add(token.Value);
                    break;
                case "OR":
                    filter.IsOrSearch = token.Value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                    break;
                case "BrokenItems":
                    filter.BrokenItems = token.Value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                    break;
                case "FreeWord":
                    filter.SearchWords.Add(token.Value);
                    break;
            }
        }

        return filter;
    }
}
