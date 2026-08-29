namespace AvatarExplorer.Core.Models.Updates;

/// <summary>
/// アップデートの配信チャンネルを表します。
/// </summary>
public enum UpdateChannel
{
    /// <summary>
    /// 安定版のみ。
    /// </summary>
    Stable,

    /// <summary>
    /// ベータ版も含む。
    /// </summary>
    Beta
}
