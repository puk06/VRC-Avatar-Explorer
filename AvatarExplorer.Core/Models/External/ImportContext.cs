using AvatarExplorer.Core.Services.System.Repositories;

namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// インポート処理へ渡される内部コンテキスト。インポート先となるリポジトリ一式を保持します。
/// </summary>
public class ImportContext
{
    /// <summary>
    /// インポート先となるアイテムのリポジトリ。
    /// </summary>
    public required ItemRepository Items { get; init; }

    /// <summary>
    /// インポート先となる共通素体のリポジトリ。
    /// </summary>
    public required CommonAvatarRepository CommonAvatars { get; init; }

    /// <summary>
    /// インポート先となる仮アバターのリポジトリ。
    /// </summary>
    public required TempAvatarRepository TempAvatars { get; init; }
}