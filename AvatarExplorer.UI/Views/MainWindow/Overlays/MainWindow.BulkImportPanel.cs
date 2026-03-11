using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Utils;
using ErrorOr;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private readonly List<BulkImportItem> _bulkImportPanel_bulkImportItems = new();

    private void BulkImportItem_Add(string itemId, string? filePath = null)
    {
        BulkImportItem_Focus();

        BulkImportItem bulkImportItem = new BulkImportItem(itemId);
        if (filePath != null) bulkImportItem.FilePath = filePath;
        
        _bulkImportPanel_bulkImportItems.Add(bulkImportItem);
        ReloadBulkImportItemButtons();

        SidePanel_BulkImportPanelScrollViewer.Offset = AvaloniaVectorUtils.MaxValue;
    }

    private void BulkImportItem_Add(IEnumerable<BulkImportItem> bulkImportItems)
    {
        BulkImportItem_Focus();
        
        _bulkImportPanel_bulkImportItems.AddRange(bulkImportItems);
        ReloadBulkImportItemButtons();

        SidePanel_BulkImportPanelScrollViewer.Offset = AvaloniaVectorUtils.MaxValue;
    }

    private void BulkImportItem_Focus()
    {
        SidePanel_Show();

        int bulkImportPanelTabIndex = SidePanel_TabControl.Items.IndexOf(SidePanel_BulkImportPanelTab);
        if (bulkImportPanelTabIndex != -1 && SidePanel_TabControl.SelectedIndex != bulkImportPanelTabIndex) SidePanel_TabControl.SelectedIndex = bulkImportPanelTabIndex;
    }

    private void BulkImportItemButton_Copy_Click(string id)
    {
        BulkImportItem? item = _bulkImportPanel_bulkImportItems.FirstOrDefault(i => i.Id == id);
        if (item == null) return;

        BulkImportItem_Add(item.ItemId);
    }

    private void BulkImportItemButton_Remove_Click(string id)
    {
        _bulkImportPanel_bulkImportItems.RemoveAll(i => i.Id == id);
        ReloadBulkImportItemButtons();
    }

    private void BulkImportItemButton_SelectionChanged(string id, string filePath)
    {
        BulkImportItem? item = _bulkImportPanel_bulkImportItems.FirstOrDefault(i => i.Id == id);
        if (item != null) item.FilePath = filePath;
    }

    private async void BulkImportPanel_Import_Click(object? sender, RoutedEventArgs e)
    {
        Dictionary<string, string> itemPathCategoryDictionary = new();

        foreach (BulkImportItem bulkImportItem in _bulkImportPanel_bulkImportItems)
        {
            Item? item = _avatarExplorerApp.GetItemById(bulkImportItem.ItemId);
            if (item == null) continue;

            if (!itemPathCategoryDictionary.ContainsKey(bulkImportItem.FilePath))
            {
                string categoryName = item.Type == ItemType.Custom ? item.CustomCategory : Localizer.Instance[item.Type.GetLocalizationKey() ?? item.Type.ToString()];
                itemPathCategoryDictionary[bulkImportItem.FilePath] = categoryName;
            }
        }

        ModifiedUnitypackagesResult importResult = await UnitypackageService.Import(
            itemPathCategoryDictionary,
            onProgress: async (name, percent) =>
            {
                ProgressOverlay_Show(Localizer.Instance.Get(name, percent.ToString()));
                ProgressOverlay_Update(percent);
            }
        );

        ProgressOverlay_Hide();

        if (!importResult.IsError && !string.IsNullOrEmpty(importResult.ModifiedUnitypackagePath))
        {
            ErrorOr<Success> result = await LauncherService.OpenFile(this, importResult.ModifiedUnitypackagePath);
            if (result.IsError) Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed]);
        }
        else
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.BulkImportFailed]);
        }
    }

    private async void BulkImportPanel_Save_Click(object? sender, RoutedEventArgs e)
    {
        string? presetName = await ShowTextDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewBulkImportPresetName]);
        if (string.IsNullOrEmpty(presetName)) return;

        _avatarExplorerApp.AddBulkImportPreset(presetName, _bulkImportPanel_bulkImportItems);
        ReloadBulkImportItemPresetButtons();
        
        BulkImportPreset_Focus();
    }

    private void BulkImportPanel_Reset_Click(object? sender, RoutedEventArgs e)
    {
        _bulkImportPanel_bulkImportItems.Clear();
        ReloadBulkImportItemButtons();
    }

    private void ReloadBulkImportItemButtons()
    {
        SidePanel_BulkImportPanel.Children.Clear();

        foreach (BulkImportItem bulkImportItem in _bulkImportPanel_bulkImportItems)
        {
            Item? item = _avatarExplorerApp.GetItemById(bulkImportItem.ItemId);
            if (item == null) continue;

            UnitypackageSelectorButtonFactory.AddItemButton(
                SidePanel_BulkImportPanel,
                new UISelectableItem(new ItemCountInfo(item, 0)),
                RuntimeSettings, _userPreferences,
                bulkImportItem.Id, bulkImportItem.FilePath,
                BulkImportItemButton_Copy_Click, BulkImportItemButton_Remove_Click, BulkImportItemButton_SelectionChanged
            );
        }
    }

    private void BulkImportPanel_DragDrop_Drop(object? sender, DragEventArgs e)
    {
        if (!(e.DataTransfer.Contains(DataFormat.Text) || e.DataTransfer.Contains(DataFormat.File))) return;

        string? text = e.DataTransfer.TryGetText();
        if (!string.IsNullOrEmpty(text) && _avatarExplorerApp.GetItemById(text) != null)
        {
            BulkImportItem_Add(text);
            return;
        }

        string? file = e.DataTransfer.TryGetFile()?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(file))
        {
            Item? currentSelectedItem = _avatarExplorerApp.GetSelectedItem();
            if (currentSelectedItem == null) return;

            if (UnitypackageService.GetUnitypackagePaths(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, currentSelectedItem.ItemPath)).Contains(file))
            {
                BulkImportItem_Add(currentSelectedItem.Id, file);
            }
        }
    }
}
