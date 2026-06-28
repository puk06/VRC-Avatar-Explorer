using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Items;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private string? _editCommonAvatarsOverlay_selectedGroupId = null;

    private void EditCommonAvatarsOverlay_Open()
    {
        EditCommonAvatarsOverlay_RefleshGroupList();
        EditCommonAvatarsOverlay_DrawItemButtons();

        if (EditCommonAvatarsOverlay_GroupComboBox.Items.Count > 0) EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = 0;
        EditCommonAvatarsOverlay.IsVisible = true;
    }
    private void EditCommonAvatarsOverlay_Close()
    {
        EditCommonAvatarsOverlay.IsVisible = false;
        _editCommonAvatarsOverlay_selectedGroupId = null;
        EditCommonAvatarsOverlay_SearchTextBox.Text = string.Empty;
        EditCommonAvatarsOverlay_AvatarsList.Children.Clear();
    }

    private void EditCommonAvatarsOverlay_RefleshGroupList()
    {
        EditCommonAvatarsOverlay_GroupComboBox.Items.Clear();
        foreach (var commonAvatar in AvatarExplorer.GetAllCommonAvatars())
        {
            EditCommonAvatarsOverlay_GroupComboBox.Items.Add(new ComboBoxItem
            {
                Content = commonAvatar.GroupName,
                Tag = commonAvatar.Id
            });
        }
    }

    private void EditCommonAvatarsOverlay_DrawItemButtons()
    {
        if (EditCommonAvatarsOverlay_AvatarsList == null) return;
        EditCommonAvatarsOverlay_AvatarsList.Children.Clear();

        var searchText = EditCommonAvatarsOverlay_SearchTextBox.Text ?? string.Empty;
        var parsedText = TextParser.Parse(searchText);

        var avatars = AvatarExplorer.GetAvatars(includeTempAvatar: true)
            .Where(i =>
                string.IsNullOrEmpty(searchText) ||
                (i.Item is Item item && EditCommonAvatarsOverlay_IsMatch(AvatarExplorer.GetSearchIndexByItemId(item.Id), parsedText)) ||
                (i.Item is TempAvatar tempAvatar && EditCommonAvatarsOverlay_IsMatch(tempAvatar.AvatarName, parsedText))
            );

        var commonAvatar = AvatarExplorer.GetCommonAvatarById(_editCommonAvatarsOverlay_selectedGroupId);
        if (commonAvatar == null) return;

        foreach (var itemCountInfo in avatars)
        {
            var button = ItemButtonFactory.AddItemButton(EditCommonAvatarsOverlay_AvatarsList, new UISelectableItem(itemCountInfo), RuntimeSettings, UserPreferences, onClick: EditCommonAvatarsOverlay_ItemButton_Click);
            if ((itemCountInfo.Item is Item item && commonAvatar.Avatars.Contains(item.Id)) || (itemCountInfo.Item is TempAvatar tempAvatar && commonAvatar.Avatars.Contains(tempAvatar.GetInternalId()))) button.Classes.Add("accentbutton");
        }
    }

    private bool EditCommonAvatarsOverlay_IsMatch(string searchIndex, string[] searchText) => searchText.Length == 0 || searchText.Any(i => searchIndex.Contains(i, System.StringComparison.CurrentCultureIgnoreCase));

    #region Event Handler
    private void EditCommonAvatarsOverlay_Close_Click(object? sender, RoutedEventArgs e) => EditCommonAvatarsOverlay_Close();
    private void EditCommonAvatarsOverlay_ItemButton_Click(object? sender, RoutedEventArgs e)
    {
        if (_editCommonAvatarsOverlay_selectedGroupId == null) return;
        if (sender is not Button button || button.Tag is not ItemTagInfo itemTagInfo) return;

        var commonAvatar = AvatarExplorer.GetCommonAvatarById(_editCommonAvatarsOverlay_selectedGroupId);
        if (commonAvatar == null) return;

        if (commonAvatar.Avatars.Contains(itemTagInfo.Value)) commonAvatar.UpdateAvatars(commonAvatar.Avatars.Where(i => i != itemTagInfo.Value));
        else commonAvatar.UpdateAvatars(commonAvatar.Avatars.Append(itemTagInfo.Value));

        EditCommonAvatarsOverlay_DrawItemButtons();
    }
    private async void EditCommonAvatarsOverlay_AddGroup_Click(object? sender, RoutedEventArgs e)
    {
        var commonAvatarGroupName = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.AddCommonAvatarGroup]);
        if (string.IsNullOrEmpty(commonAvatarGroupName)) return;

        AvatarExplorer.AddCommonAvatar(commonAvatarGroupName);

        EditCommonAvatarsOverlay_RefleshGroupList();
        EditCommonAvatarsOverlay_DrawItemButtons();

        // 追加された共通素体グループを選択してあげる
        EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = AvatarExplorer.GetAllCommonAvatars().Length - 1;

        EditCommonAvatarsOverlay_DrawItemButtons();
    }
    private async void EditCommonAvatarsOverlay_RenameGroup_Click(object? sender, RoutedEventArgs e)
    {
        var commonAvatar = AvatarExplorer.GetCommonAvatarById(_editCommonAvatarsOverlay_selectedGroupId);
        if (commonAvatar == null)
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CommonAvatarNotFound], isError: true);
            return;
        }
        
        var commonAvatarGroupName = await TextDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Title.NewCommonAvatarGroupName], commonAvatar.GroupName);
        if (string.IsNullOrEmpty(commonAvatarGroupName)) return;
        
        commonAvatar.GroupName = commonAvatarGroupName;
        AvatarExplorer.SaveCommonAvatarDatabase();

        int previousIndex = EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex;
        EditCommonAvatarsOverlay_RefleshGroupList();
        if (previousIndex != -1) EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = previousIndex;
        EditCommonAvatarsOverlay_DrawItemButtons();

        Main_ReloadCurrentWindow();
    }
    private async void EditCommonAvatarsOverlay_ReplaceAvatarsToGroup_Click(object? sender, RoutedEventArgs e)
    {
        var commonAvatar = AvatarExplorer.GetCommonAvatarById(_editCommonAvatarsOverlay_selectedGroupId);
        if (commonAvatar == null)
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CommonAvatarNotFound], isError: true);
            return;
        }

        var result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.EditCommonAvatars.ReplaceAvatarsToGroup));
        if (result == null || result != YesNoResult.Yes) return;

        AvatarExplorer.ReplaceSupportedAvatarsToCommonAvatarGroup(commonAvatar.Id);
        Main_ReloadCurrentWindow();
    }
    private async void EditCommonAvatarsOverlay_RemoveGroup_Click(object? sender, RoutedEventArgs e)
    {
        var commonAvatar = AvatarExplorer.GetCommonAvatarById(_editCommonAvatarsOverlay_selectedGroupId);
        if (commonAvatar == null)
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CommonAvatarNotFound], isError: true);
            return;
        }

        var result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.RemoveCommonAvatarGroup, commonAvatar.GroupName));
        if (result == null || result != YesNoResult.Yes) return;

        var result1 = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.EditCommonAvatars.ReplaceGroupToAvatars));
        if (result1 != null && result1 == YesNoResult.Yes) AvatarExplorer.ReplaceCommonAvatarGroupToSupportedAvatars(commonAvatar.Id);

        AvatarExplorer.RemoveCommonAvatar(commonAvatar.GetInternalId());

        EditCommonAvatarsOverlay_RefleshGroupList();
        if (EditCommonAvatarsOverlay_GroupComboBox.Items.Count > 0) EditCommonAvatarsOverlay_GroupComboBox.SelectedIndex = 0;
        EditCommonAvatarsOverlay_DrawItemButtons();

        Main_ReloadCurrentWindow();
    }
    private void EditCommonAvatarsOverlay_GroupComboBox_SelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (EditCommonAvatarsOverlay_GroupComboBox == null) return;
        _editCommonAvatarsOverlay_selectedGroupId = (EditCommonAvatarsOverlay_GroupComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString();
        EditCommonAvatarsOverlay_DrawItemButtons();
    }
    private void EditCommonAvatarsOverlay_SelectVisible_Click(object? sender, RoutedEventArgs e)
    {
        var commonAvatar = AvatarExplorer.GetCommonAvatarById(_editCommonAvatarsOverlay_selectedGroupId);
        if (commonAvatar == null) return;

        var visibleAvatarIds = EditCommonAvatarsOverlay_AvatarsList.Children
            .OfType<Button>()
            .Select(button => (button.Tag as ItemTagInfo)?.Value)
            .Where(value => !string.IsNullOrEmpty(value))
            .Cast<string>();

        commonAvatar.UpdateAvatars(commonAvatar.Avatars.Concat(visibleAvatarIds).Distinct());

        EditCommonAvatarsOverlay_DrawItemButtons();
    }
    private void EditCommonAvatarsOverlay_SearchTextBox_TextChanged(object? sender, RoutedEventArgs e) => EditCommonAvatarsOverlay_DrawItemButtons();
    #endregion
}
