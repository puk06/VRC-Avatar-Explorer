using System.Diagnostics;
using System.Security.Cryptography;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.Updates;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public partial class UpdateDialogViewModel : ViewModelBase
{
    [Reactive] public partial bool IsVisible { get; set; }

    private string CurrentVersion { get; set; } = string.Empty;
    private string LatestVersion { get; set; } = string.Empty;
    private string ReleaseDate { get; set; } = string.Empty;
    private string ReleaseUrl { get; set; } = string.Empty;
    private VersionRelease? LatestRelease { get; set; }

    [Reactive] public partial string VersionText { get; set; } = string.Empty;
    [Reactive] public partial string Content { get; set; } = string.Empty;

    public IReactiveCommand LaterCommand { get; }
    public IReactiveCommand UpdateNowCommand { get; }
    public IReactiveCommand OpenReleasePageCommand { get; }

    public UpdateDialogViewModel()
    {
        LaterCommand = ReactiveCommand.Create(OnLater);
        UpdateNowCommand = ReactiveCommand.CreateFromTask(UpdateNow);
        OpenReleasePageCommand = ReactiveCommand.CreateFromTask(OpenReleasePageInBrowser);
    }

    public void Open(string currentVersion, VersionRelease latestRelease)
    {
        CurrentVersion = currentVersion;
        LatestVersion = latestRelease.Version;
        ReleaseDate = latestRelease.ReleaseDate;
        ReleaseUrl = latestRelease.ReleaseUrl;
        LatestRelease = latestRelease;
        Content = latestRelease.ChangeLogs.ToString();

        UpdateVersionText();
        IsVisible = true;
    }

    private void UpdateVersionText()
    {
        VersionText = Localizer.Instance.Get(Loc.UpdateDialog.VersionText, [$"v{LatestVersion}", $"v{CurrentVersion}", ReleaseDate]);
    }

    private void OnLater()
    {
        IsVisible = false;
    }

    private async Task UpdateNow()
    {
        // When there is no resolved release, fall back to the release page.
        if (LatestRelease == null)
        {
            await OpenReleasePageInBrowser();
            IsVisible = false;
            return;
        }

        var asset = UpdateChecker.GetCurrentPlatformDownloadAsset(LatestRelease,
#if FLATPAK
            isFlatpak: true
#else
            isFlatpak: false
#endif
        );

        // No matching asset for this platform, or the URL failed safety validation -> fall back to the release page.
        if (asset == null || !UpdateChecker.IsDownloadUrlSafe(asset.Url, out var downloadUri) || downloadUri == null)
        {
            await OpenReleasePageInBrowser();
            IsVisible = false;
            return;
        }

        // Only Windows installers (.exe) are downloaded and executed automatically.
        // Everything else (zip/tar.gz/flatpak bundles) is opened in the browser so the user downloads it manually.
        if (!IsAutoUpdatableInstaller(downloadUri))
        {
            await LauncherService.OpenUri(asset.Url);
            IsVisible = false;
            return;
        }

        // Close the dialog; download progress is reported through the notification manager.
        IsVisible = false;

        try
        {
            await NotificationManager.ShowWithProgress(
                Localizer.Instance[Loc.Processing.SoftwareUpdate.Title],
                async progress =>
                {
                    var downloadPath = await DownloadToFileAsync(downloadUri, asset.Sha256, progress);

                    // Launch the Windows installer and exit so it can replace the in-use binaries.
                    if (ProcessUtils.IsWindows() && downloadPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                    {
                        Process.Start(new ProcessStartInfo(downloadPath) { UseShellExecute = true });
                        Environment.Exit(0);
                    }
                }
            );
        }
        catch (Exception ex)
        {
            // ErrorManager.PostError surfaces the message to the user via the notification manager.
            ErrorManager.Instance.PostError(Localizer.Instance[Loc.Error.DownloadFailed], ex);
        }
    }

    private async Task OpenReleasePageInBrowser()
    {
        if (!UpdateChecker.IsReleaseUrlSafe(ReleaseUrl, out var _))
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.InvalidReleaseUrl],
                NotificationType.Error
            );
            return;
        }

        await LauncherService.OpenUri(ReleaseUrl);
    }

    /// <summary>
    /// Returns true when the asset is an installer that can be downloaded and executed automatically.
    /// Currently only Windows .exe installers are supported; Flatpak bundles are opened in the browser
    /// because automatic installation requires sandbox permissions (flatpak-spawn --host) that may not be granted.
    /// </summary>
    private static bool IsAutoUpdatableInstaller(Uri downloadUri)
    {
        if (!ProcessUtils.IsWindows()) return false;
        return downloadUri.AbsolutePath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<string> DownloadToFileAsync(Uri downloadUri, string expectedSha256, IProgressReporter reporter)
    {
        var fileName = Path.GetFileName(downloadUri.AbsolutePath);
        var targetDir = FileSystemService.GetNewTempFolder();
        var targetPath = Path.Combine(targetDir, fileName);

        var success = await Downloader.Fetch(
            downloadUri.AbsoluteUri,
            targetPath,
            overwrite: true,
            reportProgress: async (pct) =>
            {
                var progress = (int)(pct * 0.9f);
                reporter.Report(Localizer.Instance.Get(Loc.Processing.SoftwareUpdate.Status.Downloading, progress.ToString()), progress);
                await Task.CompletedTask;
            }
        );

        if (!success)
        {
            throw new InvalidOperationException("Download failed.");
        }

        reporter.Report(Localizer.Instance[Loc.Processing.SoftwareUpdate.Status.Validating], 90);

        // Validate the SHA256 hash of the downloaded file.
        if (string.IsNullOrWhiteSpace(expectedSha256) || expectedSha256.Length != 64 || !expectedSha256.All(Uri.IsHexDigit))
        {
            throw new InvalidDataException("SHA256 hash missing or malformed; refusing to execute.");
        }

        // Security: verify the SHA256 hash before allowing execution.
        await using var verifyStream = File.OpenRead(targetPath);
        var hashBytes = await SHA256.HashDataAsync(verifyStream);
        var actualHash = Convert.ToHexString(hashBytes);

        if (!actualHash.Equals(expectedSha256, StringComparison.OrdinalIgnoreCase))
        {
            try { File.Delete(targetPath); } catch { /* best effort */ }
            throw new InvalidDataException($"SHA256 verification failed. Expected '{expectedSha256}', got '{actualHash}'.");
        }

        reporter.Report(Localizer.Instance[Loc.Processing.SoftwareUpdate.Status.Validating], 100);

        return targetPath;
    }
}
