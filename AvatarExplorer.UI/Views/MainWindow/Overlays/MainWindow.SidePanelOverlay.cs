using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void SidePanel_Show()
    {
        if (Main_SidePanelBorder.IsVisible) return;

        Main_SidePanelBorder.IsVisible = true;
        Main_PanelGrid.ColumnDefinitions[4].MinWidth = 330 + 12;
    }
    private void SidePanel_Hide()
    {
        if (!Main_SidePanelBorder.IsVisible) return;

        Main_SidePanelBorder.IsVisible = false;
        Main_PanelGrid.ColumnDefinitions[4].MinWidth = 50;
        Main_PanelGrid.ColumnDefinitions[4].Width = new(Main_PanelGrid.ColumnDefinitions[4].MinWidth);
    }

    #region Event Handler
    private void SidePanel_Show_Click(object? sender, PointerPressedEventArgs e) => SidePanel_Show();

    private void SidePanel_Item_Click(object? sender, RoutedEventArgs e)
    {
        SidePanel_Show();

        if (sender is not Button iconButton || !int.TryParse(iconButton.Tag?.ToString(), out int itemIndex)) return;
        SidePanel_TabControl.SelectedIndex = itemIndex;
    }

    private void SidePanel_TabItem_DoubleTapped(object? sender, RoutedEventArgs e) => SidePanel_Hide(); // ダブルタップでサイドパネルを閉じる
    #endregion
}
