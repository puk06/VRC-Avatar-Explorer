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
            return FromAvatar(avatar);
        }

        if (source is Item item)
        {
            return new ItemViewModel
            {
                ImageFileName = item.ThumbnailFileName,
                TitleRaw = item.Title,
                TitleLocalizable = false,
                DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]),
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
                DescriptionRaw = new(Loc.Button.Description.Item.Count, [author.ItemCount.ToString()]),
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
                DescriptionRaw = new(Loc.Button.Description.Item.Count, [folder.ItemCount.ToString()]),
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
                DescriptionRaw = new(hasExtension ? Loc.Button.Description.File.Extension : Loc.Button.Description.File.NoExtension, [file.Extension]),
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

    private static ItemViewModel FromAvatar(Avatar avatar)
    {
        if (avatar.Type == AvatarType.Item)
        {
            var item = (Item)avatar.Item;
            return new ItemViewModel
            {
                ImageFileName = item.ThumbnailFileName,
                TitleRaw = item.Title,
                TitleLocalizable = false,
                DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]),
                Identifier = avatar.Identifier,
                ViewModelType = ViewModelType.Item
            };
        }
        else if (avatar.Type == AvatarType.CommonAvatar)
        {
            var commonAvatar = (CommonAvatar)avatar.Item;

            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.GroupIcon,
                TitleRaw = commonAvatar.GroupName,
                TitleLocalizable = false,
                DescriptionRaw = new(Loc.Button.Description.CommonAvatar.Count, [commonAvatar.Avatars.Length.ToString()]),
                Identifier = avatar.Identifier,
                ViewModelType = ViewModelType.Item
            };
        }
        else if (avatar.Type == AvatarType.TempAvatar)
        {
            var tempAvatar = (TempAvatar)avatar.Item;

            return new ItemViewModel
            {
                ImageFileName = SystemIconKey.AvatarIcon,
                TitleRaw = tempAvatar.AvatarName,
                TitleLocalizable = false,
                DescriptionRaw = new(Loc.Button.Description.TempAvatar),
                Identifier = avatar.Identifier,
                ViewModelType = ViewModelType.Item
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
}
