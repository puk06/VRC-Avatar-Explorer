using System.Collections.Generic;
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
        return type switch
        {
            ViewModelType.Avatar => CreateFromItem(viewModel.ActualValue ?? string.Empty),
            ViewModelType.Item => CreateFromItem(viewModel.Identifier),
            ViewModelType.Folder => CreateFromFolder(viewModel.ActualValue ?? string.Empty),
            ViewModelType.File => CreateFromItemFile(viewModel.ActualValue ?? string.Empty),
            ViewModelType.BulkImportPreset => CreateFromBulkImportPreset(viewModel.Identifier),
            ViewModelType.TempAvatar => CreateFromTempAvatar(viewModel.Identifier),
            ViewModelType.ItemCategory => CreateFromItemCategory(viewModel.Identifier),
            _ => []
        };
    }

    private static ContextMenuAction[] CreateFromItem(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.Item.CheckForUpdate, ActionKey.CheckForUpdate, ContextMenuIconType.Update, itemId, addSeparator: true),
            new(Loc.ContextMenu.Item.ShowOtherItemsByAuthor, ActionKey.ShowOtherItemsByAuthor, ContextMenuIconType.Open, itemId, addSeparator: true),

            new(Loc.ContextMenu.Item.Add.BulkImportList, ActionKey.AddToBulkImportList, ContextMenuIconType.Add, itemId),
            new(Loc.ContextMenu.Item.Add.File, ActionKey.None, ContextMenuIconType.Add, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.Add.File, ActionKey.AddItemFile, ContextMenuIconType.Add, itemId),
                    new(Loc.ContextMenu.Item.Add.Folder, ActionKey.AddItemFolder, ContextMenuIconType.Add, itemId)
                }
            },
            new(Loc.ContextMenu.Item.Booth.Open, ActionKey.OpenBoothLink, ContextMenuIconType.Open, itemId),
            new(Loc.ContextMenu.Item.Booth.Copy, ActionKey.CopyBoothLink, ContextMenuIconType.Copy, itemId, addSeparator: true),

            new(Loc.ContextMenu.Item.CopyItemInfo, ActionKey.CopyItemInfo, ContextMenuIconType.Copy, itemId, addSeparator: true),

            new(Loc.ContextMenu.Item.Edit.Tag, ActionKey.EditItemTag, ContextMenuIconType.Edit, itemId),
            new(Loc.ContextMenu.Item.Edit.Memo, ActionKey.EditItemMemo, ContextMenuIconType.Edit, itemId),
            new(Loc.ContextMenu.Item.Edit.Implemented, ActionKey.EditImplementedAvatar, ContextMenuIconType.Edit, itemId, addSeparator: true),
            new(Loc.ContextMenu.Item.Edit.Default, ActionKey.None, ContextMenuIconType.Edit, itemId, addSeparator: true)
            {
                SubMenuItems =
                {
                    new(Loc.ContextMenu.Item.Edit.Default, ActionKey.EditItem, ContextMenuIconType.Edit, itemId),
                    new(Loc.ContextMenu.Item.Edit.Title, ActionKey.EditItemTitle, ContextMenuIconType.Edit, itemId),
                    new(Loc.ContextMenu.Item.Edit.DefaultPath, ActionKey.EditItemDefaultPath, ContextMenuIconType.Edit, itemId, addSeparator: true),
                    new(Loc.ContextMenu.Item.Thumbnail.Change, ActionKey.ChangeThumbnail, ContextMenuIconType.Edit, itemId),
                    new(Loc.ContextMenu.Item.Thumbnail.Fetch, ActionKey.FetchThumbnail, ContextMenuIconType.Fetch, itemId)
                }
            },

            new(Loc.ContextMenu.Item.Remove, ActionKey.RemoveItem, ContextMenuIconType.Delete, itemId)
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromFolder(string path)
    {
        if (string.IsNullOrEmpty(path)) return [];
        List<ContextMenuAction> contextMenuActions = [];

        var isWindows = ProcessUtils.IsWindows();

        contextMenuActions.Add(new(Loc.ContextMenu.ItemFolder.OpenFolder, ActionKey.OpenFolder, ContextMenuIconType.Open, path, addSeparator: !isWindows));

        if (isWindows)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFolder.ShowInExplorer, ActionKey.ShowInExplorer, ContextMenuIconType.Open, path, addSeparator: true));
        }

        contextMenuActions.Add(new(Loc.ContextMenu.ItemFolder.RemoveFolder, ActionKey.RemoveFolder, ContextMenuIconType.Delete, path));

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemFile(string path)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.ItemFile.OpenFile, ActionKey.OpenFile, ContextMenuIconType.Open, path),
            new(Loc.ContextMenu.ItemFile.BulkImportList, ActionKey.AddFileToBulkImportList, ContextMenuIconType.Add, path)
        ];

        if (ProcessUtils.IsWindows())
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.ShowInExplorer, ActionKey.ShowInExplorer, ContextMenuIconType.Open, path));
        }

        if (PathUtils.IsUnitypackageFile(path))
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.OpenUnitypackageViewer, ActionKey.OpenUnitypackageViewer, ContextMenuIconType.Open, path));
        }

        if (PathUtils.IsPdfFile(path))
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemFile.OpenPdfViewer, ActionKey.OpenPdfViewer, ContextMenuIconType.Open, path));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromBulkImportPreset(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.BulkImportPreset.RemovePreset, ActionKey.RemovePreset, ContextMenuIconType.Delete, itemId),
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromTempAvatar(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new(Loc.ContextMenu.TempAvatar.EditTempAvatarName, ActionKey.EditTempAvatarName, ContextMenuIconType.Edit, itemId),
            new(Loc.ContextMenu.TempAvatar.ResolveTempAvatar, ActionKey.ResolveTempAvatar, ContextMenuIconType.Link, itemId, addSeparator: true),
            new(Loc.ContextMenu.TempAvatar.RemoveTempAvatar, ActionKey.RemoveTempAvatar, ContextMenuIconType.Delete, itemId),
        ];

        return contextMenuActions.ToArray();
    }
    
    private static ContextMenuAction[] CreateFromItemCategory(string category)
    {
        List<ContextMenuAction> contextMenuActions = [];

        var itemCategory = ItemCategory.FromIdentifier(category);

        if (itemCategory.Type == ItemType.Custom)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemCategory.EditCustomCategoryName, ActionKey.EditCustomCategoryName, ContextMenuIconType.Edit, category));
        }

        if (itemCategory.Type != ItemType.None && itemCategory.Type != ItemType.All)
        {
            contextMenuActions.Add(new(Loc.ContextMenu.ItemCategory.MergeWithOtherCategory, ActionKey.MergeWithOtherCategory, ContextMenuIconType.Merge, category));
        }

        return contextMenuActions.ToArray();
    }
}
