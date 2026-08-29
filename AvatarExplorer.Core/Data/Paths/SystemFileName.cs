namespace AvatarExplorer.Core.Data.Paths;

/// <summary>
/// システムが使用するファイル名の定数を提供します。
/// </summary>
public static class SystemFileName
{
    /// <summary>
    /// データベース関連のファイル名。
    /// </summary>
    public static class Database
    {
        /// <summary>
        /// アイテムデータベースのファイル名。
        /// </summary>
        public const string Items = "items.json";

        /// <summary>
        /// 共通素体データベースのファイル名。
        /// </summary>
        public const string CommonAvatars = "commonAvatars.json";

        /// <summary>
        /// 一括インポートプリセットのファイル名。
        /// </summary>
        public const string BulkImportPresets = "bulkImportPresets.json";

        /// <summary>
        /// 仮アバターデータベースのファイル名。
        /// </summary>
        public const string TempAvatars = "tempAvatars.json";

        /// <summary>
        /// バリエーションハッシュのファイル名。
        /// </summary>
        public const string VariationHashes = "variationHashes.json";
    }

    /// <summary>
    /// 設定関連のファイル名。
    /// </summary>
    public static class Settings
    {
        /// <summary>
        /// 実行時設定のファイル名。
        /// </summary>
        public const string Runtime = "runtimeSettings.json";
    }

    /// <summary>
    /// ライセンスファイル名。
    /// </summary>
    public const string License = "LICENSE";

    /// <summary>
    /// サードパーティライセンスファイル名。
    /// </summary>
    public const string ThirdPartyLicenses = "THIRD_PARTY_LICENSES.md";
}
