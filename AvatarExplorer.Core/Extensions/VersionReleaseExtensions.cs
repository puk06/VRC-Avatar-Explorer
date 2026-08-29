using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Extensions;

/// <summary>
/// <see cref="VersionRelease"/> の列挙に対する更新関連の拡張メソッドを提供します。
/// </summary>
public static class VersionReleaseExtensions
{
    /// <summary>
    /// 指定した更新チャンネルに対して適用可能な（現在のバージョンより新しい）更新を取得します。Stable チャンネルではベータ版を除外します。
    /// </summary>
    /// <param name="versionReleases">バージョンリリースの列挙。</param>
    /// <param name="updateChannel">対象の更新チャンネル。</param>
    /// <returns>適用可能な更新の列挙。バージョン文字列が SemVer 形式でない等の例外時は空となります。</returns>
    public static IEnumerable<VersionRelease> GetPendingUpdates(this IEnumerable<VersionRelease> versionReleases, UpdateChannel updateChannel)
    {
        try
        {
            SemanticVersioning.Range versionRange = new($">{AvatarExplorerApp.CurrentVersion}");
            return versionReleases.Where(i => (updateChannel != UpdateChannel.Stable || !i.Version.Contains("beta")) && versionRange.IsSatisfied(i.Version, includePrerelease: true));
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to retrieve pending updates for the '{updateChannel}' channel. Please check if the version strings in the data source follow SemVer format.", ex);
            return [];
        }
    }

    /// <summary>
    /// 指定したバージョンリリースのうち、現在のバージョンより新しい最新のリリースを取得します。
    /// </summary>
    /// <param name="versionReleases">バージョンリリースの列挙。</param>
    /// <returns>最新の更新リリース。該当がない、または例外発生時は null。</returns>
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
            ErrorManager.Instance.PostInternalError("Failed to retrieve the latest update information. Please check if the version strings in the data source follow SemVer format.", ex);
            return null;
        }
    }
}
