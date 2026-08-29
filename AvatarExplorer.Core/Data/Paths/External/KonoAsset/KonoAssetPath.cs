namespace AvatarExplorer.Core.Data.Paths.External.KonoAsset;

/// <summary>
/// KonoAsset のデータフォルダ内の各種パスを取得するための静的クラス。
/// </summary>
public static class KonoAssetPath
{
    /// <summary>
    /// サムネイル画像フォルダのパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>サムネイルフォルダのパス。</returns>
    public static string ThumbnailsPath(string dataFolderPath) => Path.Combine(dataFolderPath, "images");

    /// <summary>
    /// アセットデータフォルダのパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>データフォルダのパス。</returns>
    public static string DataPath(string dataFolderPath) => Path.Combine(dataFolderPath, "data");

    /// <summary>
    /// メタデータフォルダのパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>メタデータフォルダのパス。</returns>
    public static string MetadataPath(string dataFolderPath) => Path.Combine(dataFolderPath, "metadata");

    /// <summary>
    /// アバターのデータベースファイル（avatars.json）のパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>avatars.json のパス。</returns>
    public static string AvatarsDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "avatars.json");

    /// <summary>
    /// アバター衣装のデータベースファイル（avatarWearables.json）のパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>avatarWearables.json のパス。</returns>
    public static string AvatarWearablesDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "avatarWearables.json");

    /// <summary>
    /// ワールドオブジェクトのデータベースファイル（worldObjects.json）のパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>worldObjects.json のパス。</returns>
    public static string WorldObjectsDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "worldObjects.json");

    /// <summary>
    /// その他アセットのデータベースファイル（otherAssets.json）のパスを取得します。
    /// </summary>
    /// <param name="dataFolderPath">KonoAsset のデータフォルダパス。</param>
    /// <returns>otherAssets.json のパス。</returns>
    public static string OtherAssetsDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "otherAssets.json");
}
