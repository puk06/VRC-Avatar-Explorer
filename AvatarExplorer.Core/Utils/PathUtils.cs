namespace AvatarExplorer.Core.Utils;

public static class PathUtils
{
    public static string GetRootPath(string basePath) => Path.Combine(basePath, "Avatar Explorer V2");

    public static string GetDatabaseFolderPath(string root) => Path.Combine(root, "database");
    public static string GetBackupFolderPath(string root) => Path.Combine(root, "backups");
    public static string GetImagesFolderPath(string root) => Path.Combine(root, "images");
    public static string GetItemsFolderPath(string root) => Path.Combine(root, "items");
    public static string GetSettingsFolderPath(string root) => Path.Combine(root, "settings");
    public static string GetLogsFolderPath(string root) => Path.Combine(root, "logs");

    public static string GetItemThumbnailsFolderPath(string root) => Path.Combine(GetImagesFolderPath(root), "item_thumbnails");

    public static bool IsUnitypackageFile(string filePath) => Path.GetExtension(filePath).Equals(".unitypackage", StringComparison.CurrentCultureIgnoreCase);
}
