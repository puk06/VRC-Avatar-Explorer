using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Models.System;

/// <summary>
/// 仮アバターの検索インデックス。アバター名およびフリーワードで検索できます。
/// </summary>
public record TempAvatarSearchIndex : ISearchIndex
{
    /// <summary>
    /// 仮アバターの名前。
    /// </summary>
    public required string AvatarName { get; init; }

    /// <summary>
    /// 仮アバターのBooth 商品 ID（文字列）。
    /// </summary>
    public required string BoothId { get; init; }

    /// <summary>
    /// フリーワード検索用文字列（アバター名の小文字表現）。
    /// </summary>
    public required string FreeWord { get; init; }

    /// <summary>
    /// 指定されたトークンがこのインデックスに一致するかどうかを判定します。
    /// </summary>
    /// <param name="token">判定対象の検索トークン。</param>
    /// <param name="locKeyProvider">ローカライズキー変換関数（仮アバターでは使用されません）。</param>
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

    public int CountMatches(IReadOnlyList<SearchQueryToken> tokens, Func<string, string>? locKeyProvider = null)
    {
        return tokens.Count(i => IsMatch(i, locKeyProvider));
    }

    private string[] GetTargets(string? field)
    {
        return field?.ToLowerInvariant() switch
        {
            "title" => [AvatarName],
            "boothid" or "booth" => [BoothId],
            null => [FreeWord],
            _ => []
        };
    }

    /// <summary>
    /// 仮アバターからこの検索インデックスを構築します。
    /// </summary>
    /// <param name="tempAvatar">対象の仮アバター。</param>
    /// <returns>構築された TempAvatarSearchIndex。</returns>
    public static TempAvatarSearchIndex Build(TempAvatar tempAvatar)
    {
        var freeWord = string.Join("\n",
            tempAvatar.AvatarName,
            tempAvatar.BoothId >= 0 ? tempAvatar.BoothId.ToString() : string.Empty
        ).ToLowerInvariant();

        return new TempAvatarSearchIndex
        {
            AvatarName = tempAvatar.AvatarName,
            BoothId = tempAvatar.BoothId >= 0 ? tempAvatar.BoothId.ToString() : string.Empty,
            FreeWord = freeWord
        };
    }
}
