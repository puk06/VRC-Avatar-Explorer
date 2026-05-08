namespace AvatarExplorer.Core.Data.Paths.External.KonoAsset;

public static class KonoAssetPath
{
    public static string ThumbnailsPath(string dataFolderPath) => Path.Combine(dataFolderPath, "images");
    public static string DataPath(string dataFolderPath) => Path.Combine(dataFolderPath, "data");
    public static string MetadataPath(string dataFolderPath) => Path.Combine(dataFolderPath, "metadata");
    public static string AvatarsDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "avatars.json");
    public static string AvatarWearablesDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "avatarWearables.json");
    public static string WorldObjectsDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "worldObjects.json");
    public static string OtherAssetsDatabasePath(string dataFolderPath) => Path.Combine(MetadataPath(dataFolderPath), "otherAssets.json");
}
