using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.Search;

/// <summary>
/// 検索文字列を <see cref="SearchQuery"/> に解析するユーティリティ。
/// </summary>
public static class SearchQueryParser
{
    /// <summary>
    /// 検索文字列を解析して <see cref="SearchQuery"/> を生成します。OR=true / IncludeHidden=true 等特殊トークンにも対応します。
    /// </summary>
    /// <param name="searchText">検索文字列。</param>
    /// <returns>解析された SearchQuery。空文字の場合は空のクエリを返します。</returns>
    public static SearchQuery Parse(string searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
            return new SearchQuery([]);

        var rawTokens = TextParser.Parse(searchText);
        var tokens = new List<SearchQueryToken>(rawTokens.Length);
        var isOr = false;
        var includeHidden = false;

        foreach (var rawToken in rawTokens)
        {
            if (string.IsNullOrWhiteSpace(rawToken)) continue;

            if (rawToken.Equals("OR=true", StringComparison.OrdinalIgnoreCase))
            {
                isOr = true;
                continue;
            }

            if (rawToken.Equals("IncludeHidden=true", StringComparison.OrdinalIgnoreCase))
            {
                includeHidden = true;
                continue;
            }

            tokens.Add(SearchQueryToken.Parse(rawToken));
        }

        return new SearchQuery(tokens, isOr, includeHidden);
    }
}
