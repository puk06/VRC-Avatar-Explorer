using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
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

        EditSupportedAvatarsOverlay_Initialize(avatars);
        EditSupportedAvatarsOverlay.IsVisible = true;

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
        _editSupportedAvatarsOverlay_selectedAvatars.Clear();
        EditSupportedAvatarsOverlay_SearchTextBox.Text = string.Empty;
        EditSupportedAvatarsOverlay_AvatarsList.Children.Clear();

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
        if (EditSupportedAvatarsOverlay_AvatarsList == null) return;
        EditSupportedAvatarsOverlay_AvatarsList.Children.Clear();

        string searchText = EditSupportedAvatarsOverlay_SearchTextBox.Text ?? string.Empty;
        string[] parsedText = TextParser.Parse(searchText);

        IEnumerable<ItemCountInfo> avatars = AvatarExplorer
            .GetAvatars(includeCommonAvatar: true, includeTempAvatar: true)
            .Where(
                i =>
                    string.IsNullOrEmpty(searchText) ||
                    (i.Item is Item item && EditSupportedAvatarsOverlay_IsMatch(AvatarExplorer.GetSearchIndexByItemId(item.Id), parsedText)) ||
                    (i.Item is CommonAvatar commonAvatar && EditSupportedAvatarsOverlay_IsMatch(commonAvatar.GroupName, parsedText)) ||
                    (i.Item is TempAvatar tempAvatar && EditSupportedAvatarsOverlay_IsMatch(tempAvatar.AvatarName, parsedText))
            );

        foreach (ItemCountInfo itemCountInfo in avatars)
        {
            Button button = ItemButtonFactory.AddItemButton(EditSupportedAvatarsOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, onClick: EditSupportedAvatarsOverlay_ItemButton_Click);

            string avatarId = string.Empty;

            if (itemCountInfo.Item is Item item) avatarId = item.Id;
            else if (itemCountInfo.Item is CommonAvatar commonAvatar) avatarId = commonAvatar.GetInternalId();
            else if (itemCountInfo.Item is TempAvatar tempAvatar) avatarId = tempAvatar.GetInternalId();

            if (!string.IsNullOrEmpty(avatarId) && _editSupportedAvatarsOverlay_selectedAvatars.Contains(avatarId)) button.Classes.Add("accent");
        }
    }
    
    private bool EditSupportedAvatarsOverlay_IsMatch(string searchIndex, string[] searchText) => searchText.Length == 0 || searchText.Any(i => searchIndex.Contains(i, StringComparison.CurrentCultureIgnoreCase));

    #region Event Handler
    private void EditSupportedAvatarsOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => EditSupportedAvatarsOverlay_Close(null);
    private void EditSupportedAvatarsOverlay_Confirm_Click(object? sender, RoutedEventArgs e) => EditSupportedAvatarsOverlay_Close(_editSupportedAvatarsOverlay_selectedAvatars.ToList());
    private async void EditSupportedAvatarsOverlay_AddTempAvatar_Click(object? sender, RoutedEventArgs e)
    {
        string? tempAvatarName = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewTempAvatarName]);
        if (string.IsNullOrEmpty(tempAvatarName)) return;

        AvatarExplorer.AddTempAvatar(tempAvatarName);

        EditSupportedAvatarsOverlay_DrawItemButtons();
        Main_ReloadCurrentWindow();
    }
    
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
