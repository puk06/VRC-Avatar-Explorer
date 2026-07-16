using System.Collections.ObjectModel;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.External;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class BulkImportPresetViewModel
{
    [Reactive] public ObservableCollection<ItemViewModel> Items { get; set; } = [];

    public IReactiveCommand SelectItemCommand { get; }

    public BulkImportPresetViewModel()
    {
    }
}
