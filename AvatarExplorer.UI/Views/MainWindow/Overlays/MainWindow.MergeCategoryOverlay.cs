using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<ItemCategory?>? _mergeCategoryOverlay_tcs;

    // カスタムカテゴリかどうか(式: ItemTypeの数 - アイテムに指定不可なItemType数 - カスタムカテゴリ)
    private static readonly int MergeCategoryOverlay_CustomCategoryIndex = Enum.GetValues<ItemType>().Length - CategoryUtils.NonSelectableItemTypes.Length - 1;

    private Task<ItemCategory?> MergeCategoryOverlay_ShowAsync()
    {
        if (_mergeCategoryOverlay_tcs != null) throw new InvalidOperationException("MergeCategoryOverlay is already shown.");

        _mergeCategoryOverlay_tcs = new();

        MergeCategoryOverlay_InitializeCategories();

        MergeCategoryOverlay.IsVisible = true;

        return _mergeCategoryOverlay_tcs.Task;
    }

    private async Task<ItemCategory?> MergeCategoryOverlay_ShowAsyncSafe()
    {
        try
        {
            return await MergeCategoryOverlay_ShowAsync();
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError("Failed to open merge category dialog.", ex);
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenDialogFailed]);
            return null;
        }
    }

    private void MergeCategoryOverlay_Close(ItemCategory? result)
    {
        MergeCategoryOverlay.IsVisible = false;
        MergeCategoryOverlay_ItemTypeComboBox.Items.Clear();

        TaskCompletionSource<ItemCategory?>? tcs = _mergeCategoryOverlay_tcs;
        _mergeCategoryOverlay_tcs = null;

        tcs?.TrySetResult(result);
    }

    private void MergeCategoryOverlay_InitializeCategories()
    {
        MergeCategoryOverlay_ItemTypeComboBox.Items.Clear();
        MergeCategoryOverlay_ItemTypeComboBox.Items.AddRange(AvatarExplorer.GetCategories(includeEmptyCategory: true, includeAllCategory: false).Select(i => Localizer.Instance[((ItemCategory)i.Item).ToString()]));

        if (MergeCategoryOverlay_ItemTypeComboBox.Items.Count > 0) MergeCategoryOverlay_ItemTypeComboBox.SelectedIndex = 0;
    }

    private ItemCategory MergeCategoryOverlay_GetCurrentCategory()
    {
        int selectedIndex = MergeCategoryOverlay_ItemTypeComboBox.SelectedIndex;

        if (selectedIndex >= MergeCategoryOverlay_CustomCategoryIndex)
        {
            return new ItemCategory(MergeCategoryOverlay_ItemTypeComboBox.SelectedItem?.ToString() ?? string.Empty);
        }

        return new ItemCategory((ItemType)selectedIndex);
    }

    #region Event Handler
    private void MergeCategoryOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => MergeCategoryOverlay_Close(null);
    private void MergeCategoryOverlay_Merge_Click(object? sender, RoutedEventArgs e) => MergeCategoryOverlay_Close(MergeCategoryOverlay_GetCurrentCategory());
    #endregion
}
