namespace AvatarExplorer.Core.Models.Search;

public sealed class SearchQuery(IReadOnlyList<SearchQueryToken> tokens, bool isOr = false)
{
    public IReadOnlyList<SearchQueryToken> Tokens { get; } = tokens;
    public bool IsOr { get; } = isOr;
}

public sealed class SearchQueryToken(string? field, string value, bool isNegation)
{
    public const char NegationPrefix = '~';

    /// <summary>
    /// 検索対象のフィールド名。null の場合はフリーワード検索。
    /// </summary>
    public string? Field { get; } = field;
    public string Value { get; } = value;
    public bool IsNegation { get; } = isNegation;

    public static SearchQueryToken Parse(string rawToken)
    {
        var value = rawToken;
        var isNegation = value.StartsWith(NegationPrefix);
        if (isNegation) value = value[1..];

        string? field = null;
        var separatorIndex = value.IndexOf('=');
        if (separatorIndex > 0)
        {
            field = value[..separatorIndex];
            value = value[(separatorIndex + 1)..];

            // 前後の引用符を取り除く
            if (value.Length >= 2)
            {
                if ((value[0] == '"' && value[^1] == '"') ||
                    (value[0] == '\'' && value[^1] == '\''))
                {
                    value = value[1..^1];
                }
            }
        }

        return new SearchQueryToken(field, value, isNegation);
    }
}
