using System.Text.RegularExpressions;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Services.Items;

public static partial class SearchFilterBuilder
{
    [GeneratedRegex(@"(?<key>Title|Author|Booth|Avatar|Category|Memo|Folder|File|Implemented|NotImplemented|Tag|Common|OR|CategoryOR|EmptyAvatarAsNone)=(?:""(?<value>.*?)""|(?<value>[^\s]+))|(?<word>[^\s]+)")]
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
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.Title, token.Value));
                    break;
                case "Author":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.Author, token.Value));
                    break;
                case "Booth":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.BoothId, token.Value));
                    break;
                case "Avatar":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.SupportedAvatar, token.Value));
                    break;
                case "Category":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.Category, toLocalizationKey == null ? token.Value : toLocalizationKey(token.Value)));
                    break;
                case "Memo":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.ItemMemo, token.Value));
                    break;
                case "Folder":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.FolderName, token.Value));
                    break;
                case "File":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.FileName, token.Value));
                    break;
                case "Implemented":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.ImplementedAvatar, token.Value));
                    break;
                case "NotImplemented":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.NotImplementedAvatar, token.Value));
                    break;
                case "Tag":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.Tag, token.Value));
                    break;
                case "Common":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.CommonAvatar, token.Value));
                    break;
                case "OR":
                    filter.IsOrCondition = token.Value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                    break;
                case "CategoryOR":
                    filter.IsCategoryOrCondition = token.Value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                    if (filter.IsCategoryOrCondition) filter.IsOrCondition = false; // カテゴリーOR検索はOR検索と排他にする
                    break;
                case "EmptyAvatarAsNone":
                    filter.TreatEmptySupportedAvatarAsNone = token.Value.Equals("true", StringComparison.CurrentCultureIgnoreCase);
                    break;
                case "FreeWord":
                    filter.SearchTokens.Add(new SearchToken(SearchTokenType.FreeWord, token.Value));
                    break;
            }
        }

        return filter;
    }
}
