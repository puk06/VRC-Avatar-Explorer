using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    internal Dictionary<ActionKey, Func<string, Task>>? _main_contextMenuHandlers;

    private async void Main_ItemButton_ContextMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menuItem && menuItem.Tag is ContextMenuAction contextMenuAction)
            await Main_ItemButton_ExecuteContextMenuItemCommand(contextMenuAction);
    }
    private async Task Main_ItemButton_ExecuteContextMenuItemCommand(ContextMenuAction contextMenuAction)
    {
        if (_main_contextMenuHandlers!.TryGetValue(contextMenuAction.ActionKey, out var handler))
            await handler(contextMenuAction.Tag);
    }

    #region Context Menu Commands
    private Item? Main_ItemButton_ContextMenu_GetItemById(string itemId)
    {
        Item? item = AvatarExplorer.GetItemById(itemId);
        if (item == null) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);

        return item;
    }
    private async Task Main_ItemButton_ContextMenu_OpenItemFolder(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        await LauncherService.OpenFolder(this, ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath));
    }
    private async Task Main_ItemButton_ContextMenu_CopyBoothLink(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        await ClipboardService.SetText(item.GetBoothLink(Localizer.Instance[LocalizationKey.BoothLanguageCode]));
    }
    private async Task Main_ItemButton_ContextMenu_OpenBoothLink(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        await LauncherService.OpenUri(this, item.GetBoothLink(Localizer.Instance[LocalizationKey.BoothLanguageCode]));
    }
    private Task Main_ItemButton_ContextMenu_ShowOtherItemsByAuthor(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        if (Main_SearchTextBox != null) Main_SearchTextBox.Text = string.Format("Author=\"{0}\"", item.Author);

        return Task.CompletedTask;
    }
    private async Task Main_ItemButton_ContextMenu_ChangeThumbnail(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFilePath], false);
        if (files == null || files.Length == 0) return;

        string selectedFile = files[0];

        ErrorOr<Success> result = await AvatarExplorer.UpdateItemThumbnail(item.Id, selectedFile);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to edit item thumbnail.", tag: result.Errors.ToErrorString());
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemThumbnailEditFailed]);
        }
        else
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemThumbnailEdit]);
            Main_ReloadCurrentWindow();
        }
    }
    private async Task Main_ItemButton_ContextMenu_FetchThumbnail(string itemId)
    {
        ErrorOr<Success> result = await AvatarExplorer.FetchAndUpdateThumbnailImage(itemId);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch item thumbnail.", tag: result.Errors.ToErrorString());
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.FetchItemThumbnailFailed]);
        }
        else
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.FetchItemThumbnail]);
            Main_ReloadCurrentWindow();
        }
    }
    private Task Main_ItemButton_ContextMenu_EditItem(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        AddItemOverlay_Open(item);

        return Task.CompletedTask;
    }
    private async Task Main_ItemButton_ContextMenu_EditItemTitle(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string? newTitle = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewItemTitle], item.Title);
        if (string.IsNullOrEmpty(newTitle)) return;

        item.Title = newTitle;
        AvatarExplorer.UpdateItemUpdatedDate(item.Id);

        AvatarExplorer.SaveItemDatabase();
        AvatarExplorer.UpdateSearchIndex();

        Main_ReloadCurrentWindow();
    }
    private async Task Main_ItemButton_ContextMenu_EditMemo(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string? memo = await EditMemoOverlay_ShowSafeAsync(item.ItemMemo);
        if (memo == null) return;

        item.ItemMemo = memo;
        AvatarExplorer.UpdateItemUpdatedDate(item.Id);

        AvatarExplorer.UpdateSearchIndex(item.Id);
        AvatarExplorer.SaveItemDatabase();

        Main_ReloadCurrentWindow();
    }
    private Task Main_ItemButton_ContextMenu_AddToBulkImportList(string itemId)
    {
        BulkImportPanel_AddItem(itemId);
        return Task.CompletedTask;
    }
    private async Task Main_ItemButton_ContextMenu_AddItemFile(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFilePath], true);
        if (files == null || files.Length == 0) return;

        ErrorOr<ExtractResult> result = await Main_ItemButton_ContextMenu_AddItemPathsInternal(item, files);

        if (result.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.AddItemFileFailed]);
        else if (result.Value.ProcessingFailedPaths.Count > 0) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()));
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemFileAdd]);
    }
    private async Task Main_ItemButton_ContextMenu_AddItemFolder(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], true);
        if (folders == null || folders.Length == 0) return;

        ErrorOr<ExtractResult> result = await Main_ItemButton_ContextMenu_AddItemPathsInternal(item, folders);

        if (result.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.AddItemFolderFailed]);
        else if (result.Value.ProcessingFailedPaths.Count > 0) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance.Get(LocalizationKey.Error.FoundProcessingFailedPath, result.Value.ProcessingFailedPaths.Count.ToString()));
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.ItemFolderAdd]);
    }
    private async Task<ErrorOr<ExtractResult>> Main_ItemButton_ContextMenu_AddItemPathsInternal(Item item, string[] itemPaths)
    {
        ProgressOverlay_Show(Localizer.Instance[LocalizationKey.Processing.ItemAdd.Copying], 0);
        ErrorOr<ExtractResult> extractResult = await AvatarExplorer.AddItemPaths(item.Id, itemPaths);
        ProgressOverlay_Hide();

        return extractResult;
    }

    private Task Main_ItemButton_ContextMenu_EditImplementedAvatar(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return Task.CompletedTask;

        EditImplementedAvatarsOverlay_Open(item.Id, item.ImplementedAvatarsView);

        return Task.CompletedTask;
    }
    private async Task Main_ItemButton_ContextMenu_EditItemTag(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        string[]? tags = await EditTagsOverlay_ShowAsyncSafe(item.TagsView);
        if (tags == null) return;

        item.UpdateTags(tags);
        AvatarExplorer.UpdateItemUpdatedDate(item.Id);

        AvatarExplorer.UpdateSearchIndex(item.Id);
        AvatarExplorer.SaveItemDatabase();

        Main_ReloadCurrentWindow();
    }
    private async Task Main_ItemButton_ContextMenu_RemoveItem(string itemId)
    {
        Item? item = Main_ItemButton_ContextMenu_GetItemById(itemId);
        if (item == null) return;

        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveItem, item.Title));
        if (result == null || result != YesNoResult.Yes) return;

        YesNoResult? result1 = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveAssetData, ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath)));
        if (result1 == null) return;

        bool removeAssetData = result1 == YesNoResult.Yes;
        bool removed = AvatarExplorer.RemoveItem(item.Id, removeAssetData);

        Main_ReloadCurrentWindow();

        if (removed) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.Remove]);
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RemoveFailed]);
    }

    private async Task Main_ItemButton_ContextMenu_OpenFile(string filePath)
    {
        ErrorOr<Success> result = await LauncherService.OpenFile(this, filePath);
        if (result.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed]);
    }
    private Task Main_ItemButton_ContextMenu_OpenFileInExplorer(string filePath)
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
    private Task Main_ItemButton_ContextMenu_AddFileToBulkImportList(string filePath)
    {
        string? itemId = AvatarExplorer.GetSelectedItem()?.Id;
        if (itemId == null) return Task.CompletedTask;

        BulkImportPanel_AddItem(itemId, filePath);
        
        return Task.CompletedTask;
    }
    private async Task Main_ItemButton_ContextMenu_OpenUnitypackageViewer(string filePath)
    {
        await UnitypackageViewerOverlay_OpenAsync(filePath);
    }

    private async Task Main_ItemButton_ContextMenu_OpenPdfViewer(string filePath)
    {
        await PdfViewerOverlay_OpenAsync(filePath);
    }

    private BulkImportPreset? Main_ItemButton_ContextMenu_GetBulkImportPresetById(string id)
    {
        BulkImportPreset? bulkImportPreset = AvatarExplorer.GetBulkImportPresetById(id);
        if (bulkImportPreset == null) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.PresetNotFound]);

        return bulkImportPreset;
    }

    private async Task Main_ItemButton_ContextMenu_RemovePreset(string id)
    {
        BulkImportPreset? bulkImportPreset = Main_ItemButton_ContextMenu_GetBulkImportPresetById(id);
        if (bulkImportPreset == null) return;

        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemovePreset, bulkImportPreset.PresetName));
        if (result == null || result != YesNoResult.Yes) return;

        bool removed = AvatarExplorer.RemoveBulkImportPreset(bulkImportPreset.Id);

        BulkImportPresetPanel_DrawItemButtons();

        if (removed) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.Remove]);
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RemoveFailed]);
    }

    private TempAvatar? Main_ItemButton_ContextMenu_GetTempAvatarById(string id)
    {
        TempAvatar? tempAvatar = AvatarExplorer.GetTempAvatarById(TempAvatar.GetAvatarId(id));
        if (tempAvatar == null) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.TempAvatarNotFound]);

        return tempAvatar;
    }

    private async Task Main_ItemButton_ContextMenu_ResolveTempAvatar(string id) => ResolveTempAvatarOverlay_Open(id);
    private async Task Main_ItemButton_ContextMenu_EditTempAvatarName(string id)
    {
        TempAvatar? tempAvatar = Main_ItemButton_ContextMenu_GetTempAvatarById(id);
        if (tempAvatar == null) return;

        string? newAvatarName = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewTempAvatarName], tempAvatar.AvatarName);
        if (string.IsNullOrEmpty(newAvatarName)) return;

        tempAvatar.AvatarName = newAvatarName;

        AvatarExplorer.SaveTempAvatarsDatabase();
        AvatarExplorer.UpdateSearchIndex();

        Main_ReloadCurrentWindow();
    }
    private async Task Main_ItemButton_ContextMenu_RemoveTempAvatar(string id)
    {
        TempAvatar? tempAvatar = Main_ItemButton_ContextMenu_GetTempAvatarById(id);
        if (tempAvatar == null) return;

        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveTempAvatar, tempAvatar.AvatarName));
        if (result == null || result != YesNoResult.Yes) return;

        bool removed = AvatarExplorer.RemoveTempAvatar(tempAvatar.GetInternalId());

        Main_ReloadCurrentWindow();

        if (removed) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.Remove]);
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.RemoveFailed]);
    }

    private async Task Main_ItemButton_ContextMenu_EditCustomCategoryName(string customCategory)
    {
        string? newCategoryName = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewCustomCategoryName], customCategory);
        if (string.IsNullOrEmpty(newCategoryName)) return;

        if (AvatarExplorer.GetCategories().Any(i => i.Item is ItemCategory itemCategory && itemCategory.Type == ItemType.Custom && itemCategory.CustomCategory == newCategoryName))
        {
            YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.DuplicateCustomCategoryName]);
            if (result == null || result != YesNoResult.Yes) return;
        }

        AvatarExplorer.EditCustomCategoryName(customCategory, newCategoryName);

        Main_ReloadCurrentWindow();
    }

    private async Task Main_ItemButton_ContextMenu_MergeWithOtherCategory(string categoryName)
    {
        if (!ItemCategory.TryParse(categoryName, out ItemCategory? sourceCategory) || sourceCategory == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.InvalidCategory]);
            return;
        }

        ItemCategory? targetCategory = await MergeCategoryOverlay_ShowAsyncSafe();
        if (targetCategory == null) return;
        
        string sourceCategoryName = sourceCategory.Type == ItemType.Custom ? sourceCategory.CategoryName : Localizer.Instance[sourceCategory.ToString()];
        string targetCategoryName = targetCategory.Type == ItemType.Custom ? targetCategory.CategoryName : Localizer.Instance[targetCategory.ToString()];

        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.MergeCategory, [sourceCategoryName, targetCategoryName]));
        if (result == null || result != YesNoResult.Yes) return;

        AvatarExplorer.MergeItemCategories(sourceCategory, targetCategory);

        Main_ReloadCurrentWindow();
    }
    #endregion
}
