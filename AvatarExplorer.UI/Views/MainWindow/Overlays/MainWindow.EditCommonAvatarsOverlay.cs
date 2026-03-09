using System;
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
    private string? _editCommonAvatarsOverlay_SelectedGroupId = null;

    private void EditCommonAvatarsOverlay_Show()
    {
        EditCommonAvatarsOverlay.IsVisible = true;

        EditCommonAvatarsOverlay_RefleshGroupList();
        EditCommonAvatarsOverlay_RefleshAvatarList();

        if (EditCommonAvatarsOverlay_GroupComboBox.Items.Count > 0) EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = 0;
    }
    private void EditCommonAvatarsOverlay_Hide() => EditCommonAvatarsOverlay.IsVisible = false;

    private void EditCommonAvatarsOverlay_RefleshGroupList()
    {
        EditCommonAvatarsOverlay_GroupComboBox.Items.Clear();
        foreach (CommonAvatar commonAvatar in _avatarExplorerApp.GetAllCommonAvatars())
        {
            EditCommonAvatarsOverlay_GroupComboBox.Items.Add(new ComboBoxItem
            {
                Content = commonAvatar.GroupName,
                Tag = commonAvatar.Id
            });
        }
    }

    private void EditCommonAvatarsOverlay_RefleshAvatarList()
    {
        if (EditCommonAvatarsOverlay_AvatarsList == null) return;
        EditCommonAvatarsOverlay_AvatarsList.Children.Clear();
        IEnumerable<ItemCountInfo> avatars = _avatarExplorerApp.GetAvatars()
            .Where(i => string.IsNullOrEmpty(EditCommonAvatarsOverlay_SearchTextBox.Text) || ((Item)i.Item).Title.Contains(EditCommonAvatarsOverlay_SearchTextBox.Text));

        CommonAvatar? commonAvatar = _avatarExplorerApp.GetCommonAvatarById(_editCommonAvatarsOverlay_SelectedGroupId);
        if (commonAvatar == null) return;

        foreach (ItemCountInfo itemCountInfo in avatars)
        {
            Button button = ItemButtonFactory.AddItemButton(EditCommonAvatarsOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, _userPreferences, onClick: EditCommonAvatarsOverlay_ItemButton_Click);
            if (commonAvatar.AvatarsView.Contains(((Item)itemCountInfo.Item).Id)) button.Classes.Add("accent");
        }
    }

    #region Event Handler
    private void EditCommonAvatarsOverlay_Close_Click(object? sender, RoutedEventArgs e) => EditCommonAvatarsOverlay_Hide();
    private void EditCommonAvatarsOverlay_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_editCommonAvatarsOverlay_SelectedGroupId == null) return;
        if (sender is not Button button || button.Tag is not ItemTagInfo itemTagInfo) return;

        CommonAvatar? commonAvatar = _avatarExplorerApp.GetCommonAvatarById(_editCommonAvatarsOverlay_SelectedGroupId);
        if (commonAvatar == null) return;

        if (commonAvatar.AvatarsView.Contains(itemTagInfo.Value)) commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Where(i => i != itemTagInfo.Value));
        else commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Append(itemTagInfo.Value));

        EditCommonAvatarsOverlay_RefleshAvatarList();
    }
    private async void EditCommonAvatarsOverlay_AddGroup_Click(object? sender, RoutedEventArgs e)
    {
        string? commonAvatarGroupName = await ShowTextDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.AddCommonAvatarGroup]);
        if (string.IsNullOrEmpty(commonAvatarGroupName)) return;

        _avatarExplorerApp.AddCommonAvatar(commonAvatarGroupName);

        EditCommonAvatarsOverlay_RefleshGroupList();
        EditCommonAvatarsOverlay_RefleshAvatarList();

        // 追加された共通素体グループを選択してあげる
        EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = _avatarExplorerApp.GetAllCommonAvatars().Count - 1;

        EditCommonAvatarsOverlay_RefleshAvatarList();
    }
    private async void EditCommonAvatarsOverlay_RenameGroup_Click(object? sender, RoutedEventArgs e)
    {
        CommonAvatar? commonAvatar = _avatarExplorerApp.GetCommonAvatarById(_editCommonAvatarsOverlay_SelectedGroupId);
        if (commonAvatar == null)
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CommonAvatarNotFound]);
            return;
        }
        
        string? commonAvatarGroupName = await ShowTextDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewCommonAvatarGroupName], commonAvatar.GroupName);
        if (string.IsNullOrEmpty(commonAvatarGroupName)) return;
        
        commonAvatar.GroupName = commonAvatarGroupName;
        _avatarExplorerApp.SaveCommonAvatarDatabase();

        int previousIndex = EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex;
        EditCommonAvatarsOverlay_RefleshGroupList();
        if (previousIndex != -1) EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = previousIndex;
        EditCommonAvatarsOverlay_RefleshAvatarList();

        Main_ReloadCurrentWindow();
    }
    private async void EditCommonAvatarsOverlay_ReplaceAvatarsToGroup_Click(object? sender, RoutedEventArgs e)
    {
        CommonAvatar? commonAvatar = _avatarExplorerApp.GetCommonAvatarById(_editCommonAvatarsOverlay_SelectedGroupId);
        if (commonAvatar == null)
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CommonAvatarNotFound]);
            return;
        }

        YesNoResult? result = await ShowYesNoDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.EditCommonAvatars.ReplaceAvatarsToGroup));
        if (result == null || result != YesNoResult.Yes) return;

        _avatarExplorerApp.ReplaceSupportedAvatarsToCommonAvatarGroup(commonAvatar.Id);
        _avatarExplorerApp.SaveItemDatabase();
        Main_ReloadCurrentWindow();
    }
    private async void EditCommonAvatarsOverlay_RemoveGroup_Click(object? sender, RoutedEventArgs e)
    {
        CommonAvatar? commonAvatar = _avatarExplorerApp.GetCommonAvatarById(_editCommonAvatarsOverlay_SelectedGroupId);
        if (commonAvatar == null)
        {
            Dialog_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CommonAvatarNotFound]);
            return;
        }

        YesNoResult? result = await ShowYesNoDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveCommonAvatarGroup, commonAvatar.GroupName));
        if (result == null || result != YesNoResult.Yes) return;

        YesNoResult? result2 = await ShowYesNoDialogSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.EditCommonAvatars.ReplaceGroupToAvatars));
        if (result2 != null && result2 == YesNoResult.Yes) _avatarExplorerApp.ReplaceCommonAvatarGroupToSupportedAvatars(commonAvatar.Id);

        _avatarExplorerApp.RemoveCommonAvatar(commonAvatar.Id);
        _avatarExplorerApp.SaveCommonAvatarDatabase();

        EditCommonAvatarsOverlay_RefleshGroupList();
        if (EditCommonAvatarsOverlay_GroupComboBox.Items.Count > 0) EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = 0;
        EditCommonAvatarsOverlay_RefleshAvatarList();

        Main_ReloadCurrentWindow();
    }
    private void EditCommonAvatarsOverlay_GroupComboBox_Changed(object? sender, RoutedEventArgs e)
    {
        if (EditCommonAvatarsOverlay_GroupComboBox == null) return;
        _editCommonAvatarsOverlay_SelectedGroupId = (EditCommonAvatarsOverlay_GroupComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        EditCommonAvatarsOverlay_RefleshAvatarList();
    }
    private void EditCommonAvatarsOverlay_SearchTextBox_Changed(object? sender, RoutedEventArgs e) => EditCommonAvatarsOverlay_RefleshAvatarList();
    #endregion
}
