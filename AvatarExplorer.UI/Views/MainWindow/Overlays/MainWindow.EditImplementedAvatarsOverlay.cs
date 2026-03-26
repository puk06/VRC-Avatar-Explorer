using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private readonly List<string> _editImplementedAvatarsOverlay_selectedAvatars = new();
    private string? _editImplementedAvatarsOverlay_selectedItemId = null;

    private void EditImplementedAvatarsOverlay_Open(string itemId, IEnumerable<string>? avatars = null)
    {
        _editImplementedAvatarsOverlay_selectedItemId = itemId;
        EditImplementedAvatarsOverlay_Initialize(avatars);
        EditImplementedAvatarsOverlay.IsVisible = true;
    }
    private void EditImplementedAvatarsOverlay_Close()
    {
        EditImplementedAvatarsOverlay.IsVisible = false;
        _editImplementedAvatarsOverlay_selectedItemId = null;
        _editImplementedAvatarsOverlay_selectedAvatars.Clear();
        EditImplementedAvatarsOverlay_SearchTextBox.Text = string.Empty;
        EditImplementedAvatarsOverlay_AvatarsList.Children.Clear();
    }

    private void EditImplementedAvatarsOverlay_Initialize(IEnumerable<string>? avatars = null)
    {
        _editImplementedAvatarsOverlay_selectedAvatars.Clear();
        if (avatars != null) _editImplementedAvatarsOverlay_selectedAvatars.AddRange(avatars);
        EditImplementedAvatarsOverlay_DrawItemButtons();
    }
    private void EditImplementedAvatarsOverlay_DrawItemButtons()
    {
        if (EditImplementedAvatarsOverlay_AvatarsList == null) return;
        EditImplementedAvatarsOverlay_AvatarsList.Children.Clear();

        string searchText = EditImplementedAvatarsOverlay_SearchTextBox.Text ?? string.Empty;
        string[] parsedText = TextParser.Parse(searchText);

        IEnumerable<ItemCountInfo> avatars = AvatarExplorer.GetAvatars()
            .Where(i =>
                string.IsNullOrEmpty(searchText) ||
                (i.Item is Item item && EditImplementedAvatarsOverlay_IsMatch(AvatarExplorer.GetSearchIndexByItemId(item.Id), parsedText))
            );

        foreach (ItemCountInfo itemCountInfo in avatars)
        {
            Button button = ItemButtonFactory.AddItemButton(EditImplementedAvatarsOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, onClick: EditImplementedAvatarsOverlay_ItemButton_Click);
            if (_editImplementedAvatarsOverlay_selectedAvatars.Contains(((Item)itemCountInfo.Item).Id)) button.Classes.Add("accent");
        }
    }

    private bool EditImplementedAvatarsOverlay_IsMatch(string searchIndex, string[] searchText) => searchText.Length == 0 || searchText.Any(i => searchIndex.Contains(i, System.StringComparison.CurrentCultureIgnoreCase));

    #region Event Handler
    private void EditImplementedAvatarsOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditImplementedAvatarsOverlay_Close();
    private void EditImplementedAvatarsOverlay_Confirm_Click(object? sender, RoutedEventArgs e)
    {
        Item? item = AvatarExplorer.GetItemById(_editImplementedAvatarsOverlay_selectedItemId);
        if (item == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        item.UpdateImplementedAvatars(_editImplementedAvatarsOverlay_selectedAvatars);
        AvatarExplorer.UpdateItemUpdatedDate(item.Id);

        AvatarExplorer.SaveItemDatabase();

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
