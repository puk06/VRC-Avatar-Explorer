using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void SidePanel_Show_Click(object? sender, PointerPressedEventArgs e)
    {
        SidePanel_Show();
    }

    private void SidePanel_Show_Click(object? sender, RoutedEventArgs e)
    {
        SidePanel_Show();

        if (sender is not Button iconButton || !int.TryParse(iconButton.Tag?.ToString(), out int itemIndex)) return;
        SidePanel_TabControl.SelectedIndex = itemIndex;
    }

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

    private void SidePanel_Tab_Click(object? sender, PointerPressedEventArgs e)
    {
        // TabItem
        if (sender is not TabItem tab) return;

        int currentSelectedIndex = SidePanel_TabControl.SelectedIndex;
        int index = SidePanel_TabControl.Items.IndexOf(tab);

        // 既に表示されていた場合は閉じる
        if (currentSelectedIndex == index) SidePanel_Hide();
    }
}
