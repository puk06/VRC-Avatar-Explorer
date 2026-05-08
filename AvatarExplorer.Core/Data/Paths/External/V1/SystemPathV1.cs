namespace AvatarExplorer.Core.Data.Paths.External.V1;

public static class SystemPathV1
{
    public static string ItemThumbnailsPath(string root) => Path.Combine(root, "Thumbnail");
    public static string ItemsFolderPath(string root) => Path.Combine(root, "Items");
    public static string ItemDatabasePath(string root) => Path.Combine(root, "ItemsData.json");
    public static string CommonAvatarDatabasePath(string root) => Path.Combine(root, "CommonAvatar.json");
}
