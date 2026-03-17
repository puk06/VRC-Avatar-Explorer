using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.Utilities;
using ErrorOr;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void SelectImportTypeOverlay_Show() => SelectImportTypeOverlay.IsVisible = true;
    private void SelectImportTypeOverlay_Hide() => SelectImportTypeOverlay.IsVisible = false;

    private async Task SelectImportTypeOverlay_DataImportInternal(DataImportType dataImportType)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false);
        if (folders == null || folders.Length == 0) return;

        string selectedFolder = folders[0];

        SelectImportTypeOverlay.IsVisible = false;
        
        async Task progressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressOverlay_Show(Localizer.Instance.Get(tuple.localizationKey, tuple.progress.ToString()));
                ProgressOverlay_Update(tuple.progress);
            });
        }

        ErrorOr<Success> result = await AvatarExplorer.Import(dataImportType, selectedFolder, progressAction);

        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to import data.", tag: result.Errors.ToErrorString());
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ImportFailed]);
        }
        else
        {
            Main_ReloadCurrentWindow();
        }
    }

    #region Event Handler
    private void SelectImportTypeOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => SelectImportTypeOverlay_Hide();
    private async void SelectImportTypeOverlay_FromV1_Click(object? sender, RoutedEventArgs e) => await SelectImportTypeOverlay_DataImportInternal(DataImportType.V1);
    private async void SelectImportTypeOverlay_FromKonoAsset_Click(object? sender, RoutedEventArgs e) => await SelectImportTypeOverlay_DataImportInternal(DataImportType.KonoAsset);
    #endregion
}
