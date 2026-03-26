using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private string? _editMemoOverlay_selectedItemId = null;

    private void EditMemoOverlay_Open(string itemId, string memo = "")
    {
        _editMemoOverlay_selectedItemId = itemId;
        EditMemoOverlay_MemoTextBox.Text = memo;
        EditMemoOverlay.IsVisible = true;
    }
    private void EditMemoOverlay_Close()
    {
        EditMemoOverlay.IsVisible = false;
        _editMemoOverlay_selectedItemId = null;
        EditMemoOverlay_MemoTextBox.Text = string.Empty;
    }

    #region Event Handler
    private void EditMemoOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditMemoOverlay_Close();
    private void EditMemoOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Item? item = AvatarExplorer.GetItemById(_editMemoOverlay_selectedItemId);
        if (item == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        item.ItemMemo = EditMemoOverlay_MemoTextBox.Text ?? string.Empty;
        AvatarExplorer.UpdateItemUpdatedDate(item.Id);

        AvatarExplorer.UpdateSearchIndex(item.Id);
        AvatarExplorer.SaveItemDatabase();

        EditMemoOverlay_Close();
        Main_ReloadCurrentWindow();
    }
    #endregion
}
