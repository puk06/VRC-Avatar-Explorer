using System.Collections.Generic;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ImportDataViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public int SelectedImportSourceIndex { get; set; }
    [Reactive] public string FolderPath { get; set; } = string.Empty;
    
    [Reactive] public bool ImportItems { get; set; } = true;
    [Reactive] public bool ImportThumbnails { get; set; } = true;
    
    public List<string> ImportSources { get; } = new()
    {
        "Avatar Explorer V1.x.x",
        "KonoAsset"
    };

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
