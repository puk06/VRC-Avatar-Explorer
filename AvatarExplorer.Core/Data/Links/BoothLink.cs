namespace AvatarExplorer.Core.Data.Links;

/// <summary>
/// Booth の各種 URL を生成するためのフォーマット定数を提供します。
/// </summary>
public static class BoothLink
{
    /// <summary>
    /// 作者 subdomain 付きの商品ページ URL のフォーマット（引数: subdomain, itemId）。
    /// </summary>
    public const string ItemURLFormat = "https://{0}.booth.pm/items/{1}";

    /// <summary>
    /// 作者 subdomain なしの商品ページ URL のフォーマット（引数: 言語コード, itemId）。
    /// </summary>
    public const string ItemURLWithoutAuthorFormat = "https://booth.pm/{0}/items/{1}";

    /// <summary>
    /// 商品の JSON 情報を取得する URL のフォーマット（引数: itemId）。カテゴリ判定を英語で行うため en ドメインを使用します。
    /// </summary>
    public const string ItemJsonURLFormat = "https://booth.pm/en/items/{0}.json"; // カテゴリなどの判定を英語で行うため、enドメインのURLを使用

    /// <summary>
    /// バリエーション付き商品の JSON 情報を取得する API の URL フォーマット（引数: itemId）。
    /// </summary>
    public const string ItemWithVariationJsonURLFormat = "https://api.booth.pm/vroid/items/{0}";

    /// <summary>
    /// 作者ページの URL のフォーマット（引数: subdomain）。
    /// </summary>
    public const string AuthorURLFormat = "https://{0}.booth.pm/";
}
