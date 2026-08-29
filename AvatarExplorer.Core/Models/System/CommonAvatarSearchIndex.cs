using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Models.System;

/// <summary>
/// 共通素体グループの検索インデックス。グループ名および所属アイテムのフリーワードで検索できます。
/// </summary>
public record CommonAvatarSearchIndex : ISearchIndex
{
    /// <summary>
    /// 共通素体グループの名前。
    /// </summary>
    public required string GroupName { get; init; }

    /// <summary>
    /// グループ名と所属アイテムを結合したフリーワード検索用文字列（小文字）。
    /// </summary>
    public required string FreeWord { get; init; }

    /// <summary>
    /// 指定されたトークンがこのインデックスに一致するかどうかを判定します。
    /// </summary>
    /// <param name="token">判定対象の検索トークン。</param>
    /// <param name="locKeyProvider">ローカライズキー変換関数（共通素体では使用されません）。</param>
    /// <returns>一致する場合は true。</returns>
    public bool IsMatch(SearchQueryToken token, Func<string, string>? locKeyProvider = null)
    {
        var targets = GetTargets(token.Field);
        if (targets.Length == 0) return false;

        const StringComparison comparison = StringComparison.CurrentCultureIgnoreCase;
        return token.IsNegation
            ? targets.All(t => !t.Contains(token.Value, comparison))
            : targets.Any(t => t.Contains(token.Value, comparison));
    }

    private string[] GetTargets(string? field)
    {
        return field?.ToLowerInvariant() switch
        {
            null => [FreeWord],
            _ => []
        };
    }

    /// <summary>
    /// 共通素体グループと所属アイテムの検索インデックスから、このインデックスを構築します。
    /// </summary>
    /// <param name="commonAvatar">対象の共通素体グループ。</param>
    /// <param name="itemSearchIndices">グループに含まれるアイテムの検索インデックス一覧。</param>
    /// <returns>構築された CommonAvatarSearchIndex。</returns>
    public static CommonAvatarSearchIndex Build(CommonAvatar commonAvatar, IEnumerable<ItemSearchIndex?> itemSearchIndices)
    {
        return new CommonAvatarSearchIndex
        {
            GroupName = commonAvatar.GroupName,
            FreeWord = string.Join("\n",
                commonAvatar.GroupName,
                string.Join("\n", itemSearchIndices.Select(i => i?.FreeWord ?? string.Empty))
            ).ToLowerInvariant()
        };
    }
}
