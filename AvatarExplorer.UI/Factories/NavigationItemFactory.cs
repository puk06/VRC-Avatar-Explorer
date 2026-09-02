using System.Linq;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;

namespace AvatarExplorer.UI.Factories;

public static class NavigationItemFactory
{
    public static ItemViewModel CreateFromNavigationable(IIdentifiable source) => source switch
    {
        Avatar avatar => FromAvatar(avatar),
        Item item => FromItem(item, source.Identifier),
        Author author => FromAuthor(author, source.Identifier),
        Folder folder => FromFolder(folder),
        ItemFile file => FromItemFile(file),
        _ => CreateEmpty()
    };

    private static ItemViewModel FromItem(Item item, string identifier) => new()
    {
        ThumbnailSource = new() { Primary = item.ThumbnailFileName, Fallback = SystemIconKey.FileIcon },
        TitleRaw = item.Title,
        TitleLocalizable = false,
        DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]),
        Identifier = identifier,
        ViewModelType = ViewModelType.Item,
        Tags = item.Tags.Select(t => new TagViewModel { ValueRaw = t }).ToArray(),
        CreatedDate = item.CreatedDate,
        UpdatedDate = item.UpdatedDate,
        ItemMemo = item.ItemMemo
    };

    private static ItemViewModel FromAuthor(Author author, string identifier) => new()
    {
        ThumbnailSource = new() { Primary = string.Empty },
        TitleRaw = author.Name,
        TitleLocalizable = false,
        DescriptionRaw = new(Loc.Button.Description.Item.Count, [author.ItemCount.ToString()]),
        Identifier = identifier,
        ViewModelType = ViewModelType.None
    };

    private static ItemViewModel FromFolder(Folder folder)
    {
        var isCategory = ItemCategory.IsCategoryIdentifier(folder.Identifier);
        var isHiddenCategory = isCategory && ItemCategory.FromIdentifier(folder.Identifier).Type == ItemType.Hidden;

        return new ItemViewModel
        {
            ThumbnailSource = new() { Primary = isHiddenCategory ? SystemIconKey.HiddenFolderIcon : SystemIconKey.FolderIcon },
            TitleRaw = folder.Title,
            TitleLocalizable = folder.TitleLocalizable,
            DescriptionRaw = new(Loc.Button.Description.Item.Count, [folder.ItemCount.ToString()]),
            Identifier = folder.Identifier,
            ViewModelType = isCategory ? ViewModelType.ItemCategory : ViewModelType.Folder,
            ActualValue = folder.Path
        };
    }

    private static ItemViewModel FromItemFile(ItemFile file)
    {
        var hasExtension = !string.IsNullOrEmpty(file.Extension);
        var isImageFile = ImageService.IsImageFile(file.FilePath);

        return new ItemViewModel
        {
            ThumbnailSource = new() { Primary = SystemIconKey.FileIcon, FilePath = isImageFile ? file.FilePath : null },
            TitleRaw = file.FileName,
            TitleLocalizable = false,
            DescriptionRaw = new(hasExtension ? Loc.Button.Description.File.Extension : Loc.Button.Description.File.NoExtension, [file.Extension]),
            Identifier = file.Identifier,
            ViewModelType = ViewModelType.File,
            ActualValue = file.FilePath
        };
    }

    private static ItemViewModel FromAvatar(Avatar avatar) => avatar.Type switch
    {
        AvatarType.Item => FromAvatarItem(avatar, (Item)avatar.Item),
        AvatarType.CommonAvatar => FromCommonAvatar(avatar, (CommonAvatar)avatar.Item),
        AvatarType.TempAvatar => FromTempAvatar(avatar, (TempAvatar)avatar.Item),
        _ => CreateEmpty()
    };

    private static ItemViewModel FromAvatarItem(Avatar avatar, Item item) => new()
    {
        ThumbnailSource = new() { Primary = item.ThumbnailFileName, Fallback = SystemIconKey.FileIcon },
        TitleRaw = item.Title,
        TitleLocalizable = false,
        DescriptionRaw = new(Loc.Button.Description.Item.Author, [item.Author]),
        Identifier = avatar.Identifier,
        ActualValue = item.Identifier,
        ViewModelType = ViewModelType.Avatar,
        Tags = item.Tags.Select(t => new TagViewModel { ValueRaw = t }).ToArray(),
        CreatedDate = item.CreatedDate,
        UpdatedDate = item.UpdatedDate,
        ItemMemo = item.ItemMemo
    };

    private static ItemViewModel FromCommonAvatar(Avatar avatar, CommonAvatar commonAvatar) => new()
    {
        ThumbnailSource = new() { Primary = SystemIconKey.GroupIcon },
        TitleRaw = commonAvatar.GroupName,
        TitleLocalizable = false,
        DescriptionRaw = new(Loc.Button.Description.CommonAvatar.Count, [commonAvatar.Avatars.Length.ToString()]),
        Identifier = avatar.Identifier,
        ActualValue = commonAvatar.Identifier,
        ViewModelType = ViewModelType.CommonAvatar
    };

    private static ItemViewModel FromTempAvatar(Avatar avatar, TempAvatar tempAvatar)
    {
        var vm = new ItemViewModel()
        {
            ThumbnailSource = new() { Primary = SystemIconKey.AvatarIcon },
            TitleRaw = tempAvatar.AvatarName,
            TitleLocalizable = false,
            DescriptionRaw = new(Loc.Button.Description.TempAvatar),
            Identifier = avatar.Identifier,
            ActualValue = tempAvatar.Identifier,
            ViewModelType = ViewModelType.TempAvatar
        };

        if (tempAvatar.BoothId != -1)
        {
            vm.Tags = [new() { ValueRaw = tempAvatar.BoothId.ToString(), IsBoothId = true }];
        }

        return vm;
    }

    private static ItemViewModel CreateEmpty() => new()
    {
        ThumbnailSource = new() { Primary = SystemIconKey.None },
        TitleRaw = string.Empty,
        TitleLocalizable = false,
        DescriptionRaw = new(string.Empty, []),
        Identifier = string.Empty,
        ViewModelType = ViewModelType.None
    };
}
