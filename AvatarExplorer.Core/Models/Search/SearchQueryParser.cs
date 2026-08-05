using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.Search;

public static class SearchQueryParser
{
    public static SearchQuery Parse(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return new SearchQuery([]);

        var rawTokens = TextParser.Parse(searchText);
        var tokens = new List<SearchQueryToken>(rawTokens.Length);
        var isOr = false;

        foreach (var rawToken in rawTokens)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) continue;

            if (rawToken.Equals("OR=true", StringComparison.OrdinalIgnoreCase))
            {
                isOr = true;
                continue;
            }

            tokens.Add(SearchQueryToken.Parse(rawToken));
        }

        return new SearchQuery(tokens, isOr);
    }
}
