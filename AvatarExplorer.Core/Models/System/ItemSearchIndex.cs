using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Models.System;

/// <summary>
/// アイテムの検索インデックス。各フィールドごとの検索やフリーワード検索を高速に行うためのデータを保持します。
/// </summary>
public record ItemSearchIndex : ISearchIndex
{
    /// <summary>
    /// アイテム名。
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// 作者名。
    /// </summary>
    public required string Author { get; init; }

    /// <summary>
    /// Booth 商品 ID（文字列）。
    /// </summary>
    public required string BoothId { get; init; }

    /// <summary>
    /// 対応アバター名の一覧。
    /// </summary>
    public required string[] SupportedAvatars { get; init; }

    /// <summary>
    /// カテゴリ（カテゴリ検索用の文字列表現）。
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// メモ。
    /// </summary>
    public required string Memo { get; init; }

    /// <summary>
    /// 実装済みアバター名の一覧。
    /// </summary>
    public required string[] ImplementedAvatars { get; init; }

    /// <summary>
    /// 未実装アバター名の一覧。
    /// </summary>
    public required string[] NotImplementedAvatars { get; init; }

    /// <summary>
    /// タグの一覧。
    /// </summary>
    public required string[] Tags { get; init; }

    /// <summary>
    /// 共通素体グループ名の一覧。
    /// </summary>
    public required string[] CommonAvatars { get; init; }

    /// <summary>
    /// 各フィールドを結合したフリーワード検索用文字列（小文字）。
    /// </summary>
    public required string FreeWord { get; init; }

    /// <summary>
    /// 指定されたトークンがこのインデックスに一致するかどうかを判定します。category フィールドは locKeyProvider を通じて変換されます。
    /// </summary>
    /// <param name="token">判定対象の検索トークン。</param>
    /// <param name="locKeyProvider">category 検索時に表示名をローカライズキーに変換する関数。</param>
    /// <returns>一致する場合は true。</returns>
    public bool IsMatch(SearchQueryToken token, Func<string, string>? locKeyProvider = null)
    {
        var comparisonValue = token.Field?.ToLowerInvariant() switch
        {
            "category" when locKeyProvider != null => locKeyProvider(token.Value) ?? token.Value,
            _ => token.Value
        };

        var targets = GetTargets(token.Field);
        if (targets.Length == 0) return false;

        const StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
        return token.IsNegation
            ? targets.All(t => !t.Contains(comparisonValue, comparison))
            : targets.Any(t => t.Contains(comparisonValue, comparison));
    }

    public int CountMatches(IReadOnlyList<SearchQueryToken> tokens, Func<string, string>? locKeyProvider = null)
    {
        return tokens.Count(i => IsMatch(i, locKeyProvider));
    }

    private string[] GetTargets(string? field)
    {
        return field?.ToLowerInvariant() switch
        {
            "title" => [Title],
            "author" => [Author],
            "boothid" or "booth" => [BoothId],
            "supportedavatar" => SupportedAvatars,
            "category" => [Category],
            "memo" => [Memo],
            "implementedavatar" => ImplementedAvatars,
            "notimplementedavatar" => NotImplementedAvatars,
            "tag" => Tags,
            "commonavatar" => CommonAvatars,
            null => [FreeWord],
            _ => []
        };
    }

    /// <summary>
    /// アイテムと関連名一覧から、この検索インデックスを構築します。
    /// </summary>
    /// <param name="item">対象のアイテム。</param>
    /// <param name="supportedAvatarNames">対応アバターの表示名一覧。</param>
    /// <param name="implementedAvatarNames">実装済みアバターの表示名一覧。</param>
    /// <param name="notImplementedAvatarNames">未実装アバターの表示名一覧。</param>
    /// <param name="commonAvatarNames">共通素体グループ名一覧。</param>
    /// <returns>構築された ItemSearchIndex。</returns>
    public static ItemSearchIndex Build(Item item, string[] supportedAvatarNames, string[] implementedAvatarNames, string[] notImplementedAvatarNames, string[] commonAvatarNames)
    {
        var category = item.Category.ToString();

        var freeWord = string.Join("\n",
            item.Title,
            item.Author,
            item.ItemMemo,
            item.BoothId.ToString(),
            string.Join("\n", item.Tags),
            string.Join("\n", supportedAvatarNames),
            string.Join("\n", implementedAvatarNames),
            string.Join("\n", commonAvatarNames)
        ).ToLowerInvariant();

        return new ItemSearchIndex
        {
            Title = item.Title,
            Author = item.Author,
            BoothId = item.BoothId.ToString(),
            SupportedAvatars = supportedAvatarNames,
            Category = category,
            Memo = item.ItemMemo,
            ImplementedAvatars = implementedAvatarNames,
            NotImplementedAvatars = notImplementedAvatarNames,
            Tags = item.Tags.ToArray(),
            CommonAvatars = commonAvatarNames,
            FreeWord = freeWord
        };
    }
}
