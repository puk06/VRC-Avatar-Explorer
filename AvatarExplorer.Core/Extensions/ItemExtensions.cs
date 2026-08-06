using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;

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
        List<string> folderList = new();

        if (includeRootFolder && Directory.Exists(item.ItemPath)) folderList.Add(item.ItemPath);

        foreach (string itemPath in item.ItemPaths)
        {
            if (Directory.Exists(itemPath)) folderList.Add(itemPath);
        }

        return folderList.SortByFileName();
    }
}
