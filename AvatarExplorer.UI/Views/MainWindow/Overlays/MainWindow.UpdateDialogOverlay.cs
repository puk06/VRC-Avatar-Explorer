using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.Updates;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private string? _updateDialogOverlay_latestVersion = null;
    private string? _updateDialogOverlay_latestReleaseUrl = null;
    
    private async Task UpdateDialogOverlay_CheckAsync(UpdateChannel updateChannel = UpdateChannel.Stable, bool silent = true)
    {
        VersionRelease? latestVersionRelease = await UpdateChecker.GetLatestUpdateReleaseInfo(updateChannel);
        if (latestVersionRelease == null && !silent)
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.UpdateDialog.NoUpdateAvailableTitle], Localizer.Instance.Get(LocalizationKey.UpdateDialog.NoUpdateAvailable, AvatarExplorerApp.CurrentVersion));
        }
        else if (latestVersionRelease != null)
        {
            UpdateDialogOverlay_Show(latestVersionRelease);
            _updateDialogOverlay_latestVersion = latestVersionRelease.Version;
            _updateDialogOverlay_latestReleaseUrl = latestVersionRelease.ReleaseUrl;
        }
    }
    
    private void UpdateDialogOverlay_Show(VersionRelease versionRelease)
    {
        UpdateDialogOverlay_VersionText.Text = Localizer.Instance.Get(LocalizationKey.UpdateDialog.VersionText, [$"v{versionRelease.Version}", $"v{AvatarExplorerApp.CurrentVersion}", versionRelease.ReleaseDate]);
        UpdateDialogOverlay_UpdateContentText.Text = versionRelease.ChangeLogs.ToString();
        UpdateDialogOverlay.IsVisible = true;
    }
    private void UpdateDialogOverlay_Hide() => UpdateDialogOverlay.IsVisible = false;

    #region Event Handler
    private void UpdateDialogOverlay_Later_Click(object? sender, RoutedEventArgs e) => UpdateDialogOverlay_Hide();
    private async void UpdateDialogOverlay_UpdateNow_Click(object? sender, RoutedEventArgs e)
    {
        string targetUrl;

        if (!string.IsNullOrWhiteSpace(_updateDialogOverlay_latestReleaseUrl))
        {
            targetUrl = _updateDialogOverlay_latestReleaseUrl;
        }
        else if (string.IsNullOrEmpty(_updateDialogOverlay_latestVersion))
        {
            targetUrl = SoftwareLink.LatestReleasePageURL;
        }
        else
        {
            targetUrl = string.Format(SoftwareLink.ReleasePageURL, _updateDialogOverlay_latestVersion);
        }

        await LauncherService.OpenUri(this, targetUrl);
        UpdateDialogOverlay_Hide();
    }
    #endregion
}
