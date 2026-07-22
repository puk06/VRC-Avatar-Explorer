using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;

namespace AvatarExplorer.Core.Extensions;

public static class ItemExtensions
{
    internal static IEnumerable<Item> GetSortedItems(this IEnumerable<Item> items, RuntimeSettings runtimeSettings)
    {
        return runtimeSettings.ItemSortOrder switch
        {
            // TODO: Implementedも作る。RemoveBracketでも。
            ItemSortOrder.Title => items.OrderBy(item => item.Title),
            ItemSortOrder.Author => items.OrderBy(item => item.Author),
            ItemSortOrder.Created => items.OrderByDescending(item => item.CreatedDate),
            ItemSortOrder.Updated => items.OrderByDescending(item => item.UpdatedDate),
            _ => items.OrderBy(item => item.Title)
        };
    }

    internal static bool IsCategoryMatch(this Item item, string identifier)
    {
        if (identifier == $"type:{(int)ItemType.All}") return true;
        return item.Category.Identifier == identifier;
    }

    public static IEnumerable<string> EnumerateFiles(this Item item, bool isRecursive = true)
    {
        List<string> fileList = new();

        foreach (string itemPath in item.GetFolderPaths())
        {
            fileList.AddRange(FileSystemService.EnumerateFiles(itemPath, isRecursive));
        }

        return fileList.SortByFileName();
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
