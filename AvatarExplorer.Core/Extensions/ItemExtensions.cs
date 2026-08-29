using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// <see cref="Item"/> に対するパス関連の拡張メソッドを提供します。
/// </summary>
public static class ItemExtensions
{
    /// <summary>
    /// アイテムが存在するフォルダパスの一覧を取得します。ルートフォルダと、実在する各アイテムパスが含まれます。
    /// </summary>
    /// <param name="item">対象のアイテム。</param>
    /// <param name="includeRootFolder">ルートフォルダを含めるかどうか。</param>
    /// <returns>実在するフォルダパスの列挙。</returns>
    public static IEnumerable<string> GetFolderPaths(this Item item, bool includeRootFolder = true)
    {
        var folderList = new List<string>();

        var rootPath = item.GetItemPath();
        if (includeRootFolder && Directory.Exists(rootPath)) folderList.Add(rootPath);

        folderList.AddRange(item.ItemPaths.Where(Directory.Exists));

        return folderList;
    }

    /// <summary>
    /// アイテムの相対パスを実際のフルパスに変換して取得します。
    /// </summary>
    /// <param name="item">対象のアイテム。</param>
    /// <returns>アイテムのフルパス。</returns>
    public static string GetItemPath(this Item item) => ItemUtils.GetFullPath(item.ItemPath);
}
