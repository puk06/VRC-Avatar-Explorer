namespace AvatarExplorer.Core.Models.External.V1;

/// <summary>
/// AvatarExplorer V1 形式における共通素体グループのデータを表します。
/// </summary>
public class CommonAvatarV1
{
    /// <summary>
    /// 共通素体グループの名前。
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// グループに含まれるアバターの識別子一覧。
    /// </summary>
    public List<string> Avatars { get; set; } = [];
}
