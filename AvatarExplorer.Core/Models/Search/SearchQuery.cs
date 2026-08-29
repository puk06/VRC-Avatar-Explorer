namespace AvatarExplorer.Core.Models.Search;

/// <summary>
/// 検索クエリを表します。トークン一覧と、OR 検索・非表示を含むかどうかのフラグを保持します。
/// </summary>
public sealed class SearchQuery(IReadOnlyList<SearchQueryToken> tokens, bool isOr = false, bool includeHidden = false)
{
    /// <summary>
    /// 検索トークンの一覧。
    /// </summary>
    public IReadOnlyList<SearchQueryToken> Tokens { get; } = tokens;

    /// <summary>
    /// トークン同士の AND/OR を切り替えるかどうか（true で OR 検索）。
    /// </summary>
    public bool IsOr { get; } = isOr;

    /// <summary>
    /// 非表示アイテムを検索結果に含めるかどうか。
    /// </summary>
    public bool IncludeHidden { get; } = includeHidden;
}

/// <summary>
/// 検索クエリの1トークン（フィールド指定・値・否定の有無）を表します。
/// </summary>
public sealed class SearchQueryToken(string? field, string value, bool isNegation)
{
    /// <summary>
    /// 否定（除外）を表すプレフィックス文字。
    /// </summary>
    public const char NegationPrefix = '~';

    /// <summary>
    /// 検索対象のフィールド名。null の場合はフリーワード検索。
    /// </summary>
    public string? Field { get; } = field;

    /// <summary>
    /// 検索する値。
    /// </summary>
    public string Value { get; } = value;

    /// <summary>
    /// このトークンが否定（除外条件）かどうか。
    /// </summary>
    public bool IsNegation { get; } = isNegation;

    /// <summary>
    /// 生のトークン文字列を解析し、SearchQueryToken を生成します。
    /// </summary>
    /// <param name="rawToken">解析対象のトークン文字列（例: <c>author="ぷこるふ"</c>、<c>~赤</c>）。</param>
    /// <returns>解析された SearchQueryToken。</returns>
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
            if (
                value.Length >= 2 &&
                ((value[0] == '"' && value[^1] == '"') ||
                (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }
        }

        return new SearchQueryToken(field, value, isNegation);
    }
}
