using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Updates;

public static class UpdateChecker
{
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

            var pendingReleases = updateManifest.Releases.GetPendingUpdates();
            if (!pendingReleases.Any()) return null;

            var latestVersion = pendingReleases.GetLatestUpdate(updateChannel);
            if (latestVersion == null) return null;

            var latestUpdateReleaseInfo = new VersionRelease()
            {
                Version = latestVersion.Version,
                ReleaseDate = latestVersion.ReleaseDate,
                ReleaseUrl = latestVersion.ReleaseUrl
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
}
