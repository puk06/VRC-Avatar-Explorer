using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Data;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Panels;

public partial class BulkImportPresetViewModel : ViewModelBase, IInitializable, IPostInitializable
{
    [Reactive] public partial IEnumerable<ItemViewModel> Items { get; set; } = [];

    public IReactiveCommand SelectItemCommand { get; }

    public BulkImportPresetViewModel()
    {
        SelectItemCommand = ReactiveCommand.Create<ItemViewModel>(Select);
        IInitializableRegistry.Register(0, (IInitializable)this);
        IInitializableRegistry.Register(0, (IPostInitializable)this);
    }

    public async Task Initialize()
    {
        Localizer.Instance.LanguageChanged += Reload;
        InstanceRepository.BulkImportPresets.OnUpdated += Reload;
    }

    public async Task OnInitialized()
    {
        Reload();
    }

    public void Reload()
    {
        Items = InstanceRepository.BulkImportPresets.GetAll()
            .Select(i => {
                var vm = new ItemViewModel()
                {
                    ThumbnailSource = new() { Primary = SystemIconKey.FolderIcon },
                    TitleRaw = i.PresetName,
                    TitleLocalizable = false,
                    DescriptionRaw = new(Loc.Button.Description.BulkImportPreset.Count, [i.Items.Length.ToString()]),
                    Identifier = i.Identifier,
                    ViewModelType = ViewModelType.BulkImportPreset,
                };
                vm.Actions = ContextMenuCreator.Create(vm.ViewModelType, vm);
                return vm.Update();
            });
    }

    public static void Select(ItemViewModel presetVm)
    {
        var presetIdentifier = presetVm.Identifier;
        var preset = InstanceRepository.BulkImportPresets.Get(presetIdentifier);
        if (preset == null) return;

        var bulkVm = InstanceRepository.MainWindow.MainVM.BulkImportVM;
        foreach (var item in preset.Items) bulkVm.AddItem(item.ItemId, item.FilePath);

        // 一括インポートの画面
        InstanceRepository.MainWindow.MainVM.SelectedSidePanelTab = 1;
    }
}
