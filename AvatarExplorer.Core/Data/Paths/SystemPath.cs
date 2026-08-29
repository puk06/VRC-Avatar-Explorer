using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Data.Paths;

/// <summary>
/// システムが使用するディレクトリやデータベースファイルのパスを提供する静的クラス。
/// </summary>
public static class SystemPath
{
    /// <summary>
    /// アプリケーションのルートデータディレクトリのパス。
    /// </summary>
    public static readonly string SoftwareDataPath = PathUtils.GetRootPath(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));

    /// <summary>
    /// データベースフォルダのパス。
    /// </summary>
    public static readonly string DatabaseFolderPath = PathUtils.GetDatabaseFolderPath(SoftwareDataPath);

    /// <summary>
    /// 画像フォルダのパス。
    /// </summary>
    public static readonly string ImagesFolderPath = PathUtils.GetImagesFolderPath(SoftwareDataPath);

    /// <summary>
    /// アイテムのサムネイルフォルダのパス。
    /// </summary>
    public static readonly string ItemThumbnailsFolderPath = PathUtils.GetItemThumbnailsFolderPath(SoftwareDataPath);

    /// <summary>
    /// アイテムの既定の保存先フォルダのパス。
    /// </summary>
    public static readonly string DefaultItemsFolderPath = PathUtils.GetItemsFolderPath(SoftwareDataPath);

    /// <summary>
    /// バックアップフォルダのパス。
    /// </summary>
    public static readonly string BackupFolderPath = PathUtils.GetBackupFolderPath(SoftwareDataPath);

    /// <summary>
    /// 設定フォルダのパス。
    /// </summary>
    public static readonly string SettingsFolderPath = PathUtils.GetSettingsFolderPath(SoftwareDataPath);

    /// <summary>
    /// ログフォルダのパス。
    /// </summary>
    public static readonly string LogsFolderPath = PathUtils.GetLogsFolderPath(SoftwareDataPath);

    /// <summary>
    /// 一時フォルダのパス（{TempFolder}/Avatar Explorer V2）。
    /// </summary>
    public static readonly string TempFolderPath = PathUtils.GetRootPath(Path.GetTempPath()); // {TempFolder}/Avatar Explorer V2

    /// <summary>
    /// アイテムデータベースファイルのパス。
    /// </summary>
    public static readonly string ItemDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.Items);

    /// <summary>
    /// 共通素体データベースファイルのパス。
    /// </summary>
    public static readonly string CommonAvatarDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.CommonAvatars);

    /// <summary>
    /// 一括インポートプリセットデータベースファイルのパス。
    /// </summary>
    public static readonly string BulkImportPresetDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.BulkImportPresets);

    /// <summary>
    /// 仮アバターデータベースファイルのパス。
    /// </summary>
    public static readonly string TempAvatarsDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.TempAvatars);

    /// <summary>
    /// バリエーションハッシュデータベースファイルのパス。
    /// </summary>
    public static readonly string VariationHashDatabasePath = Path.Join(DatabaseFolderPath, SystemFileName.Database.VariationHashes);

    /// <summary>
    /// 実行時設定ファイルのパス。
    /// </summary>
    public static readonly string RuntimeSettingsFilePath = Path.Join(SettingsFolderPath, SystemFileName.Settings.Runtime);

    /// <summary>
    /// スキーム（データ形式）バックアップフォルダのパス。
    /// </summary>
    public static readonly string SchemeBackupFolderPath = Path.Join(SettingsFolderPath, "SchemeBackups");
}
