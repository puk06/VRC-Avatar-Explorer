using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ErrorOr;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ExportDataViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public int SelectedExportTypeIndex { get; set; }
    [Reactive] public string FolderPath { get; set; } = string.Empty;
    [Reactive] public bool IncludeCommonToSupported { get; set; } = true;

    private List<(string LocKey, DataExportType Type)> ExportTypeOptions { get; } =
    [
        (Loc.ExportData.ExportTypeOptions.Csv, DataExportType.Csv),
        (Loc.ExportData.ExportTypeOptions.KonoAsset, DataExportType.KonoAsset),
    ];

    public List<string> ExportTypeNames => ExportTypeOptions.ConvertAll(o => Localizer.Instance[o.LocKey]);

    private DataExportType SelectedExportType => (SelectedExportTypeIndex >= 0 && SelectedExportTypeIndex < ExportTypeOptions.Count)
        ? ExportTypeOptions[SelectedExportTypeIndex].Type
        : DataExportType.None;

    public IReactiveCommand BrowseFolderCommand { get; }
    public IReactiveCommand ExportCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ExportDataViewModel()
    {
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolder);
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        CancelCommand = ReactiveCommand.Create(Close);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        Localizer.Instance.LanguageChanged += OnLanguageChanged;
        OnLanguageChanged();
    }

    private void OnLanguageChanged()
    {
        this.RaisePropertyChanged(nameof(ExportTypeNames));
    }

    public void Open()
    {
        SelectedExportTypeIndex = 0;
        FolderPath = string.Empty;
        IsVisible = true;
    }

    private async Task BrowseFolder()
    {
        var folders = await StorageService.OpenFolderDialog(Localizer.Instance[Loc.Dialog.SelectSaveFolderPath]);
        if (folders == null || folders.Length == 0) return;

        FolderPath = folders[0];
    }

    private async Task Export()
    {
        if (string.IsNullOrEmpty(FolderPath)) return;

        ErrorOr<Success>? result = null;
        await NotificationManager.ShowWithProgress(
            Localizer.Instance[Loc.Processing.Export.Title],
            async progress =>
            {
                result = await InstanceRepository.ItemGroupService.Export(
                    SelectedExportType,
                    FolderPath,
                    GetLocalizedType,
                    IncludeCommonToSupported,
                    tuple =>
                    {
                        progress.Report(Localizer.Instance.Get(tuple.Item1, tuple.Item2.ToString()), tuple.Item2);
                        return Task.CompletedTask;
                    }
                );
            }
        );

        if (result == null || result.Value.IsError)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ExportFailed],
                NotificationType.Error
            );
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance[Loc.Success.Export],
                NotificationType.Success
            );
        }
    }
    private void Close() => IsVisible = false;

    private async ValueTask<string?> GetLocalizedType(ItemType type)
    {
        var locKey = type.GetLocalizationKey();
        if (string.IsNullOrEmpty(locKey)) return type.ToString();

        return Localizer.Instance[locKey];
    }
}
