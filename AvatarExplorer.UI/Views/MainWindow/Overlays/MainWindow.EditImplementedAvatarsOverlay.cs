using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private readonly List<string> _editImplementedAvatarsOverlay_selectedAvatars = new();

    private void EditImplementedAvatarsOverlay_Open(IReadOnlyList<string>? avatars = null)
    {
        EditImplementedAvatarsOverlay.IsVisible = true;
        EditImplementedAvatarsOverlay_Initialize(avatars);
    }
    private void EditImplementedAvatarsOverlay_Close() => EditImplementedAvatarsOverlay.IsVisible = false;

    private void EditImplementedAvatarsOverlay_Initialize(IReadOnlyList<string>? avatars = null)
    {
        _editImplementedAvatarsOverlay_selectedAvatars.Clear();
        if (avatars != null) _editImplementedAvatarsOverlay_selectedAvatars.AddRange(avatars);
        EditImplementedAvatarsOverlay_DrawItemButtons();
    }
    private void EditImplementedAvatarsOverlay_DrawItemButtons()
    {
        EditImplementedAvatarsOverlay_AvatarsList.Children.Clear();
        IEnumerable<ItemCountInfo> avatars = _avatarExplorerApp.GetAvatars().Where(i => string.IsNullOrEmpty(EditImplementedAvatarsOverlay_SearchTextBox.Text) || ((Item)i.Item).Title.Contains(EditImplementedAvatarsOverlay_SearchTextBox.Text));

        foreach (ItemCountInfo itemCountInfo in avatars)
        {
            Button button = ItemButtonFactory.AddItemButton(EditImplementedAvatarsOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, onClick: EditImplementedAvatarsOverlay_ItemButton_Click);
            if (_editImplementedAvatarsOverlay_selectedAvatars.Contains(((Item)itemCountInfo.Item).Id)) button.Classes.Add("accent");
        }
    }

    #region Event Handler
    private void EditImplementedAvatarsOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditImplementedAvatarsOverlay_Close();
    private void EditImplementedAvatarsOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Item? item = _avatarExplorerApp.GetItemById(_contextMenu_selectedItemId);
        if (item == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        item.UpdateImplementedAvatars(_editImplementedAvatarsOverlay_selectedAvatars);
        _avatarExplorerApp.SaveItemDatabase();

        EditImplementedAvatarsOverlay_Close();
    }
    private void EditImplementedAvatarsOverlay_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ItemTagInfo itemTagInfo) return;

        if (_editImplementedAvatarsOverlay_selectedAvatars.Contains(itemTagInfo.Value)) _editImplementedAvatarsOverlay_selectedAvatars.RemoveAll(i => i == itemTagInfo.Value);
        else _editImplementedAvatarsOverlay_selectedAvatars.Add(itemTagInfo.Value);

        EditImplementedAvatarsOverlay_DrawItemButtons();
    }
    private void EditImplementedAvatarsOverlay_SearchTextBox_TextChanged(object? sender, RoutedEventArgs e) => EditImplementedAvatarsOverlay_DrawItemButtons();
    #endregion
}
