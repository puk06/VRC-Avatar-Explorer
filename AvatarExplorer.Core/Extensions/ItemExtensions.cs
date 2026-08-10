using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Extensions;

public static class ItemExtensions
{
    internal static bool IsCategoryMatch(this Item item, string identifier)
    {
        if (identifier == $"type:{(int)ItemType.All}") return true;
        return item.Category.Identifier == identifier;
    }

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
