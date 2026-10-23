using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Models.System;

/// <summary>
/// 検索インデックスの1エントリ。フィールド名・検索対象の値・検索時の重み（ポイント）を保持します。
/// <see cref="Weight"/> が 0 のエントリはフリーワード検索の対象外となり、フィールド指定検索でのみ使用されます。
/// </summary>
public record SearchIndex
{
    /// <summary>
    /// フィールド名（小文字）。例: title, author, tag。
    /// </summary>
    public required string FieldName { get; init; }

    /// <summary>
    /// 検索対象の値の一覧。フリーワード検索用にローマ字・ひらがなの表現も含みます。
    /// </summary>
    public required string[] FieldValues { get; init; }

    /// <summary>
    /// 一致時に加算される検索ポイント。
    /// </summary>
    public required int Weight { get; init; }

    /// <summary>
    /// 検索クエリに対してインデックス一覧がマッチするかを判定し、検索ポイントを算出します。
    /// 各トークンのポイントは一致したエントリの重みの最大値とし、トークンごとの合計をスコアとします。
    /// </summary>
    /// <param name="indices">判定対象の検索インデックス一覧。</param>
    /// <param name="query">検索クエリ。</param>
    /// <param name="locKeyProvider">category 検索時に表示名をローカライズキーに変換する関数。</param>
    /// <param name="score">算出された検索ポイント。</param>
    /// <returns>クエリに一致する場合は true。</returns>
    public static bool TryGetScore(IReadOnlyList<SearchIndex> indices, SearchQuery query, Func<string, string>? locKeyProvider, out int score)
    {
        score = 0;
        var matchedCount = 0;

        foreach (var token in query.Tokens)
        {
            if (!TryMatchToken(indices, token, locKeyProvider, out var tokenScore)) continue;

            matchedCount++;
            score += tokenScore;
        }

        return query.IsOr ? matchedCount > 0 : matchedCount == query.Tokens.Count;
    }

    private static bool TryMatchToken(IReadOnlyList<SearchIndex> indices, SearchQueryToken token, Func<string, string>? locKeyProvider, out int score)
    {
        score = 0;

        var field = token.Field?.ToLowerInvariant() switch
        {
            "booth" => "boothid",
            var f => f
        };

        var comparisonValue = field switch
        {
            "category" when locKeyProvider != null => locKeyProvider(token.Value) ?? token.Value,
            _ => token.Value
        };
        if (field is null) comparisonValue = comparisonValue.ToHiragana();

        var targets = indices.Where(i => field is null ? i.Weight > 0 : i.FieldName == field).ToList();
        if (targets.Count == 0) return false;

        const StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
        var matchedWeights = targets
            .Where(t => t.FieldValues.Any(v => v.Contains(comparisonValue, comparison)))
            .Select(t => t.Weight)
            .ToList();

        if (token.IsNegation) return matchedWeights.Count == 0;
        if (matchedWeights.Count == 0) return false;

        score = matchedWeights.Max();
        return true;
    }

    /// <summary>
    /// アイテムと関連名一覧から、検索インデックスの一覧を構築します。
    /// </summary>
    /// <param name="item">対象のアイテム。</param>
    /// <param name="supportedAvatarNames">対応アバターの表示名一覧。</param>
    /// <param name="implementedAvatarNames">実装済みアバターの表示名一覧。</param>
    /// <param name="notImplementedAvatarNames">未実装アバターの表示名一覧。</param>
    /// <param name="commonAvatarNames">共通素体グループ名一覧。</param>
    /// <returns>構築された検索インデックスの一覧。</returns>
    public static IReadOnlyList<SearchIndex> BuildItem(Item item, string[] supportedAvatarNames, string[] implementedAvatarNames, string[] notImplementedAvatarNames, string[] commonAvatarNames)
    {
        return
        [
            Create("identifier", [item.Identifier], 0, expandKana: false),
            Create("title", [item.Title], 10),
            Create("author", [item.Author], 5),
            Create("boothid", [item.BoothId >= 0 ? item.BoothId.ToString() : string.Empty], 1),
            Create("category", [item.Category.ToString()], 0, expandKana: false),
            Create("memo", [item.ItemMemo], 3),
            Create("tag", item.Tags, 4),
            Create("supportedavatar", supportedAvatarNames, 1),
            Create("implementedavatar", implementedAvatarNames, 1),
            Create("notimplementedavatar", notImplementedAvatarNames, 0),
            Create("commonavatar", commonAvatarNames, 1),
        ];
    }

    /// <summary>
    /// 共通素体グループと所属アイテムの検索インデックスから、検索インデックスの一覧を構築します。
    /// </summary>
    /// <param name="commonAvatar">対象の共通素体グループ。</param>
    /// <param name="itemIndices">グループに含まれるアイテムの検索インデックス一覧。</param>
    /// <returns>構築された検索インデックスの一覧。</returns>
    public static IReadOnlyList<SearchIndex> BuildCommonAvatar(CommonAvatar commonAvatar, IEnumerable<IReadOnlyList<SearchIndex>?> itemIndices)
    {
        var indices = new List<SearchIndex>
        {
            Create("title", [commonAvatar.GroupName], 10)
        };

        indices.AddRange(itemIndices
            .Where(i => i != null)
            .SelectMany(i => i!)
            .Where(i => i.Weight > 0));

        return indices;
    }

    /// <summary>
    /// 仮アバターから検索インデックスの一覧を構築します。
    /// </summary>
    /// <param name="tempAvatar">対象の仮アバター。</param>
    /// <returns>構築された検索インデックスの一覧。</returns>
    public static IReadOnlyList<SearchIndex> BuildTempAvatar(TempAvatar tempAvatar)
    {
        return
        [
            Create("title", [tempAvatar.AvatarName], 10),
            Create("boothid", [tempAvatar.BoothId >= 0 ? tempAvatar.BoothId.ToString() : string.Empty], 1),
        ];
    }

    private static SearchIndex Create(string fieldName, IEnumerable<string> values, int weight, bool expandKana = true)
    {
        var fieldValues = values
            .Where(v => !string.IsNullOrEmpty(v))
            .SelectMany(v => expandKana
                ? new[] { v, v.ToCanonicalRomaji(), v.ToHiragana() }
                : new[] { v })
            .Distinct()
            .ToArray();

        return new SearchIndex
        {
            FieldName = fieldName,
            FieldValues = fieldValues,
            Weight = weight
        };
    }
}
