namespace AvatarExplorer.Core.Data.Paths.External.KonoAsset;

public static class KonoAssetPath
{
    public static string ThumbnailsPath(string dataFolderPath) => Path.Combine(dataFolderPath, "images");
    public static string ItemsPath(string dataFolderPath) => Path.Combine(dataFolderPath, "data");
    public static string AvatarsDatabasePath(string dataFolderPath) => Path.Combine(dataFolderPath, "metadata", "avatars.json");
    public static string AvatarWearablesDatabasePath(string dataFolderPath) => Path.Combine(dataFolderPath, "metadata", "avatarWearables.json");
    public static string WorldObjectsDatabasePath(string dataFolderPath) => Path.Combine(dataFolderPath, "metadata", "worldObjects.json");
}
