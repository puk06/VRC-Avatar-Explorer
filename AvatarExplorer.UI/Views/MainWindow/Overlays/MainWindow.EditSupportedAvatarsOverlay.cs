using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private readonly List<string> _editSupportedAvatarsOverlay_selectedAvatars = new();
    private TaskCompletionSource<List<string>?>? _editSupportedAvatarsOverlay_tcs;

    private Task<List<string>?> EditSupportedAvatarsOverlay_OpenAsync(IEnumerable<string>? avatars = null)
    {
        if (_editSupportedAvatarsOverlay_tcs != null) throw new InvalidOperationException("EditSupportedAvatarsOverlay is already shown.");

        _editSupportedAvatarsOverlay_tcs = new();

        EditSupportedAvatarsOverlay.IsVisible = true;
        EditSupportedAvatarsOverlay_Initialize(avatars);

        return _editSupportedAvatarsOverlay_tcs.Task;
    }
    private async Task<List<string>?> EditSupportedAvatarsOverlay_OpenAsyncSafe(IEnumerable<string>? avatars = null)
    {
        try
        {
            return await EditSupportedAvatarsOverlay_OpenAsync(avatars);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open dialog.", ex);
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed]);
            return null;
        }
    }
    private void EditSupportedAvatarsOverlay_Close(List<string>? result)
    {
        EditSupportedAvatarsOverlay.IsVisible = false;

        TaskCompletionSource<List<string>?>? tcs = _editSupportedAvatarsOverlay_tcs;
        _editSupportedAvatarsOverlay_tcs = null;

        tcs?.TrySetResult(result);
    }

    private void EditSupportedAvatarsOverlay_Initialize(IEnumerable<string>? avatars = null)
    {
        _editSupportedAvatarsOverlay_selectedAvatars.Clear();
        if (avatars != null) _editSupportedAvatarsOverlay_selectedAvatars.AddRange(avatars);
        EditSupportedAvatarsOverlay_DrawItemButtons();
    }
    private void EditSupportedAvatarsOverlay_DrawItemButtons()
    {
        EditSupportedAvatarsOverlay_AvatarsList.Children.Clear();
        IEnumerable<ItemCountInfo> avatars = _avatarExplorerApp.GetAvatars(includeCommonAvatar: true).Where(i => string.IsNullOrEmpty(EditSupportedAvatarsOverlay_SearchTextBox.Text) || (i.Item is Item item && item.Title.Contains(EditSupportedAvatarsOverlay_SearchTextBox.Text)) || (i.Item is CommonAvatar commonAvatar && commonAvatar.GroupName.Contains(EditSupportedAvatarsOverlay_SearchTextBox.Text)));

        foreach (ItemCountInfo itemCountInfo in avatars)
        {
            Button button = ItemButtonFactory.AddItemButton(EditSupportedAvatarsOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, onClick: EditSupportedAvatarsOverlay_ItemButton_Click);

            string avatarId = string.Empty;

            if (itemCountInfo.Item is Item item) avatarId = item.Id;
            else if (itemCountInfo.Item is CommonAvatar commonAvatar) avatarId = commonAvatar.GetInternalId();

            if (!string.IsNullOrEmpty(avatarId) && _editSupportedAvatarsOverlay_selectedAvatars.Contains(avatarId)) button.Classes.Add("accent");
        }
    }

    #region Event Handler
    private void EditSupportedAvatarsOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditSupportedAvatarsOverlay_Close(null);
    private void EditSupportedAvatarsOverlay_Confirm_Click(object? sender, RoutedEventArgs e) => EditSupportedAvatarsOverlay_Close(_editSupportedAvatarsOverlay_selectedAvatars);
    
    private void EditSupportedAvatarsOverlay_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ItemTagInfo itemTagInfo) return;

        if (_editSupportedAvatarsOverlay_selectedAvatars.Contains(itemTagInfo.Value)) _editSupportedAvatarsOverlay_selectedAvatars.RemoveAll(i => i == itemTagInfo.Value);
        else _editSupportedAvatarsOverlay_selectedAvatars.Add(itemTagInfo.Value);

        EditSupportedAvatarsOverlay_DrawItemButtons();
    }
    private void EditSupportedAvatarsOverlay_SearchTextBox_TextChanged(object? sender, RoutedEventArgs e) => EditSupportedAvatarsOverlay_DrawItemButtons();
    #endregion
}
