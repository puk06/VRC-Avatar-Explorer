using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Extensions;

public static class ItemExtensions
{
    public static IEnumerable<string> GetFolderPaths(this Item item, bool includeRootFolder = true)
    {
        var folderList = new List<string>();

        if (includeRootFolder && Directory.Exists(item.ItemPath)) folderList.Add(item.ItemPath);

        foreach (var itemPath in item.ItemPaths)
        {
            if (Directory.Exists(itemPath)) folderList.Add(itemPath);
        }

        return folderList.SortByFileName();
    }
}
