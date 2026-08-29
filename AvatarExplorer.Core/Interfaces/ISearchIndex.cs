using AvatarExplorer.Core.Models.Search;

namespace AvatarExplorer.Core.Interfaces;

/// <summary>
/// 検索インデックスの基本インターフェース。
/// 他のデータベースタイプに対しても同様に実装して拡張できます。
/// </summary>
public interface ISearchIndex
{
    bool IsMatch(SearchQueryToken token, Func<string, string>? locKeyProvider = null);

    int CountMatches(IReadOnlyList<SearchQueryToken> tokens, Func<string, string>? locKeyProvider = null);
}
