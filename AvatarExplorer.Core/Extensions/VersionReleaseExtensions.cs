using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Extensions;

public static class VersionReleaseExtensions
{
    public static IEnumerable<VersionRelease> GetPendingUpdates(this IEnumerable<VersionRelease> versionReleases, UpdateChannel updateChannel)
    {
        try
        {
            List<VersionRelease> filteredReleases = versionReleases.ToList();
            filteredReleases.RemoveAll(i => updateChannel == UpdateChannel.Stable && i.Version.Contains("beta"));

            SemanticVersioning.Range versionRange = new SemanticVersioning.Range($">{AvatarExplorerApp.CurrentVersion}");
            return filteredReleases.Where(i => versionRange.IsSatisfied(i.Version));
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to retrieve the latest update information for the '{updateChannel}' channel. Please check if the version strings in the data source follow SemVer format.", ex);
            return [];
        }
    }

    public static VersionRelease? GetLatestUpdate(this IEnumerable<VersionRelease> versionReleases, UpdateChannel updateChannel)
    {
        try
        {
            List<VersionRelease> filteredReleases = versionReleases.ToList();
            filteredReleases.RemoveAll(i => updateChannel == UpdateChannel.Stable && i.Version.Contains("beta"));

            SemanticVersioning.Range latestVersionRange = new SemanticVersioning.Range($">{AvatarExplorerApp.CurrentVersion}");
            VersionRelease? latestVersionRelease = null;

            foreach (VersionRelease versionRelease in filteredReleases)
            {
                if (latestVersionRange.IsSatisfied(versionRelease.Version))
                {
                    latestVersionRange = new SemanticVersioning.Range($">={versionRelease.Version}");
                    latestVersionRelease = versionRelease;
                }
            }

            return latestVersionRelease;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to retrieve the latest update information for the '{updateChannel}' channel. Please check if the version strings in the data source follow SemVer format.", ex);
            return null;
        }
    }
}
