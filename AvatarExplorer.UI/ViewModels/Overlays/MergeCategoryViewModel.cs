using System.Collections.Generic;
using System.Linq;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class MergeCategoryViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    public IEnumerable<ItemCategoryViewModel> Categories { get; set; } = [];
    [Reactive] public ItemCategoryViewModel? SelectedSourceCategory { get; set; } = null;
    [Reactive] public ItemCategoryViewModel? SelectedTargetCategory { get; set; } = null;

    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand MergeCommand { get; }

    public MergeCategoryViewModel()
    {
        CancelCommand = ReactiveCommand.Create(OnCancel);
        MergeCommand = ReactiveCommand.Create(OnMerge);
    }

    public void Open(ItemCategory initialSourceCategory)
    {
        RefleshCategories();

        SelectedTargetCategory = GetVMFromCategory(initialSourceCategory);
        SelectedTargetCategory = GetVMFromCategory(new(ItemType.Avatar));
    }

    private ItemCategoryViewModel? GetVMFromCategory(ItemCategory category)
    {
        foreach (var cat in Categories)
        {
            if (cat.Category.Equals(category)) return cat;
        }

        return null;
    }

    private void RefleshCategories()
    {
        Categories = AvatarExplorerApp.Instance
            .ItemGroupService
            .GetCategories(includeEmptyCategory: true, includeAllCategory: false)
            .Select(i => new ItemCategoryViewModel(ResolveCategory(i.Identifier)));
    }
    
    private static ItemCategory ResolveCategory(string groupKey)
    {
        if (!ItemNavigationService.TryParseState(groupKey, out var prefix, out var value)) return new(ItemType.Avatar);

        if (prefix == ItemNavigationService.TypePrefix)
        {
            if (ItemNavigationService.TryResolveItemType(value, out var itemType))
            {
                return new(itemType);
            }

            return new(ItemType.Avatar);
        }

        if (prefix == ItemNavigationService.CustomPrefix) return new(value);

        return new(ItemType.Avatar);
    }

    public void OnCancel()
    {
        IsVisible = false;
    }

    public void OnMerge()
    {
        IsVisible = false;
    }
}
