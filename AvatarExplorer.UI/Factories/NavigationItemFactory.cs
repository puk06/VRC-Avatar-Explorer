using System;
using System.IO;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.Factories;

public static class NavigationItemFactory
{
    public static ItemViewModel CreateFromSelectableItem(ISelectableItem source)
    {
        if (source is Item item)
        {
            return new ItemViewModel
            {
                ImageFileName = item.ThumbnailFileName,
                IconType = IconType.Item,
                TitleRaw = item.Title,
                TitleLocalizable = false,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Author, [item.Author]),
                Tag = source.Identifier
            };
        }

        if (source is Author author)
        {
            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.AvatarIcon,
                IconType = IconType.None,
                TitleRaw = author.Name,
                TitleLocalizable = false,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [author.ItemCount.ToString()]),
                Tag = source.Identifier
            };
        }

        if (source is Folder folder)
        {
            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.FolderIcon,
                IconType = IconType.None,
                TitleRaw = folder.Title,
                TitleLocalizable = folder.TitleLocalizable,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [folder.ItemCount.ToString()]),
                Tag = folder.Identifier
            };
        }

        if (source is ItemFile file)
        {
            var hasExtension = !string.IsNullOrEmpty(file.Extension);

            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.FileIcon,
                IconType = IconType.None,
                TitleRaw = file.FileName,
                TitleLocalizable = false,
                DescriptionRaw = new(hasExtension ? LocalizationKey.Button.Description.File.Extension : LocalizationKey.Button.Description.File.NoExtension, [file.Extension]),
                Tag = file.Identifier
            };
        }

        return new ItemViewModel
        {
            ImageFileName = SystemIconKey.None,
            IconType = IconType.None,
            TitleRaw = string.Empty,
            TitleLocalizable = false,
            DescriptionRaw = new(string.Empty, []),
            Tag = string.Empty
        };
    }

    public static ItemViewModel CreateCategoryNode(string key, int count)
    {
        return new ItemViewModel
        {
            ImageFileName = SystemIconKey.FolderIcon,
            IconType = IconType.None,
            TitleRaw = GetCategoryLocalizationKey(key),
            TitleLocalizable = key.StartsWith("type:"),
            DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [count.ToString()]),
            Tag = key
        };
    }

    public static ItemViewModel CreateFolderNode(string folderPath, int fileCount)
    {
        var folderName = Path.GetFileName(folderPath);
        if (string.IsNullOrWhiteSpace(folderName))
            folderName = folderPath;

        return new ItemViewModel
        {
            ImageFileName = SystemIconKey.FolderIcon,
            IconType = IconType.None,
            TitleRaw = folderName,
            TitleLocalizable = false,
            DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [fileCount.ToString()]),
            Tag = $"folder:{folderPath}"
        };
    }

    public static ItemViewModel CreateFileCategoryNode(ItemFileCategoryType categoryType, int fileCount)
    {
        var localizationKey = categoryType.GetLocalizationKey();
        return new ItemViewModel
        {
            ImageFileName = SystemIconKey.FolderIcon,
            IconType = IconType.None,
            TitleRaw = localizationKey ?? categoryType.ToString(),
            TitleLocalizable = !string.IsNullOrEmpty(localizationKey),
            DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [fileCount.ToString()]),
            Tag = $"filecategory:{categoryType}"
        };
    }

    public static ItemViewModel CreateFileButton(ItemFile file)
    {
        var description = string.IsNullOrEmpty(file.Extension)
            ? new LoclizableDescription(LocalizationKey.Button.Description.File.NoExtension, [])
            : new LoclizableDescription(LocalizationKey.Button.Description.File.Extension, [file.Extension]);

        return new ItemViewModel
        {
            ImageFileName = SystemIconKey.FileIcon,
            IconType = IconType.None,
            TitleRaw = file.FileName,
            TitleLocalizable = false,
            DescriptionRaw = description,
            Tag = file.FilePath
        };
    }

    private static string GetCategoryLocalizationKey(string groupKey)
    {
        if (groupKey.StartsWith("type:"))
        {
            var raw = groupKey["type:".Length..];
            if (Enum.TryParse<ItemType>(raw, out var itemType))
            {
                var key = itemType.GetLocalizationKey();
                return string.IsNullOrEmpty(key) ? raw : key;
            }

            return raw;
        }

        if (groupKey.StartsWith("custom:"))
            return groupKey["custom:".Length..];

        return groupKey;
    }
}
