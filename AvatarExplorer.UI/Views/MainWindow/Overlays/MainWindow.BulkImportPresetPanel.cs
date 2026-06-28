using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.ViewControl;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void BulkImportPresetPanel_Focus()
    {
        SidePanel_Show();

        int bulkImportPresetPanelTabIndex = SidePanel_TabControl.Items.IndexOf(SidePanel_BulkImportPresetPanelTab);
        if (bulkImportPresetPanelTabIndex != -1 && SidePanel_TabControl.SelectedIndex != bulkImportPresetPanelTabIndex) SidePanel_TabControl.SelectedIndex = bulkImportPresetPanelTabIndex;
    }
    
    private void BulkImportPresetPanel_DrawItemButtons()
    {
        SidePanel_BulkImportPresetPanel.Children.Clear();

        foreach (var bulkImportPreset in AvatarExplorer.GetAllBulkImportPresets())
        {
            var itemContextMenu = ContextMenuFactory.GetContextMenu(ContextMenuCreator.Create(bulkImportPreset), Main_ItemButton_ContextMenuItem_Click);
            ItemButtonFactory.AddItemButton(SidePanel_BulkImportPresetPanel, new UISelectableItem(bulkImportPreset, 0), RuntimeSettings, UserPreferences, itemContextMenu, BulkImportPresetPanel_ItemButton_Click);
        }
    }

    #region Event Handler
    private async void BulkImportPresetPanel_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        if (button.Tag is ItemTagInfo itemTagInfo)
        {
            var bulkImportPreset = AvatarExplorer.GetBulkImportPresetById(itemTagInfo.Value);
            if (bulkImportPreset == null) return;

            BulkImportPanel_AddItem(bulkImportPreset.Items);
        }
    }
    #endregion
}
