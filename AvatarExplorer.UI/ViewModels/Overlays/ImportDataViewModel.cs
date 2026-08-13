using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Threading;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
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

    private DataImportType SelectedImportSource => (SelectedImportSourceIndex >= 0 && SelectedImportSourceIndex < ImportSourceOptions.Count)
        ? ImportSourceOptions[SelectedImportSourceIndex].Type
        : DataImportType.None;

    public IReactiveCommand BrowseFolderCommand { get; }
    public IReactiveCommand ImportCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ImportDataViewModel()
    {
        BrowseFolderCommand = ReactiveCommand.CreateFromTask(BrowseFolder);
        ImportCommand = ReactiveCommand.CreateFromTask(Import);
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);

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
            TopLevelProvider.Current,
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

        var copyAssetData = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.CopyAssetData]
        );

        async Task ProgressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                MainWindowViewModel.Instance.ProgressVM.Update(
                    Localizer.Instance.Get(tuple.localizationKey, tuple.progress.ToString()),
                    tuple.progress
                );
            });
        }

        var request = new ImportRequest
        {
            ImportType = type,
            DataFolderPath = FolderPath,
            CopyAssetData = copyAssetData,
            ReportProgress = ProgressAction
        };

        MainWindowViewModel.Instance.ProgressVM.Open(Localizer.Instance[Loc.Processing.Import.Copying]);
        var result = await AvatarExplorerApp.Instance.ItemGroupService.Import(request);
        MainWindowViewModel.Instance.ProgressVM.Close();

        if (result.IsError)
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ImportFailed],
                NotificationType.Error
            );
            return;
        }

        MainWindowViewModel.ShowNotification(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Success.Import],
            NotificationType.Success
        );

        IsVisible = false;
    }
}
