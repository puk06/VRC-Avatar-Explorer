using System;
using System.Collections.Generic;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
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
    internal static ContextMenuAction[] Create(ViewModelType type, string value)
    {
        return type switch
        {
            ViewModelType.Item => CreateFromItem(value),
            ViewModelType.Folder => CreateFromItemFolder(value),
            ViewModelType.File => CreateFromItemFile(value),
            ViewModelType.BulkImportPreset => CreateFromBulkImportPreset(value),
            ViewModelType.TempAvatar => CreateFromTempAvatar(value),
            ViewModelType.ItemCategory => CreateFromItemCategory(value),
            _ => []
        };
    }

    private static ContextMenuAction[] CreateFromItem(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.OpenFolder, ActionKey.OpenItemFolder, ContextMenuIconType.Open, itemId),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.ShowOtherItemsByAuthor, ActionKey.ShowOtherItemsByAuthor, ContextMenuIconType.Open, itemId, addSeparator: true),

            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.BulkImportList, ActionKey.AddToBulkImportList, ContextMenuIconType.Add, itemId),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.File, ActionKey.None, ContextMenuIconType.Add, addSeparator: true)
            {
                SubMenuItems =
                {
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.File, ActionKey.AddItemFile, ContextMenuIconType.Add, itemId),
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.Folder, ActionKey.AddItemFolder, ContextMenuIconType.Add, itemId)
                }
            },
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Booth.Open, ActionKey.OpenBoothLink, ContextMenuIconType.Open, itemId),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Booth.Copy, ActionKey.CopyBoothLink, ContextMenuIconType.Copy, itemId, addSeparator: true),

            new ContextMenuAction(LocalizationKey.ContextMenu.Item.CopyItemInfo, ActionKey.CopyItemInfo, ContextMenuIconType.Copy, itemId, addSeparator: true),

            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Tag, ActionKey.EditItemTag, ContextMenuIconType.Edit, itemId),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Memo, ActionKey.EditItemMemo, ContextMenuIconType.Edit, itemId),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Implemented, ActionKey.EditImplementedAvatar, ContextMenuIconType.Edit, itemId, addSeparator: true),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Default, ActionKey.None, ContextMenuIconType.Edit, itemId, addSeparator: true)
            {
                SubMenuItems =
                {
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Default, ActionKey.EditItem, ContextMenuIconType.Edit, itemId),
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Title, ActionKey.EditItemTitle, ContextMenuIconType.Edit, itemId),
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.DefaultPath, ActionKey.EditItemDefaultPath, ContextMenuIconType.Edit, itemId, addSeparator: true),
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Thumbnail.Change, ActionKey.ChangeThumbnail, ContextMenuIconType.Edit, itemId),
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Thumbnail.Fetch, ActionKey.FetchThumbnail, ContextMenuIconType.Fetch, itemId)
                }
            },

            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Remove, ActionKey.RemoveItem, ContextMenuIconType.Delete, itemId)
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemFolder(string path)
    {
        List<ContextMenuAction> contextMenuActions = [];

        if (ProcessUtils.IsWindows())
        {
            contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenFileInExplorer, ActionKey.OpenFileInExplorer, ContextMenuIconType.Open, path));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemFile(string path)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenFile, ActionKey.OpenFile, ContextMenuIconType.Open, path),
            new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.BulkImportList, ActionKey.AddFileToBulkImportList, ContextMenuIconType.Add, path)
        ];

        if (ProcessUtils.IsWindows())
        {
            contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenFileInExplorer, ActionKey.OpenFileInExplorer, ContextMenuIconType.Open, path));
        }

        if (PathUtils.IsUnitypackageFile(path))
        {
            contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenUnitypackageViewer, ActionKey.OpenUnitypackageViewer, ContextMenuIconType.Open, path));
        }

        if (PathUtils.IsPdfFile(path))
        {
            contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenPdfViewer, ActionKey.OpenPdfViewer, ContextMenuIconType.Open, path));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromBulkImportPreset(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.BulkImportPreset.RemovePreset, ActionKey.RemovePreset, ContextMenuIconType.Delete, itemId),
        ];

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromTempAvatar(string itemId)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.TempAvatar.EditTempAvatarName, ActionKey.EditTempAvatarName, ContextMenuIconType.Edit, itemId),
            new ContextMenuAction(LocalizationKey.ContextMenu.TempAvatar.ResolveTempAvatar, ActionKey.ResolveTempAvatar, ContextMenuIconType.Link, itemId, addSeparator: true),
            new ContextMenuAction(LocalizationKey.ContextMenu.TempAvatar.RemoveTempAvatar, ActionKey.RemoveTempAvatar, ContextMenuIconType.Delete, itemId),
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
                contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemCategory.MergeWithOtherCategory, ActionKey.MergeWithOtherCategory, ContextMenuIconType.Merge, category));
            }
        }
        else // custom: => CustomCategory
        {
            contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemCategory.MergeWithOtherCategory, ActionKey.MergeWithOtherCategory, ContextMenuIconType.Merge, category));
        }

        return contextMenuActions.ToArray();
    }
}
