using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Extensions;

public static class ItemExtensions
{
    public static IEnumerable<string> GetFolderPaths(this Item item, bool includeRootFolder = true)
    {
        var folderList = new List<string>();

        var rootPath = item.GetItemPath();
        if (includeRootFolder && Directory.Exists(rootPath)) folderList.Add(rootPath);

        folderList.AddRange(item.ItemPaths.Where(Directory.Exists));

        return folderList.SortByFileName();
    }

    public static string GetItemPath(this Item item) => ItemUtils.GetFullPath(item.ItemPath);
}
