using System.Runtime.InteropServices;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Updates;

public static class UpdateChecker
{
    // Security: only allow download URLs that point to the official repository's release assets over HTTPS.
    private const string AllowedDownloadHost = "github.com";
    private const string AllowedDownloadPathPrefix = "/puk06/VRC-Avatar-Explorer/releases/download/";

    public static event Action<VersionRelease>? UpdateAvailable;

    public static async Task<bool> CheckForUpdate(UpdateChannel updateChannel)
    {
        var latestRelease = await GetLatestUpdateReleaseInfo(updateChannel);
        if (latestRelease == null) return false;

        UpdateAvailable?.Invoke(latestRelease);
        return true;
    }

    public async static Task<UpdateManifest?> GetUpdateManifest()
    {
        try
        {
            var response = await HttpService.Client.GetStringAsync(SoftwareLink.UpdateCheckURL);
            return JsonManager.Deserialize<UpdateManifest>(response);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to retrieve update information: '{SoftwareLink.UpdateCheckURL}'.", ex);
            return null;
        }
    }

    public static async Task<VersionRelease?> GetLatestUpdateReleaseInfo(UpdateChannel updateChannel)
    {
        try
        {
            var updateManifest = await GetUpdateManifest();
            if (updateManifest == null) return null;

            var pendingReleases = updateManifest.Releases.GetPendingUpdates(updateChannel);
            if (!pendingReleases.Any()) return null;

            var latestVersion = pendingReleases.GetLatestUpdate();
            if (latestVersion == null) return null;

            var latestUpdateReleaseInfo = new VersionRelease()
            {
                Version = latestVersion.Version,
                ReleaseDate = latestVersion.ReleaseDate,
                ReleaseUrl = latestVersion.ReleaseUrl,
                DownloadUrls = latestVersion.DownloadUrls
            };

            foreach (var pendingRelease in pendingReleases)
            {
                latestUpdateReleaseInfo.ChangeLogs.AddRange(pendingRelease.ChangeLogs);
            }

            return latestUpdateReleaseInfo;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to retrieve latest update information.", ex);
            return null;
        }
    }

    /// <summary>
    /// Resolves the download asset (URL + SHA256) that matches the currently running platform/architecture.
    /// Returns null when no matching asset is available (e.g. unsupported architectures),
    /// in which case the caller should fall back to opening the release page.
    /// </summary>
    /// <param name="release">The version release containing download URLs.</param>
    /// <param name="isFlatpak">True if the application is running as a Flatpak package.</param>
    public static DownloadAsset? GetCurrentPlatformDownloadAsset(VersionRelease release, bool isFlatpak = false)
    {
        if (release.DownloadUrls == null || release.DownloadUrls.Count == 0) return null;

        var key = GetCurrentPlatformDownloadKey(isFlatpak);
        if (key == null) return null;

        return release.DownloadUrls.TryGetValue(key, out var asset) ? asset : null;
    }

    /// <summary>
    /// Validates that a download URL is safe to fetch and execute: it must be HTTPS, point to the
    /// official repository's release assets, and have a recognizable installer/archive extension.
    /// This prevents loading/executing arbitrary URLs even if the update manifest is tampered with.
    /// </summary>
    public static bool IsDownloadUrlSafe(string url, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != Uri.UriSchemeHttps) return false;
        if (!parsed.Host.Equals(AllowedDownloadHost, StringComparison.OrdinalIgnoreCase)) return false;
        if (!parsed.AbsolutePath.StartsWith(AllowedDownloadPathPrefix, StringComparison.Ordinal)) return false;

        var fileName = Path.GetFileName(parsed.AbsolutePath);
        if (string.IsNullOrWhiteSpace(fileName)) return false;
        if (fileName.IndexOfAny(['/', '\\']) >= 0) return false;

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".exe" or ".zip" or ".gz" or ".flatpak"))
            return false;

        uri = parsed;
        return true;
    }

    public static bool IsReleaseUrlSafe(string url, out Uri? uri)
    {
        uri = null;
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != Uri.UriSchemeHttps) return false;
        if (!parsed.Host.Equals(AllowedDownloadHost, StringComparison.OrdinalIgnoreCase)) return false;

        uri = parsed;
        return true;
    }

    private static string? GetCurrentPlatformDownloadKey(bool isFlatpak)
    {
        var arch = RuntimeInformation.OSArchitecture;

        // Flatpak builds are distributed as installable bundles (.flatpak).
        if (isFlatpak)
        {
            return arch switch
            {
                Architecture.Arm64 => "flatpak-aarch64",
                Architecture.X64 => "flatpak-x86_64",
                _ => null
            };
        }

        if (OperatingSystem.IsWindows())
            return arch == Architecture.Arm64 ? "win-arm64" : "win-x64";

        if (OperatingSystem.IsMacOS())
            return arch == Architecture.Arm64 ? "osx-arm64" : null;

        if (OperatingSystem.IsLinux())
        {
            if (IsMusl())
                return arch == Architecture.X64 ? "linux-musl-x64" : null;

            return arch switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _ => null
            };
        }

        return null;
    }

    private static bool IsMusl()
    {
        return RuntimeInformation.RuntimeIdentifier?.Contains("musl") is true;
    }
}
