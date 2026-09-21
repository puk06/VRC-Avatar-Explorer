using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.Services.ViewControl;

public enum ViewModelType
{
    None,
    Avatar,
    Item,
    CommonAvatar,
    Folder,
    File,
    BulkImportPreset,
    TempAvatar,
    ItemCategory
}

internal static class ContextMenuCreator
{
    internal static ContextMenuAction[] Create(ViewModelType type, ItemViewModel viewModel)
    {
        // HandlerにはItemViewModelのActualValue ?? Identifierを渡すので、ここでは必要ない。
        return type switch
        {
            ViewModelType.Avatar => CreateFromItem(viewModel.ActualValue ?? string.Empty, isAvatar: true),
            ViewModelType.Item => CreateFromItem(viewModel.Identifier),
            ViewModelType.Folder => CreateFromFolder(viewModel.ActualValue ?? string.Empty),
            ViewModelType.File => CreateFromItemFile(viewModel.ActualValue ?? string.Empty),
            ViewModelType.BulkImportPreset => CreateFromBulkImportPreset(),
            ViewModelType.TempAvatar => CreateFromTempAvatar(),
            ViewModelType.ItemCategory => CreateFromItemCategory(viewModel.Identifier),
            _ => []
        };
    }

    private static ContextMenuAction[] CreateFromItem(string identifier, bool isAvatar = false)
    {
        if (string.IsNullOrEmpty(identifier)) return [];

        var item = InstanceRepository.Items.Get(identifier);
        if (item == null) return [];

        var hasBoothId = item.BoothId != -1;
        var isHidden = item.IsHidden;
        var skipIndirectCommonAvatarCheck = item.SkipIndirectCommonAvatarCheck;

        var contextMenuActions = new List<ContextMenuAction>();

        if (isAvatar)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.Item.ShowInMainView, ActionKey.ShowInMainView, ContextMenuIconType.Open, addSeparator: true));
        }

        contextMenuActions.AddRange(
        [
            new(Loc.ContextMenu.Item.CheckForUpdate, ActionKey.CheckForUpdate, ContextMenuIconType.Update, isEnabled: hasBoothId, addSeparator: true),
            new(Loc.ContextMenu.Item.ShowOtherItemsByAuthor, ActionKey.ShowOtherItemsByAuthor, ContextMenuIconType.Open, addSeparator: true),

            new(Loc.ContextMenu.Item.Add.BulkImportList, ActionKey.AddToBulkImportList, ContextMenuIconType.Add),
            new(Loc.ContextMenu.Item.Add.Content, ActionKey.None, ContextMenuIconType.Add, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.Add.File, ActionKey.AddItemFile, ContextMenuIconType.AddFile),
                    new(Loc.ContextMenu.Item.Add.Folder, ActionKey.AddItemFolder, ContextMenuIconType.AddFolder),
                    new(Loc.ContextMenu.Item.Add.Url, ActionKey.AddItemUrl, ContextMenuIconType.AddUrl),
                }
            },
            new(Loc.ContextMenu.Item.Booth.Open, ActionKey.OpenBoothLink, ContextMenuIconType.Open, isEnabled: hasBoothId),
            new(Loc.ContextMenu.Item.Booth.Copy, ActionKey.CopyBoothLink, ContextMenuIconType.Copy, isEnabled: hasBoothId, addSeparator: true),

            new(Loc.ContextMenu.Item.CopyItemInfo, ActionKey.CopyItemInfo, ContextMenuIconType.Copy, addSeparator: true),

            new(Loc.ContextMenu.Item.Edit.Tag, ActionKey.EditItemTag, ContextMenuIconType.Tag),
            new(Loc.ContextMenu.Item.Edit.Memo, ActionKey.EditItemMemo, ContextMenuIconType.NoteEdit),

            new(Loc.ContextMenu.Item.Edit.Implemented, ActionKey.None, ContextMenuIconType.Avatars, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.Edit.MarkItemAsImplemented, ActionKey.MarkItemAsImplemented, ContextMenuIconType.Implemented),
                    new(Loc.ContextMenu.Item.Edit.MarkItemAsNotImplemented, ActionKey.MarkItemAsNotImplemented, ContextMenuIconType.NotImplemented, addSeparator: true),
                    new(Loc.ContextMenu.Item.Edit.ManageImplementedAvatars, ActionKey.ManageImplementedAvatars, ContextMenuIconType.Avatars)
                }
            },

            new(Loc.ContextMenu.Item.Edit.Default, ActionKey.None, ContextMenuIconType.Edit, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.Edit.Default, ActionKey.EditItem, ContextMenuIconType.Edit),
                    new(Loc.ContextMenu.Item.Edit.Title, ActionKey.EditItemTitle, ContextMenuIconType.Edit),
                    new(Loc.ContextMenu.Item.Edit.DefaultPath, ActionKey.EditItemDefaultPath, ContextMenuIconType.Edit, addSeparator: true),
                    new(Loc.ContextMenu.Item.Thumbnail.Change, ActionKey.ChangeThumbnail, ContextMenuIconType.Edit),
                    new(Loc.ContextMenu.Item.Thumbnail.Fetch, ActionKey.FetchThumbnail, ContextMenuIconType.Fetch, isEnabled: hasBoothId)
                }
            },

            new(Loc.ContextMenu.Item.Visibility, ActionKey.None, ContextMenuIconType.Visible, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.HideItem, ActionKey.HideItem, ContextMenuIconType.Hidden, isEnabled: !isHidden),
                    new(Loc.ContextMenu.Item.ShowItem, ActionKey.ShowItem, ContextMenuIconType.Visible, isEnabled: isHidden)
                }
            },

            new(Loc.ContextMenu.Item.CommonAvatarCheck, ActionKey.None, ContextMenuIconType.Avatars, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.SkipIndirectCommonAvatarCheck, ActionKey.SkipIndirectCommonAvatarCheck, ContextMenuIconType.ExcludeCommonAvatarCheck, isEnabled: !skipIndirectCommonAvatarCheck),
                    new(Loc.ContextMenu.Item.EnableIndirectCommonAvatarCheck, ActionKey.EnableIndirectCommonAvatarCheck, ContextMenuIconType.IncludeCommonAvatarCheck, isEnabled: skipIndirectCommonAvatarCheck)
                }
            },

            new(Loc.ContextMenu.Item.Remove, ActionKey.RemoveItem, ContextMenuIconType.Delete)
        ]);

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return [];

        var isWindows = ProcessUtils.IsWindows();

        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.ItemFolder.OpenFolder, ActionKey.OpenFolder, ContextMenuIconType.Open, addSeparator: !isWindows),
        ];

        if (isWindows)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFolder.ShowInExplorer, ActionKey.ShowInExplorer, ContextMenuIconType.Open, addSeparator: true));
        }

        contextMenuActions.Add(new(Loc.ContextMenu.ItemFolder.CopyFolderPath, ActionKey.CopyPath, ContextMenuIconType.Copy, addSeparator: true));
        contextMenuActions.Add(new(Loc.ContextMenu.ItemFolder.RemoveFolder, ActionKey.RemoveFolder, ContextMenuIconType.Delete));

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return [];

        var isWindows = ProcessUtils.IsWindows();

        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.ItemFile.OpenFile, ActionKey.OpenFile, ContextMenuIconType.Open, addSeparator: !isWindows),
        ];

        if (isWindows)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.ShowInExplorer, ActionKey.ShowInExplorer, ContextMenuIconType.Open, addSeparator: true));
        }

        contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.CopyFilePath, ActionKey.CopyPath, ContextMenuIconType.Copy, addSeparator: true));
        contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.BulkImportList, ActionKey.AddFileToBulkImportList, ContextMenuIconType.Add));

        if (PathUtils.IsUnitypackageFile(path))
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.OpenUnitypackageViewer, ActionKey.OpenUnitypackageViewer, ContextMenuIconType.Open));
        }

        if (PathUtils.IsPdfFile(path))
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.OpenPdfViewer, ActionKey.OpenPdfViewer, ContextMenuIconType.Open));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromBulkImportPreset()
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.BulkImportPreset.RemovePreset, ActionKey.RemovePreset, ContextMenuIconType.Delete),
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromTempAvatar()
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.TempAvatar.EditTempAvatarName, ActionKey.EditTempAvatarName, ContextMenuIconType.Edit),
            new(Loc.ContextMenu.TempAvatar.EditBoothId, ActionKey.EditBoothId, ContextMenuIconType.Edit),
            new(Loc.ContextMenu.TempAvatar.ResolveTempAvatar, ActionKey.ResolveTempAvatar, ContextMenuIconType.Link, addSeparator: true),
            new(Loc.ContextMenu.TempAvatar.RemoveTempAvatar, ActionKey.RemoveTempAvatar, ContextMenuIconType.Delete),
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemCategory(string category)
    {
        List<ContextMenuAction> contextMenuActions = [];

        var itemCategory = ItemCategory.FromIdentifier(category);

        if (itemCategory.Type == ItemType.Custom)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemCategory.EditCustomCategoryName, ActionKey.EditCustomCategoryName, ContextMenuIconType.Edit));
        }

        if (itemCategory.Type != ItemType.None && itemCategory.Type != ItemType.All && itemCategory.Type != ItemType.Hidden)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemCategory.MergeWithOtherCategory, ActionKey.MergeWithOtherCategory, ContextMenuIconType.Merge));
        }

        return contextMenuActions.ToArray();
    }
}
