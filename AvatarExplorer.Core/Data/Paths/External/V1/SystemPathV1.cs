namespace AvatarExplorer.Core.Data.Paths.External.V1;

/// <summary>
/// AvatarExplorer V1 のデータフォルダ内の各種パスを取得するための静的クラス。
/// </summary>
public static class SystemPathV1
{
    /// <summary>
    /// アイテムのサムネイルフォルダのパスを取得します。
    /// </summary>
    /// <param name="root">V1 のデータルートパス。</param>
    /// <returns>Thumbnail フォルダのパス。</returns>
    public static string ItemThumbnailsPath(string root) => Path.Combine(root, "Thumbnail");

    /// <summary>
    /// アイテムフォルダのパスを取得します。
    /// </summary>
    /// <param name="root">V1 のデータルートパス。</param>
    /// <returns>Items フォルダのパス。</returns>
    public static string ItemsFolderPath(string root) => Path.Combine(root, "Items");

    /// <summary>
    /// アイテムデータベースファイル（ItemsData.json）のパスを取得します。
    /// </summary>
    /// <param name="root">V1 のデータルートパス。</param>
    /// <returns>ItemsData.json のパス。</returns>
    public static string ItemDatabasePath(string root) => Path.Combine(root, "ItemsData.json");

    /// <summary>
    /// 共通素体データベースファイル（CommonAvatar.json）のパスを取得します。
    /// </summary>
    /// <param name="root">V1 のデータルートパス。</param>
    /// <returns>CommonAvatar.json のパス。</returns>
    public static string CommonAvatarDatabasePath(string root) => Path.Combine(root, "CommonAvatar.json");
}
