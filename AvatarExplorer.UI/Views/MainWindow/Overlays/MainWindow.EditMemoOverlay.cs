using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void EditMemoOverlay_Open(string memo = "")
    {
        EditMemoOverlay.IsVisible = true;
        EditMemoOverlay_MemoTextBox.Text = memo;
    }
    private void EditMemoOverlay_Close() => EditMemoOverlay.IsVisible = false;

    #region Event Handler
    private void EditMemoOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditMemoOverlay_Close();
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

        EditMemoOverlay_Close();
        Main_ReloadCurrentWindow();
    }
    #endregion
}
