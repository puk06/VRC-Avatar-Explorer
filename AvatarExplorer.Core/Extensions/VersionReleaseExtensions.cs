using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Extensions;

public static class VersionReleaseExtensions
{
    public static IEnumerable<VersionRelease> GetPendingUpdates(this IEnumerable<VersionRelease> versionReleases, UpdateChannel updateChannel)
    {
        try
        {
            SemanticVersioning.Range versionRange = new($">{AvatarExplorerApp.CurrentVersion}");
            return versionReleases
                .Where(i => updateChannel != UpdateChannel.Stable || !i.Version.Contains("beta"))
                .Where(i => versionRange.IsSatisfied(i.Version, includePrerelease: true));
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to retrieve pending updates for the '{updateChannel}' channel. Please check if the version strings in the data source follow SemVer format.", ex);
            return [];
        }
    }

    public static VersionRelease? GetLatestUpdate(this IEnumerable<VersionRelease> versionReleases)
    {
        try
        {
            SemanticVersioning.Range versionRange = new($">{AvatarExplorerApp.CurrentVersion}");

            return versionReleases
                .Where(i => versionRange.IsSatisfied(i.Version, includePrerelease: true))
                .Aggregate<VersionRelease, VersionRelease?>(null, (latestVersionRelease, versionRelease) =>
                    latestVersionRelease == null || new SemanticVersioning.Range($">={latestVersionRelease.Version}").IsSatisfied(versionRelease.Version, includePrerelease: true)
                        ? versionRelease
                        : latestVersionRelease);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to retrieve the latest update information. Please check if the version strings in the data source follow SemVer format.", ex);
            return null;
        }
    }
}
