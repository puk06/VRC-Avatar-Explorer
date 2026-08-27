using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ErrorOr;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ImportDataViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public int SelectedImportSourceIndex { get; set; }
    [Reactive] public string FolderPath { get; set; } = string.Empty;

    [Reactive] public bool ImportItems { get; set; } = true;
    [Reactive] public bool ImportThumbnails { get; set; } = true;
    [Reactive] public bool CanImportThumbnails { get; set; } = true;

    private List<(string LocKey, DataImportType Type)> ImportSourceOptions { get; } =
    [
        (Loc.ImportData.ImportSourceOptions.V1, DataImportType.V1),
        (Loc.ImportData.ImportSourceOptions.KonoAsset, DataImportType.KonoAsset),
        (Loc.ImportData.ImportSourceOptions.Folder, DataImportType.Folder)
    ];

    public List<string> ImportSourceNames => ImportSourceOptions.ConvertAll(o => Localizer.Instance[o.LocKey]);

    private DataImportType SelectedImportSource => ImportSourceNames.IsValidIndex(SelectedImportSourceIndex)
        ? ImportSourceOptions[SelectedImportSourceIndex].Type
        : DataImportType.None;

    public IReactiveCommand BrowseFolderCommand { get; }
    public IReactiveCommand ImportCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public ImportDataViewModel()
    {
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolder);
        ImportCommand = ReactiveCommand.CreateFromTask(Import);
        CloseCommand = ReactiveCommand.Create(Close);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(x => x.SelectedImportSourceIndex)
            .Subscribe(_ => CanImportThumbnails = SelectedImportSource != DataImportType.Folder);

        Localizer.Instance.LanguageChanged += OnLanguageChanged;
        OnLanguageChanged();
    }

    private void OnLanguageChanged()
    {
        this.RaisePropertyChanged(nameof(ImportSourceNames));
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
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: false
        );
        if (folders == null || folders.Length == 0) return;

        FolderPath = folders[0];
    }

    private async Task Import()
    {
        if (string.IsNullOrEmpty(FolderPath)) return;

        var type = SelectedImportSource;
        if (ImportItems) type |= DataImportType.Items;
        if (ImportThumbnails) type |= DataImportType.Thumbnails;

        var copyAssetData = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.CopyAssetData]
        );

        var request = new ImportRequest
        {
            ImportType = type,
            DataFolderPath = FolderPath,
            CopyAssetData = copyAssetData,
            ReportProgress = null
        };

        ErrorOr<Success>? result = null;
        await NotificationManager.ShowWithProgress(
            Localizer.Instance[Loc.Processing.Import.Title],
            async progress =>
            {
                request.ReportProgress = p =>
                {
                    progress.Report(Localizer.Instance.Get(p.Message, p.Percent.ToString()), p.Percent);
                    return Task.CompletedTask;
                };
                result = await InstanceRepository.ItemGroupService.Import(request);
            }
        );

        if (result?.IsError is false)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance[Loc.Success.Import],
                NotificationType.Success
            );
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ImportFailed],
                NotificationType.Error
            );
        }
    }

    private void Close() => IsVisible = false;
}
