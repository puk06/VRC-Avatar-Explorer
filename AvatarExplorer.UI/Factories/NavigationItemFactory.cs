using System;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.UI.Services.ViewControl;

namespace AvatarExplorer.UI.Factories;

public static class NavigationItemFactory
{
    public static ItemViewModel CreateFromNavigationable(INavigationable source)
    {
        if (source is Avatar avatar)
        {
            return new ItemViewModel
            {
                ImageFileName = avatar.Item.ThumbnailFileName,
                TitleRaw = avatar.Item.Title,
                TitleLocalizable = false,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Author, [avatar.Item.Author]),
                Identifier = source.Identifier,
                ViewModelType = ViewModelType.Item
            };
        }

        if (source is Item item)
        {
            return new ItemViewModel
            {
                ImageFileName = item.ThumbnailFileName,
                TitleRaw = item.Title,
                TitleLocalizable = false,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Author, [item.Author]),
                Identifier = source.Identifier,
                ViewModelType = ViewModelType.Item
            };
        }

        if (source is Author author)
        {
            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.AvatarIcon,
                TitleRaw = author.Name,
                TitleLocalizable = false,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [author.ItemCount.ToString()]),
                Identifier = source.Identifier,
                ViewModelType = ViewModelType.None
            };
        }

        if (source is Folder folder)
        {
            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.FolderIcon,
                TitleRaw = folder.Title,
                TitleLocalizable = folder.TitleLocalizable,
                DescriptionRaw = new(LocalizationKey.Button.Description.Item.Count, [folder.ItemCount.ToString()]),
                Identifier = folder.Identifier,
                ViewModelType = ViewModelType.Folder
            };
        }

        if (source is ItemFile file)
        {
            var hasExtension = !string.IsNullOrEmpty(file.Extension);

            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.FileIcon,
                TitleRaw = file.FileName,
                TitleLocalizable = false,
                DescriptionRaw = new(hasExtension ? LocalizationKey.Button.Description.File.Extension : LocalizationKey.Button.Description.File.NoExtension, [file.Extension]),
                Identifier = file.Identifier,
                ViewModelType = ViewModelType.File
            };
        }

        return new ItemViewModel
        {
            ImageFileName = SystemIconKey.None,
            TitleRaw = string.Empty,
            TitleLocalizable = false,
            DescriptionRaw = new(string.Empty, []),
            Identifier = string.Empty,
            ViewModelType = ViewModelType.None
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
