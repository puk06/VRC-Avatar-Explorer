using System.Collections.Generic;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Models.ContextMenu;

namespace AvatarExplorer.UI.Services.ViewControl;

internal static class ContextMenuCreator
{
    internal static ContextMenuAction[] Create(ISelectableItem selectableItem)
    {
        if (selectableItem is Item item) return CreateFromItem(item);
        if (selectableItem is ItemFile itemFile) return CreateFromItemFile(itemFile);
        if (selectableItem is BulkImportPreset bulkImportPreset) return CreateFromBulkImportPreset(bulkImportPreset);
        
        return [];
    }

    private static ContextMenuAction[] CreateFromItem(Item item)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.OpenFolder, ActionKey.OpenItemFolder, ContextMenuIconType.Open, item.Id),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.ShowOtherItemsByAuthor, ActionKey.ShowOtherItemsByAuthor, ContextMenuIconType.Open, item.Id, addSeparator: true),

            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.BulkImportList, ActionKey.AddToBulkImportList, ContextMenuIconType.Add, item.Id),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.File, ActionKey.AddItemFile, ContextMenuIconType.Add, item.Id),
            new ContextMenuAction(LocalizationKey.ContextMenu.Item.Add.Folder, ActionKey.AddItemFolder, ContextMenuIconType.Add, item.Id, addSeparator: true),
        ];

        if (item.BoothId != -1)
        {
            contextMenuActions.AddRange(
                [
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Booth.Open, ActionKey.OpenBoothLink, ContextMenuIconType.Open, item.Id),
                    new ContextMenuAction(LocalizationKey.ContextMenu.Item.Booth.Copy, ActionKey.CopyBoothLink, ContextMenuIconType.Copy, item.Id, addSeparator: true)
                ]
            );
        }

        contextMenuActions.AddRange(
            [
                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Default, ActionKey.EditItem, ContextMenuIconType.Edit, item.Id),
                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Title, ActionKey.EditItemTitle, ContextMenuIconType.Edit, item.Id),
                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Tag, ActionKey.EditItemTag, ContextMenuIconType.Edit, item.Id),
                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Memo, ActionKey.EditItemMemo, ContextMenuIconType.Edit, item.Id),
                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Edit.Implemented, ActionKey.EditImplementedAvatar, ContextMenuIconType.Edit, item.Id, addSeparator: true),

                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Thumbnail.Change, ActionKey.ChangeThumbnail, ContextMenuIconType.Edit, item.Id),
                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Thumbnail.Fetch, ActionKey.FetchThumbnail, ContextMenuIconType.Fetch, item.Id, addSeparator: true),

                new ContextMenuAction(LocalizationKey.ContextMenu.Item.Remove, ActionKey.RemoveItem, ContextMenuIconType.Delete, item.Id)
            ]
        );

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromItemFile(ItemFile itemFile)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenFile, ActionKey.OpenFile, ContextMenuIconType.Open, itemFile.FullPath),
            new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.BulkImportList, ActionKey.AddFileToBulkImportList, ContextMenuIconType.Add, itemFile.FullPath)
        ];

        if (ProcessUtils.IsWindows())
        {
            contextMenuActions.Add(new ContextMenuAction(LocalizationKey.ContextMenu.ItemFile.OpenFileInExplorer, ActionKey.OpenFileInExplorer, ContextMenuIconType.Open, itemFile.FullPath));
        }

        return contextMenuActions.ToArray();
    }

    private static ContextMenuAction[] CreateFromBulkImportPreset(BulkImportPreset bulkImportPreset)
    {
        List<ContextMenuAction> contextMenuActions =
        [
            new ContextMenuAction(LocalizationKey.ContextMenu.BulkImportPreset.RemovePreset, ActionKey.RemovePreset, ContextMenuIconType.Delete, bulkImportPreset.Id),
        ];

        return contextMenuActions.ToArray();
    }
}
