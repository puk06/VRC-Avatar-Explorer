using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void SelectImportTypeOverlay_Show()
    {
        // SelectImportTypeOverlay.IsVisible = true;
    }
    private void SelectImportTypeOverlay_Hide()
    {
        // SelectImportTypeOverlay.IsVisible = false;
    }

    private async Task SelectImportTypeOverlay_DataImportInternal(DataImportType dataImportType)
    {
        var folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false);
        if (folders == null || folders.Length == 0) return;

        var selectedFolder = folders[0];

        // SelectImportTypeOverlay.IsVisible = false;
        
        async Task progressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressOverlay_Show(Localizer.Instance.Get(tuple.localizationKey, tuple.progress.ToString()));
                ProgressOverlay_Update(tuple.progress);
            });
        }

        var localizedItemTypesMapping = Enum.GetValues<ItemType>().ToDictionary(i => i, i => Localizer.Instance[i.GetLocalizationKey() ?? i.ToString()]);

        var copyAssetData = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.CopyAssetData]);
        if (copyAssetData == null) return;

        var shouldCopyAssetData = copyAssetData == YesNoResult.Yes;

        var result = await AvatarExplorer.Import(dataImportType, selectedFolder, localizedItemTypesMapping, shouldCopyAssetData, progressAction);
        ProgressOverlay_Hide();

        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to import data.", tag: result.Errors.ToErrorString());
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ImportFailed], isError: true);
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
