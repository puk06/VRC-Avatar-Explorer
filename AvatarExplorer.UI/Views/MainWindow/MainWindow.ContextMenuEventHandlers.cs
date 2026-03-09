using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Services.Utilities;
using ErrorOr;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    internal Dictionary<ActionKey, Func<string, Task>>? _contextMenuHandlers;

    private async void ItemButton_ContextMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ContextMenuAction contextMenuAction)
            await ItemButton_ExecuteContextMenuItemCommand(contextMenuAction);
    }
    private async Task ItemButton_ExecuteContextMenuItemCommand(ContextMenuAction contextMenuAction)
    {
        if (_contextMenuHandlers!.TryGetValue(contextMenuAction.ActionKey, out var handler))
            await handler(contextMenuAction.Tag);
    }

    #region Context Menu Commands
    private Item? ItemButton_ContextMenu_GetItemById(string itemId)
    {
        Item? item = _avatarExplorerApp.GetItemById(itemId);
        if (item == null) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);

        return item;
    }
    private async Task ItemButton_ContextMenu_OpenItemFolder(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        await LauncherService.OpenFolder(this, ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath));
    }
    private async Task ItemButton_ContextMenu_CopyBoothLink(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        await ClipboardService.SetText(item.GetBoothLink());
    }
    private async Task ItemButton_ContextMenu_OpenBoothLink(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        await LauncherService.OpenUri(this, item.GetBoothLink());
    }
    private Task ItemButton_ContextMenu_ShowOtherItemsByAuthor(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        if (Main_SearchTextBox != null) Main_SearchTextBox.Text = string.Format("Author=\"{0}\"", item.Author);

        return Task.CompletedTask;
    }
    private async Task ItemButton_ContextMenu_ChangeThumbnail(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFilePath], false);
        if (files == null || files.Length == 0) return;

        string selectedFile = files[0];

        ErrorOr<Success> result = await _avatarExplorerApp.UpdateItemThumbnail(item.Id, selectedFile);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to edit item thumbnail.", tag: result.Errors.ToErrorString());
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemThumbnailEditFailed]);
        }
        else
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemThumbnailEdit]);
            Main_ReloadCurrentWindow();
        }
    }
    private async Task ItemButton_ContextMenu_FetchThumbnail(string itemId)
    {
        ErrorOr<Success> result = await _avatarExplorerApp.FetchAndUpdateThumbnailImage(itemId);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch item thumbnail.", tag: result.Errors.ToErrorString());
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.FetchItemThumbnailFailed]);
        }
        else
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.FetchItemThumbnail]);
            Main_ReloadCurrentWindow();
        }
    }
    private Task ItemButton_ContextMenu_EditItem(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        AddItemOverlay_Show(item);
        return Task.CompletedTask;
    }
    private async Task ItemButton_ContextMenu_EditItemTitle(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string? newTitle = await ShowTextDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewItemTitle], item.Title);
        if (string.IsNullOrEmpty(newTitle)) return;
        item.Title = newTitle;
        
        _avatarExplorerApp.SaveItemDatabase();
        _avatarExplorerApp.UpdateSearchIndex();

        Main_ReloadCurrentWindow();
    }
    private Task ItemButton_ContextMenu_EditMemo(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        _contextMenu_selectedItemId = item.Id;

        EditMemoOverlay_Show(item.ItemMemo);

        return Task.CompletedTask;
    }
    private Task ItemButton_ContextMenu_AddToBulkImportList(string itemId)
    {
        BulkImportItem_Add(itemId);
        return Task.CompletedTask;
    }
    private async Task ItemButton_ContextMenu_AddItemFile(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFilePath], true);
        if (files == null || files.Length == 0) return;

        ErrorOr<ExtractResult> result = await ItemButton_ContextMenu_AddItemPathsInternal(item, files);

        if (result.IsError) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.AddItemFileFailed]);
        else if (result.Value.ProcessingFailedPaths.Count > 0) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()));
        else Dialog_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemFileAdd]);
    }
    private async Task ItemButton_ContextMenu_AddItemFolder(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], true);
        if (folders == null || folders.Length == 0) return;

        ErrorOr<ExtractResult> result = await ItemButton_ContextMenu_AddItemPathsInternal(item, folders);

        if (result.IsError) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.AddItemFolderFailed]);
        else if (result.Value.ProcessingFailedPaths.Count > 0) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()));
        else Dialog_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemFolderAdd]);
    }
    private async Task<ErrorOr<ExtractResult>> ItemButton_ContextMenu_AddItemPathsInternal(Item item, string[] itemPaths)
    {
        ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
        ErrorOr<ExtractResult> extractResult = await _avatarExplorerApp.AddItemPaths(item.Id, itemPaths);
        ProgressOverlay_Hide();

        return extractResult;
    }

    internal string? _contextMenu_selectedItemId = null;
    private Task ItemButton_ContextMenu_EditImplementedAvatar(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        _contextMenu_selectedItemId = item.Id;

        EditImplementedAvatarsOverlay_Show(item.ImplementedAvatarsView);

        return Task.CompletedTask;
    }
    private Task ItemButton_ContextMenu_EditItemTag(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        _contextMenu_selectedItemId = item.Id;

        EditTagsOverlay_Show(item.TagsView);

        return Task.CompletedTask;
    }
    private async Task ItemButton_ContextMenu_RemoveItem(string itemId)
    {
        Item? item = ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        YesNoResult? result = await ShowYesNoDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveItem, item.Title));
        if (result == null || result != YesNoResult.Yes) return;

        YesNoResult? result2 = await ShowYesNoDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveItem, item.Title));
        if (result2 == null) return;

        bool removeItemFromSupportedAndImplemented = result2 == YesNoResult.Yes;
        bool removed = _avatarExplorerApp.RemoveItem(item.Id, removeItemFromSupportedAndImplemented);

        Main_ReloadCurrentWindow();

        if (removed) Dialog_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.Remove]);
        else Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RemoveFailed]);
    }

    private async Task ItemButton_ContextMenu_OpenFile(string filePath)
    {
        ErrorOr<Success> result = await LauncherService.OpenFile(this, filePath);
        if (result.IsError) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed]);
    }
    private Task ItemButton_ContextMenu_OpenFileInExplorer(string filePath)
    {
        if (!ProcessUtils.IsWindows()) return Task.CompletedTask;

        try
        {
            Process.Start("explorer.exe", "/select," + filePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError(string.Format("Failed to open file in explorer. '{0}'", filePath), ex);
        }

        return Task.CompletedTask;
    }
    private Task ItemButton_ContextMenu_AddFileToBulkImportList(string filePath)
    {
        string? itemId = _avatarExplorerApp.GetSelectedItem()?.Id;
        if (itemId == null) return Task.CompletedTask;

        BulkImportItem_Add(itemId, filePath);
        return Task.CompletedTask;
    }
    #endregion
}
