using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void EditMemoOverlay_Show(string initialMemo = "")
    {
        EditMemoOverlay.IsVisible = true;
        if (!string.IsNullOrEmpty(initialMemo)) EditMemoOverlay_MemoTextBox.Text = initialMemo;
    }
    private void EditMemoOverlay_Hide() => EditMemoOverlay.IsVisible = false;

    #region Event Handler
    private void EditMemoOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditMemoOverlay_Hide();
    private void EditMemoOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Item? item = _avatarExplorerApp.GetItemById(_contextMenu_selectedItemId);
        if (item == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        item.ItemMemo = EditMemoOverlay_MemoTextBox.Text ?? string.Empty;
        _avatarExplorerApp.UpdateSearchIndex(item.Id);
        _avatarExplorerApp.SaveItemDatabase();

        EditMemoOverlay_Hide();
        Main_ReloadCurrentWindow();
    }
    #endregion
}
