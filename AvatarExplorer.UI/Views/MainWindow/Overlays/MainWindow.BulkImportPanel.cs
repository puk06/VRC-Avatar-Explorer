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
    internal class BulkImportItem(string itemId)
    {
        internal string ItemId { get; init; } = itemId;
        internal int SelectedIndex { get; set; } = 0;
    }

    private readonly List<BulkImportItem> _bulkImportPanel_bulkImportItems = new();

    private void BulkImportItem_Add(string itemId, string? filePath = null)
    {
        SidePanel_Show();

        int bulkImportPanelTabIndex = SidePanel_TabControl.Items.IndexOf(SidePanel_BulkImportPanelTab);
        if (bulkImportPanelTabIndex != -1 && SidePanel_TabControl.SelectedIndex != bulkImportPanelTabIndex) SidePanel_TabControl.SelectedIndex = bulkImportPanelTabIndex;

        BulkImportItem bulkImportItem = new BulkImportItem(itemId);

        if (filePath != null)
        {
            Item? item = _avatarExplorerApp.GetItemById(bulkImportItem.ItemId);
            if (item != null)
            {
                List<string> unitypackagePaths = UnitypackageService.GetUnitypackagePaths(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath)).ToList();
                int index = unitypackagePaths.IndexOf(filePath);
                if (index != -1) bulkImportItem.SelectedIndex = index;
            }
        }
        
        _bulkImportPanel_bulkImportItems.Add(bulkImportItem);
        ReloadBulkImportItemButtons();

        SidePanel_BulkImportPanelScrollViewer.Offset = AvaloniaVectorUtils.MaxValue;
    }

    private void BulkImportItemButton_Copy_Click(int itemIndex)
    {
        BulkImportItem item = _bulkImportPanel_bulkImportItems[itemIndex];
        BulkImportItem_Add(item.ItemId);
    }

    private void BulkImportItemButton_Remove_Click(int itemIndex)
    {
        _bulkImportPanel_bulkImportItems.RemoveAt(itemIndex);
        ReloadBulkImportItemButtons();
    }

    private void BulkImportItemButton_SelectionChanged(int itemIndex, int selectedIndex)
    {
        _bulkImportPanel_bulkImportItems[itemIndex].SelectedIndex = selectedIndex;
    }

    private async void BulkImportPanel_Import_Click(object? sender, RoutedEventArgs e)
    {
        Dictionary<string, string> itemPathCategoryDictionary = new();

        foreach (BulkImportItem bulkImportItem in _bulkImportPanel_bulkImportItems)
        {
            Item? item = _avatarExplorerApp.GetItemById(bulkImportItem.ItemId);
            if (item == null) continue;

            IReadOnlyList<string> unitypackagePaths = UnitypackageService.GetUnitypackagePaths(ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath));
            if (unitypackagePaths.Count == 0 || bulkImportItem.SelectedIndex >= unitypackagePaths.Count) continue;

            string unitypackagePath = unitypackagePaths[bulkImportItem.SelectedIndex];
            if (!itemPathCategoryDictionary.ContainsKey(unitypackagePath))
            {
                string categoryName = item.Type == ItemType.Custom ? item.CustomCategory : Localizer.Instance[item.Type.GetLocalizationKey() ?? item.Type.ToString()];
                itemPathCategoryDictionary[unitypackagePath] = categoryName;
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

    private void BulkImportPanel_Reset_Click(object? sender, RoutedEventArgs e)
    {
        _bulkImportPanel_bulkImportItems.Clear();
        ReloadBulkImportItemButtons();
    }

    private void ReloadBulkImportItemButtons()
    {
        SidePanel_BulkImportPanel.Children.Clear();

        for (int i = 0; i < _bulkImportPanel_bulkImportItems.Count; i++)
        {
            BulkImportItem bulkImportItem = _bulkImportPanel_bulkImportItems[i];
            Item? item = _avatarExplorerApp.GetItemById(bulkImportItem.ItemId);
            if (item == null) continue;

            UnitypackageSelectorButtonFactory.AddItemButton(SidePanel_BulkImportPanel, new UISelectableItem(new ItemCountInfo(item, 0)), RuntimeSettings, _userPreferences, i, bulkImportItem.SelectedIndex, BulkImportItemButton_Copy_Click, BulkImportItemButton_Remove_Click, BulkImportItemButton_SelectionChanged);
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
