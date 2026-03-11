using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.ViewControl;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void ReloadBulkImportItemPresetButtons()
    {
        SidePanel_BulkImportPresetPanel.Children.Clear();

        foreach (BulkImportPreset bulkImportPreset in _avatarExplorerApp.GetAllBulkImportPresets())
        {
            ContextMenu itemContextMenu = ContextMenuFactory.GetContextMenu(ContextMenuCreator.Create(bulkImportPreset), ItemButton_ContextMenuItem_Click);
            ItemButtonFactory.AddItemButton(SidePanel_BulkImportPresetPanel, new UISelectableItem(bulkImportPreset, 0), RuntimeSettings, _userPreferences, itemContextMenu, BulkImportPreset_Button_Click);
        }
    }

    private void BulkImportPreset_Focus()
    {
        SidePanel_Show();

        int bulkImportPresetPanelTabIndex = SidePanel_TabControl.Items.IndexOf(SidePanel_BulkImportPresetPanelTab);
        if (bulkImportPresetPanelTabIndex != -1 && SidePanel_TabControl.SelectedIndex != bulkImportPresetPanelTabIndex) SidePanel_TabControl.SelectedIndex = bulkImportPresetPanelTabIndex;
    }

    private async void BulkImportPreset_Button_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        if (button.Tag is ItemTagInfo itemTagInfo)
        {
            BulkImportPreset? bulkImportPreset = _avatarExplorerApp.GetBulkImportPresetById(itemTagInfo.Value);
            if (bulkImportPreset == null) return;

            BulkImportItem_Add(bulkImportPreset.ItemsView);
        }
    }
}
