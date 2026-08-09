using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
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
    [Reactive] public bool IncludeCommonToSupported { get; set; } = true;

    private List<(string Name, DataExportType Type)> ExportTypeOptions { get; } =
    [
        ("CSV", DataExportType.Csv),
    ];

    public List<string> ExportTypeNames => ExportTypeOptions.ConvertAll(o => o.Name);

    private DataExportType SelectedExportType => ExportTypeOptions[SelectedExportTypeIndex].Type;

    public IReactiveCommand BrowseFolderCommand { get; }
    public IReactiveCommand ExportCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ExportDataViewModel()
    {
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolder);
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
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
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, Localizer.Instance[Loc.Dialog.SelectSaveFolderPath]);
        if (folders == null || folders.Length == 0) return;

        FolderPath = folders[0];
    }

    private async Task Export()
    {
        if (string.IsNullOrEmpty(FolderPath)) return;

        var result = await AvatarExplorerApp.Instance.ItemGroupService.Export(SelectedExportType, FolderPath, GetLocalizedType, IncludeCommonToSupported);

        if (result.IsError)
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ExportFailed],
                NotificationType.Error
            );
        }
        else
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance[Loc.Success.Export],
                NotificationType.Success
            );
        }
    }

    private async ValueTask<string?> GetLocalizedType(ItemType type)
    {
        var locKey = type.GetLocalizationKey();
        if (string.IsNullOrEmpty(locKey)) return type.ToString();

        return Localizer.Instance[locKey];
    }
}
