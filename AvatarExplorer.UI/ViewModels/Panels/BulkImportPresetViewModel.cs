using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class BulkImportPresetViewModel : ViewModelBase, IPostInitializable
{
    [Reactive] public IEnumerable<ItemViewModel> Items { get; set; } = [];

    public IReactiveCommand SelectItemCommand { get; }

    public BulkImportPresetViewModel()
    {
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(Select);
        IInitializableRegistry.Register(this);
    }

    public async Task OnInitialized()
    {
        await Dispatcher.UIThread.InvokeAsync(Reload);
    }

    public async void Reload()
    {
        Items = AvatarExplorerApp.Instance.BulkImportPresets.GetAll()
            .Select(i => new ItemViewModel()
            {
                ImageFileName = SystemIconKey.FolderIcon,
                TitleRaw = i.PresetName,
                TitleLocalizable = false,
                DescriptionRaw = new(Loc.Button.Description.Item.Count, [i.Items.Length.ToString()]),
                Identifier = i.Identifier,
                ViewModelType = ViewModelType.BulkImportPreset
            }.Update());
    }

    public void Select(ItemViewModel presetVm)
    {
        Debug.WriteLine("SELECTED!!!!!!!");
        var presetIdentifier = presetVm.Identifier;
        var preset = AvatarExplorerApp.Instance.BulkImportPresets.Get(presetIdentifier);
        if (preset == null) return;

        var bulkVm = MainWindowViewModel.Instance.MainVM.BulkImportVM;
        foreach (var item in preset.Items) bulkVm.AddItem(item.ItemId, item.FilePath);
    }
}
