using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Updates;

namespace AvatarExplorer.Core.Models.System;

/// <summary>
/// AvatarExplorer.Core の実行時設定を保持するレコード。with 式でコピーして変更します。
/// </summary>
public record RuntimeSettings
{
    /// <summary>
    /// アイテムデータの保存先ルートディレクトリ。
    /// </summary>
    public string DataRootDirectory { get; init; } = SystemPath.DefaultItemsFolderPath;

    /// <summary>
    /// 自動バックアップの保存先ディレクトリ。
    /// </summary>
    public string AutoBackupRootDirectory { get; init; } = SystemPath.BackupFolderPath;

    /// <summary>
    /// インポート時に元のファイルを削除するかどうか。
    /// </summary>
    public bool RemoveOriginal { get; init; } = false;

    /// <summary>
    /// 元のファイルへのリンクを作成するかどうか（true でコピーせずリンク）。
    /// </summary>
    public bool ShouldLinkToOriginal { get; init; } = false;

    /// <summary>
    /// 自動バックアップの間隔（日数）。
    /// </summary>
    public int AutoBackupInterval { get; init; } = 5;

    /// <summary>
    /// 対応アバターが空の場合に「なし」として扱うかどうか。
    /// </summary>
    public bool TreatEmptySupportedAvatarAsNone { get; init; } = false;

    /// <summary>
    /// 処理の最大並列数。
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 4;

    /// <summary>
    /// Unitypackage のインポートパスを自動変更するかどうか。
    /// </summary>
    public bool AutoChangeUnitypackagePath { get; init; } = true;

    /// <summary>
    /// アップデートを自動チェックするかどうか。
    /// </summary>
    public bool CheckForUpdate { get; init; } = true;

    /// <summary>
    /// チェック対象のアップデートチャンネル。
    /// </summary>
    public UpdateChannel UpdateChannel { get; init; } = UpdateChannel.Stable;
}
