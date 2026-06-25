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
    private void SelectThumbnailImportTypeOverlay_Show() => SelectThumbnailImportTypeOverlay.IsVisible = true;
    private void SelectThumbnailImportTypeOverlay_Hide() => SelectThumbnailImportTypeOverlay.IsVisible = false;

    private async Task SelectThumbnailImportTypeOverlay_ImportInternal(ThumbnailImportType importType)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false);
        if (folders == null || folders.Length == 0) return;

        string selectedFolder = folders[0];

        SelectThumbnailImportTypeOverlay_Hide();

        async Task progressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressOverlay_Show(Localizer.Instance.Get(tuple.localizationKey, tuple.progress.ToString()));
                ProgressOverlay_Update(tuple.progress);
            });
        }

        ErrorOr<Success> result = await AvatarExplorer.ImportThumbnail(importType, selectedFolder, progressAction);
        ProgressOverlay_Hide();

        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to import thumbnail.", tag: result.Errors.ToErrorString());
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ImportFailed], isError: true);
            return;
        }

        Main_ReloadCurrentWindow();
    }

    #region Event Handler
    private void SelectThumbnailImportTypeOverlay_Cancel_Click(object? sender, RoutedEventArgs e) => SelectThumbnailImportTypeOverlay_Hide();
    private async void SelectThumbnailImportTypeOverlay_FromV1_Click(object? sender, RoutedEventArgs e) => await SelectThumbnailImportTypeOverlay_ImportInternal(ThumbnailImportType.V1);
    private async void SelectThumbnailImportTypeOverlay_FromKonoAsset_Click(object? sender, RoutedEventArgs e) => await SelectThumbnailImportTypeOverlay_ImportInternal(ThumbnailImportType.KonoAsset);
    #endregion
}
