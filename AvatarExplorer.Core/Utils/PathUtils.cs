namespace AvatarExplorer.Core.Utils;

public static class PathUtils
{
    public static string GetSoftwareFolderPath(string path) => Path.Combine(path, "Avatar Explorer V2");
    public static string GetDataFolderPath(string softwarePath) => Path.Combine(softwarePath, "database");
    public static string GetBackupFolderPath(string softwarePath) => Path.Combine(softwarePath, "backups");
    public static string GetImagesFolderPath(string softwarePath) => Path.Combine(softwarePath, "images");
    public static string GetItemsFolderPath(string softwarePath) => Path.Combine(softwarePath, "items");
    public static string GetSettingsFolderPath(string softwarePath) => Path.Combine(softwarePath, "settings");
    public static string GetLogsFolderPath(string softwarePath) => Path.Combine(softwarePath, "logs");
    public static string GetItemThumbnailsFolderPath(string softwarePath) => Path.Combine(GetImagesFolderPath(softwarePath), "item_thumbnails");

    public static bool IsUnitypackageFile(string filePath) => Path.GetExtension(filePath).Equals(".unitypackage", StringComparison.CurrentCultureIgnoreCase);
}
