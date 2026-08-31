using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.ViewModels.Component;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class MergeCategoryViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public ObservableCollection<ItemCategoryViewModel> Categories { get; set; } = [];
    [Reactive] public int SelectedSourceCategoryIndex { get; set; } = 0;
    [Reactive] public int SelectedTargetCategoryIndex { get; set; } = 0;
    public ItemCategoryViewModel? SelectedSourceCategory => Categories.IsValidIndex(SelectedSourceCategoryIndex) ? Categories[SelectedSourceCategoryIndex] : null;
    public ItemCategoryViewModel? SelectedTargetCategory => Categories.IsValidIndex(SelectedTargetCategoryIndex) ? Categories[SelectedTargetCategoryIndex] : null;

    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand MergeCommand { get; }

    private static ItemRepository Items => InstanceRepository.Items;

    public MergeCategoryViewModel()
    {
        CancelCommand = ReactiveCommand.Create(Cancel);
        MergeCommand = ReactiveCommand.CreateFromTask(Merge);
    }

    public void Open(string state)
    {
        SelectedSourceCategoryIndex = -1;
        SelectedTargetCategoryIndex = -1;

        RefleshCategories();

        var sourceIndex = GetCategoryIndex(ItemCategory.FromIdentifier(state));
        SelectedSourceCategoryIndex = Categories.IsValidIndex(sourceIndex) ? sourceIndex : 0;

        var targetIndex = GetCategoryIndex(ItemCategory.Get(ItemType.Avatar));
        SelectedTargetCategoryIndex = Categories.IsValidIndex(targetIndex) ? targetIndex : 0;

        IsVisible = true;
    }

    public int GetCategoryIndex(ItemCategory? category)
    {
        if (category == null) return 0;

        for (int i = 0; i < Categories.Count; i++)
        {
            if (Categories[i].Category.Equals(category))
            {
                return i;
            }
        }

        return -1;
    }

    private void RefleshCategories()
    {
        var categories = InstanceRepository.ItemGroupService
            .GetCategoryFolders(includeEmptyCategory: true, includeAllCategory: false)
            .Select(i => ItemCategory.FromIdentifier(i.Identifier));

        Categories.Clear();
        Categories.AddRange(categories.Select(i => new ItemCategoryViewModel(i).Update()));
    }

    public void Cancel()
    {
        SelectedSourceCategoryIndex = -1;
        SelectedTargetCategoryIndex = -1;
        IsVisible = false;
    }

    public async Task Merge()
    {
        if (SelectedSourceCategory == null || SelectedTargetCategory == null) return;

        var sourceCategoryName = SelectedSourceCategory.DisplayName;
        var targetCategoryName = SelectedTargetCategory.DisplayName;

        var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance.Get(Loc.Dialog.Confirmation.MergeCategory, [sourceCategoryName, targetCategoryName])
        );
        if (!result) return;

        Items.MergeCategory(SelectedSourceCategory.Category, SelectedTargetCategory.Category);

        SelectedSourceCategoryIndex = -1;
        SelectedTargetCategoryIndex = -1;
        IsVisible = false;
    }
}
