namespace AvatarExplorer.Core.Models.Updates;

/// <summary>
/// アップデートのバージョンマニフェスト。公開されているリリース一覧を保持します。
/// </summary>
public class UpdateManifest
{
    /// <summary>
    /// 公開されているバージョンリリースの一覧。
    /// </summary>
    public List<VersionRelease> Releases { get; set; } = [];
}
