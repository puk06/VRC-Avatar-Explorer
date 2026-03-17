using System.Text.Json;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Updates;

public static class UpdateChecker
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { PropertyNameCaseInsensitive = true };

    public async static Task<UpdateManifest?> GetUpdateManifest()
    {
        try
        {
            string response = await HttpService.Client.GetStringAsync(SoftwareLink.UpdateCheckURL);
            return JsonSerializer.Deserialize<UpdateManifest>(response, JsonSerializerOptions);
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
            UpdateManifest? updateManifest = await GetUpdateManifest();
            if (updateManifest == null) return null;

            IEnumerable<VersionRelease> pendingReleases = updateManifest.Releases.GetPendingUpdates();
            if (!pendingReleases.Any()) return null;

            VersionRelease? latestVersion = pendingReleases.GetLatestUpdate(updateChannel);
            if (latestVersion == null) return null;

            VersionRelease latestUpdateReleaseInfo = new()
            {
                Version = latestVersion.Version,
                ReleaseDate = latestVersion.ReleaseDate
            };

            foreach (VersionRelease pendingRelease in pendingReleases)
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
}
