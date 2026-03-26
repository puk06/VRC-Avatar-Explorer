using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Items;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private string? _resolveTempAvatarOverlay_selectedAvatar = null;

    private void ResolveTempAvatarOverlay_Open(string tempAvatarId)
    {
        _resolveTempAvatarOverlay_selectedAvatar = tempAvatarId;
        ResolveTempAvatarOverlay_DrawItemButtons();
        ResolveTempAvatarOverlay.IsVisible = true;
    }
    private void ResolveTempAvatarOverlay_Close()
    {
        ResolveTempAvatarOverlay.IsVisible = false;
        _resolveTempAvatarOverlay_selectedAvatar = null;
        ResolveTempAvatarOverlay_SearchTextBox.Text = string.Empty;
        ResolveTempAvatarOverlay_AvatarsList.Children.Clear();
    }

    private void ResolveTempAvatarOverlay_DrawItemButtons()
    {
        ResolveTempAvatarOverlay_AvatarsList.Children.Clear();
        IEnumerable<ItemCountInfo> avatars = AvatarExplorer.GetAvatars().Where(i => string.IsNullOrEmpty(ResolveTempAvatarOverlay_SearchTextBox.Text) || ((Item)i.Item).Title.Contains(ResolveTempAvatarOverlay_SearchTextBox.Text));

        foreach (ItemCountInfo itemCountInfo in avatars)
        {
            ItemButtonFactory.AddItemButton(ResolveTempAvatarOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, onClick: ResolveTempAvatarOverlay_ItemButton_Click);
        }
    }

    #region Event Handler
    private void ResolveTempAvatarOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => ResolveTempAvatarOverlay_Close();
    private async void ResolveTempAvatarOverlay_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not ItemTagInfo itemTagInfo) return;

        if (_resolveTempAvatarOverlay_selectedAvatar == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.TempAvatarNotFound]);
            return;
        }

        TempAvatar? tempAvatar = AvatarExplorer.GetTempAvatarById(TempAvatar.GetAvatarId(_resolveTempAvatarOverlay_selectedAvatar));
        if (tempAvatar == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.TempAvatarNotFound]);
            return;
        }

        Item? targetAvatar = AvatarExplorer.GetItemById(itemTagInfo.Value);
        if (targetAvatar == null)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ItemNotFound]);
            return;
        }

        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.ResolveTempAvatar, [tempAvatar.AvatarName, targetAvatar.Title]));
        if (result == null || result != YesNoResult.Yes) return;

        AvatarExplorer.ResolveTempAvatar(tempAvatar.GetInternalId(), targetAvatar.Id);

        ResolveTempAvatarOverlay_Close();
        Main_ReloadCurrentWindow();
    }
    private void ResolveTempAvatarOverlay_SearchTextBox_TextChanged(object? sender, RoutedEventArgs e) => ResolveTempAvatarOverlay_DrawItemButtons();
    #endregion
}
