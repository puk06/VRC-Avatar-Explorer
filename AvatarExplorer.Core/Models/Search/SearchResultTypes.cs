namespace AvatarExplorer.Core.Models.Search;

/// <summary>
/// 検索結果に含める対象の種別を指定するフラグ列挙型。
/// </summary>
[Flags]
public enum SearchResultTypes
{
    /// <summary>
    /// 検索なし / 未指定。
    /// </summary>
    None = 0,

    /// <summary>
    /// アイテムを検索対象にする。
    /// </summary>
    Items = 1,

    /// <summary>
    /// 共通素体グループを検索対象にする。
    /// </summary>
    CommonAvatar = 2,

    /// <summary>
    /// 仮アバターを検索対象にする。
    /// </summary>
    TempAvatar = 4,

    /// <summary>
    /// 全ての種別（Items | CommonAvatar | TempAvatar）を検索対象にする。
    /// </summary>
    All = Items | CommonAvatar | TempAvatar
}
