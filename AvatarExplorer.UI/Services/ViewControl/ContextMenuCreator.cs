using System;
using System.Collections.Generic;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Models.ContextMenu;

namespace AvatarExplorer.UI.Services.ViewControl;

public enum ViewModelType
{
    None,
    Item,
    Author,
    Folder,
    File,
    BulkImportPreset,
    TempAvatar,
    ItemCategory
}

internal static class ContextMenuCreator
{
    // TODO: Identifierから内部のIdに変換する部分を作る
    internal static ContextMenuAction[] Create(ViewModelType type, string identifier)
    {
        return type switch
        {
            ViewModelType.Item => CreateFromItem(identifier),
            ViewModelType.Folder => CreateFromFolder(identifier),
            ViewModelType.File => CreateFromItemFile(identifier),
            ViewModelType.BulkImportPreset => CreateFromBulkImportPreset(identifier),
            ViewModelType.TempAvatar => CreateFromTempAvatar(identifier),
            ViewModelType.ItemCategory => CreateFromItemCategory(identifier),
            _ => []
        };
    }

    private static ContextMenuAction[] CreateFromItem(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(Loc.ContextMenu.Item.OpenFolder, ActionKey.OpenItemFolder, ContextMenuIconType.Open, itemId),
            new ContextMenuAction(Loc.ContextMenu.Item.ShowOtherItemsByAuthor, ActionKey.ShowOtherItemsByAuthor, ContextMenuIconType.Open, itemId, addSeparator: true),

            new ContextMenuAction(Loc.ContextMenu.Item.Add.BulkImportList, ActionKey.AddToBulkImportList, ContextMenuIconType.Add, itemId),
            new ContextMenuAction(Loc.ContextMenu.Item.Add.File, ActionKey.None, ContextMenuIconType.Add, addSeparator: true)
            {
                SubMenuItems =
                {
                    new ContextMenuAction(Loc.ContextMenu.Item.Add.File, ActionKey.AddItemFile, ContextMenuIconType.Add, itemId),
                    new ContextMenuAction(Loc.ContextMenu.Item.Add.Folder, ActionKey.AddItemFolder, ContextMenuIconType.Add, itemId)
                }
            },
            new ContextMenuAction(Loc.ContextMenu.Item.Booth.Open, ActionKey.OpenBoothLink, ContextMenuIconType.Open, itemId),
            new ContextMenuAction(Loc.ContextMenu.Item.Booth.Copy, ActionKey.CopyBoothLink, ContextMenuIconType.Copy, itemId, addSeparator: true),

            new ContextMenuAction(Loc.ContextMenu.Item.CopyItemInfo, ActionKey.CopyItemInfo, ContextMenuIconType.Copy, itemId, addSeparator: true),

            new ContextMenuAction(Loc.ContextMenu.Item.Edit.Tag, ActionKey.EditItemTag, ContextMenuIconType.Edit, itemId),
            new ContextMenuAction(Loc.ContextMenu.Item.Edit.Memo, ActionKey.EditItemMemo, ContextMenuIconType.Edit, itemId),
            new ContextMenuAction(Loc.ContextMenu.Item.Edit.Implemented, ActionKey.EditImplementedAvatar, ContextMenuIconType.Edit, itemId, addSeparator: true),
            new ContextMenuAction(Loc.ContextMenu.Item.Edit.Default, ActionKey.None, ContextMenuIconType.Edit, itemId, addSeparator: true)
            {
                SubMenuItems =
                {
                    new ContextMenuAction(Loc.ContextMenu.Item.Edit.Default, ActionKey.EditItem, ContextMenuIconType.Edit, itemId),
                    new ContextMenuAction(Loc.ContextMenu.Item.Edit.Title, ActionKey.EditItemTitle, ContextMenuIconType.Edit, itemId),
                    new ContextMenuAction(Loc.ContextMenu.Item.Edit.DefaultPath, ActionKey.EditItemDefaultPath, ContextMenuIconType.Edit, itemId, addSeparator: true),
                    new ContextMenuAction(Loc.ContextMenu.Item.Thumbnail.Change, ActionKey.ChangeThumbnail, ContextMenuIconType.Edit, itemId),
                    new ContextMenuAction(Loc.ContextMenu.Item.Thumbnail.Fetch, ActionKey.FetchThumbnail, ContextMenuIconType.Fetch, itemId)
                }
            },

            new ContextMenuAction(Loc.ContextMenu.Item.Remove, ActionKey.RemoveItem, ContextMenuIconType.Delete, itemId)
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromFolder(string path)
    {
        List<ContextMenuAction> contextMenuActions = [];

        if (ProcessUtils.IsWindows() && path.StartsWith(ItemNavigationService.FolderPrefix))
        {
            contextMenuActions.Add(new ContextMenuAction(Loc.ContextMenu.ItemFile.OpenFileInExplorer, ActionKey.OpenFileInExplorer, ContextMenuIconType.Open, path));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemFile(string path)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(Loc.ContextMenu.ItemFile.OpenFile, ActionKey.OpenFile, ContextMenuIconType.Open, path),
            new ContextMenuAction(Loc.ContextMenu.ItemFile.BulkImportList, ActionKey.AddFileToBulkImportList, ContextMenuIconType.Add, path)
        ];

        if (ProcessUtils.IsWindows())
        {
            contextMenuActions.Add(new ContextMenuAction(Loc.ContextMenu.ItemFile.OpenFileInExplorer, ActionKey.OpenFileInExplorer, ContextMenuIconType.Open, path));
        }

        if (PathUtils.IsUnitypackageFile(path))
        {
            contextMenuActions.Add(new ContextMenuAction(Loc.ContextMenu.ItemFile.OpenUnitypackageViewer, ActionKey.OpenUnitypackageViewer, ContextMenuIconType.Open, path));
        }

        if (PathUtils.IsPdfFile(path))
        {
            contextMenuActions.Add(new ContextMenuAction(Loc.ContextMenu.ItemFile.OpenPdfViewer, ActionKey.OpenPdfViewer, ContextMenuIconType.Open, path));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromBulkImportPreset(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(Loc.ContextMenu.BulkImportPreset.RemovePreset, ActionKey.RemovePreset, ContextMenuIconType.Delete, itemId),
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromTempAvatar(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(Loc.ContextMenu.TempAvatar.EditTempAvatarName, ActionKey.EditTempAvatarName, ContextMenuIconType.Edit, itemId),
            new ContextMenuAction(Loc.ContextMenu.TempAvatar.ResolveTempAvatar, ActionKey.ResolveTempAvatar, ContextMenuIconType.Link, itemId, addSeparator: true),
            new ContextMenuAction(Loc.ContextMenu.TempAvatar.RemoveTempAvatar, ActionKey.RemoveTempAvatar, ContextMenuIconType.Delete, itemId),
        ];

        return contextMenuActions.ToArray();
    }
    
    private static ContextMenuAction[] CreateFromItemCategory(string category)
    {
        List<ContextMenuAction> contextMenuActions = new();

        if (category.StartsWith("type:")) // type: => Enum
        {
            var raw = category["type:".Length..];
            if (Enum.TryParse<ItemType>(raw, out var itemType) && itemType != ItemType.All)
            {
                contextMenuActions.Add(new ContextMenuAction(Loc.ContextMenu.ItemCategory.MergeWithOtherCategory, ActionKey.MergeWithOtherCategory, ContextMenuIconType.Merge, category));
            }
        }
        else // custom: => CustomCategory
        {
            contextMenuActions.Add(new ContextMenuAction(Loc.ContextMenu.ItemCategory.MergeWithOtherCategory, ActionKey.MergeWithOtherCategory, ContextMenuIconType.Merge, category));
        }

        return contextMenuActions.ToArray();
    }
}
