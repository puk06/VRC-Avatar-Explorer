using System.Collections.Generic;
using System.Threading.Tasks;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ExportDataViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public int SelectedExportTypeIndex { get; set; }
    [Reactive] public string FolderPath { get; set; } = string.Empty;

    public List<string> ExportTypes { get; } = new()
    {
        "CSV"
    };

    public IReactiveCommand BrowseFolderCommand { get; }
    public IReactiveCommand ExportCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ExportDataViewModel()
    {
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolder);
        ExportCommand = ReactiveCommand.Create(Export);
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);
    }

    public void Open()
    {
        SelectedExportTypeIndex = 0;
        FolderPath = string.Empty;
        IsVisible = true;
    }

    private async Task BrowseFolder()
    {
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, "Select Export Folder");
        if (folders == null || folders.Length == 0) return;

        FolderPath = folders[0];
    }

    private void Export()
    {
        // TODO: エクスポート処理を実装
    }
}
