namespace AvatarExplorer.Core.Utils;

/// <summary>
/// アプリが使用する各種フォルダパスの構築や、ファイル種別の判定、パスのハッシュ計算を行うユーティリティを提供します。
/// </summary>
public static class PathUtils
{
    /// <summary>
    /// ベースパス配下のアプリルートフォルダ（Avatar Explorer V2）のパスを取得します。
    /// </summary>
    /// <param name="basePath">ベースとなるディレクトリパス。</param>
    /// <returns>アプリルートフォルダのパス。</returns>
    public static string GetRootPath(string basePath) => Path.Combine(basePath, "Avatar Explorer V2");

    /// <summary>アプリルート配下の database フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>database フォルダのパス。</returns>
    public static string GetDatabaseFolderPath(string root) => Path.Combine(root, "database");
    /// <summary>アプリルート配下の backups フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>backups フォルダのパス。</returns>
    public static string GetBackupFolderPath(string root) => Path.Combine(root, "backups");
    /// <summary>アプリルート配下の images フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>images フォルダのパス。</returns>
    public static string GetImagesFolderPath(string root) => Path.Combine(root, "images");
    /// <summary>アプリルート配下の items フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>items フォルダのパス。</returns>
    public static string GetItemsFolderPath(string root) => Path.Combine(root, "items");
    /// <summary>アプリルート配下の settings フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>settings フォルダのパス。</returns>
    public static string GetSettingsFolderPath(string root) => Path.Combine(root, "settings");
    /// <summary>アプリルート配下の logs フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>logs フォルダのパス。</returns>
    public static string GetLogsFolderPath(string root) => Path.Combine(root, "logs");

    /// <summary>アプリルート配下の images/item_thumbnails フォルダのパスを取得します。</summary>
    /// <param name="root">アプリルートフォルダのパス。</param>
    /// <returns>アイテムサムネイル用フォルダのパス。</returns>
    public static string GetItemThumbnailsFolderPath(string root) => Path.Combine(GetImagesFolderPath(root), "item_thumbnails");

    /// <summary>指定したファイルが .unitypackage ファイルかどうかを拡張子で判定します。</summary>
    /// <param name="filePath">判定対象のファイルパス。</param>
    /// <returns>.unitypackage の場合は true。</returns>
    public static bool IsUnitypackageFile(string filePath) => Path.GetExtension(filePath).Equals(".unitypackage", StringComparison.CurrentCultureIgnoreCase);
    /// <summary>指定したファイルが .pdf ファイルかどうかを拡張子で判定します。</summary>
    /// <param name="filePath">判定対象のファイルパス。</param>
    /// <returns>.pdf の場合は true。</returns>
    public static bool IsPdfFile(string filePath) => Path.GetExtension(filePath).Equals(".pdf", StringComparison.CurrentCultureIgnoreCase);

    /// <summary>
    /// 指定したパス文字列の FNV-1a ハッシュを計算し、8桁の16進数文字列として返します。
    /// </summary>
    /// <param name="path">ハッシュ化するパス文字列。</param>
    /// <returns>8桁の大文字16進数ハッシュ文字列。</returns>
    public static string ComputeHash(string path)
    {
        unchecked
        {
            const uint basis = 2166136261;
            const uint prime = 16777619;
            uint hash = basis;

            foreach (var c in path)
            {
                hash ^= (uint)c;
                hash *= prime;
            }

            return hash.ToString("X8");
        }
    }
}
