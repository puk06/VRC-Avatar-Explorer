using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;

namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// エクスポート処理へ渡される内部コンテキスト。エクスポート対象のデータ一式を保持します。
/// </summary>
public class ExportContext
{
    /// <summary>
    /// エクスポート対象となる全アイテム。
    /// </summary>
    public required IEnumerable<Item> Items { get; init; }

    /// <summary>
    /// エクスポート対象となる全共通素体グループ。
    /// </summary>
    public required IEnumerable<CommonAvatar> CommonAvatars { get; init; }

    /// <summary>
    /// エクスポート対象となる全仮アバター。
    /// </summary>
    public required IEnumerable<TempAvatar> TempAvatars { get; init; }

    /// <summary>
    /// エクスポート時に参照される実行時設定。
    /// </summary>
    public required RuntimeSettings RuntimeSettings { get; init; }
}
