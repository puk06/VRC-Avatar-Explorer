namespace AvatarExplorer.Core.Data.Paths.External.V1;

public static class SystemPathV1
{
    public static string ItemThumbnailsPath(string dataFolderPath) => Path.Combine(dataFolderPath, "Thumbnail");
    public static string ItemsPath(string dataFolderPath) => Path.Combine(dataFolderPath, "Items");
    public static string ItemDatabasePath(string dataFolderPath) => Path.Combine(dataFolderPath, "ItemsData.json");
    public static string CommonAvatarDatabasePath(string dataFolderPath) => Path.Combine(dataFolderPath, "CommonAvatar.json");
}
