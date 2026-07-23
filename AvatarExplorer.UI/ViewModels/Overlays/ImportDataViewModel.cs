using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

// TODO: CoreのImporterができていないためできていない。

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ImportDataViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public int SelectedImportSourceIndex { get; set; }
    [Reactive] public string FolderPath { get; set; } = string.Empty;
    
    [Reactive] public bool ImportItems { get; set; } = true;
    [Reactive] public bool ImportThumbnails { get; set; } = true;
    
    private List<(string Name, DataImportType Type)> ImportSourceOptions { get; } =
    [
        ("Avatar Explorer V1.x.x", DataImportType.V1),
        ("KonoAsset", DataImportType.KonoAsset),
    ];

    public List<string> ImportSourceNames => ImportSourceOptions.ConvertAll(o => o.Name);

    private DataImportType SelectedImportSource => ImportSourceOptions[SelectedImportSourceIndex].Type;

    public IReactiveCommand BrowseFolderCommand { get; }
    public IReactiveCommand ImportCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ImportDataViewModel()
    {
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolder);
        ImportCommand = ReactiveCommand.Create(Import);
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);
    }

    public void Open()
    {
        SelectedImportSourceIndex = 0;
        FolderPath = string.Empty;
        IsVisible = true;
    }

    private async Task BrowseFolder()
    {
        var folders = await StorageService.OpenFolderDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: false
        );
        if (folders == null || folders.Length == 0) return;

        FolderPath = folders[0];
    }

    private void Import()
    {
        // TODO: インポート処理を実装
    }
}
