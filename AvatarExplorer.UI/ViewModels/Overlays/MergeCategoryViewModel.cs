using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class MergeCategoryViewModel : ViewModelBase
{
    public IEnumerable<string> ItemCategories { get; set; } = [];
    [Reactive] public int SelectedCategory { get; set; } = 0;

    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand MergeCommand { get; }

    public MergeCategoryViewModel()
    {
        CancelCommand = ReactiveCommand.Create(OnCancel);
        MergeCommand = ReactiveCommand.Create(OnMerge);
    }

    public void Reload()
    {
        // ItemCategories = AvatarExplorerApp.Instance
        //     .GetCategories(includeEmptyCategory: true, includeAllCategory: false)
        //     .Select(i => Localizer.Instance[((ItemCategory)i.Item).ToString()]);
    }

    public void OnCancel()
    {
        
    }

    public void OnMerge()
    {
        
    }
}
