using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System.Repositories;

namespace AvatarExplorer.Core.Services.System;

public sealed class AvatarExplorerApp
{
    /// <summary>現在の AvatarExplorer.Core のバージョン文字列。</summary>
    public static readonly string CurrentVersion = "2.9.0-beta.2";

    /// <summary>アプリケーションのシングルトンインスタンス。どこからでも同じインスタンスが返されます。</summary>
    public static AvatarExplorerApp Instance { get; } = new();

    private bool _initialized = false;

    /// <summary>アイテムの CRUD 操作を行うリポジトリ。</summary>
    public ItemRepository ItemRepository { get; } = new();
    /// <summary>共通素体グループの管理を行うリポジトリ。</summary>
    public CommonAvatarRepository CommonAvatarRepository { get; } = new();
    /// <summary>仮アバターの管理を行うリポジトリ。</summary>
    public TempAvatarRepository TempAvatarRepository { get; } = new();
    /// <summary>一括インポートプリセットの管理を行うリポジトリ。</summary>
    public BulkImportPresetRepository BulkImportPresetRepository { get; } = new();
    /// <summary>横断的な操作（検索・削除・インポート/エクスポートなど）を行うサービス。</summary>
    public ItemGroupService ItemGroupService { get; }
    /// <summary>ナビゲーション（選択状態の管理）を行うサービス。</summary>
    public ItemNavigationService ItemNavigationService { get; }
    /// <summary>設定の管理を行うリポジトリ。</summary>
    public RuntimeSettingsRepository RuntimeSettingsRepository { get; } = new();
    /// <summary>Booth バリエーションのハッシュ管理を行うリポジトリ。</summary>
    public VariationHashRepository VariationHashRepository { get; } = new();

    // Aliases for easier access
    /// <summary>現在の設定 (<see cref="RuntimeSettingsRepository.Settings"/>) へのショートカット。</summary>
    public RuntimeSettings RuntimeSettings => RuntimeSettingsRepository.Settings;

    /// <summary>アーカイブ展開時にパスワードを要求するためのプロバイダー。必要に応じて設定してください。</summary>
    public Func<ArchivePasswordRequest, ValueTask<string?>>? ArchivePasswordProvider { get; set; }

    /// <summary>自動バックアップの管理を行うバックアップマネージャー。</summary>
    public readonly BackupManager BackupManager = new();

    private AvatarExplorerApp()
    {
        ItemGroupService = new(ItemRepository, CommonAvatarRepository, TempAvatarRepository, RuntimeSettingsRepository);
        ItemNavigationService = new(ItemGroupService);
    }

    /// <summary>
    /// ライブラリを初期化します。設定・各データベースの読み込み、検索インデックスの構築、
    /// 自動バックアップの開始、エラーハンドリングの設定を行います。必ず最初に一度だけ呼び出してください。
    /// </summary>
    public void Initialize()
    {
        if (_initialized) return;

        RuntimeSettingsRepository.Load();

        ItemRepository.Load();
        CommonAvatarRepository.Load();
        TempAvatarRepository.Load();
        BulkImportPresetRepository.Load();
        VariationHashRepository.Load();

        ItemGroupService.RebuildIndices();

        BackupManager.AddTargetFiles(
            [
                SystemPath.ItemDatabasePath,
                SystemPath.CommonAvatarDatabasePath,
                SystemPath.TempAvatarsDatabasePath,
                SystemPath.BulkImportPresetDatabasePath,
                SystemPath.RuntimeSettingsFilePath,
                SystemPath.VariationHashDatabasePath
            ]
        );
        BackupManager.OnBackupRestored += OnBackupRestored;

        RuntimeSettingsRepository.OnSettingsChanged += OnRuntimeSettingsUpdated;
        BackupManager.StartAutoBackup(RuntimeSettings.AutoBackupInterval, RuntimeSettings.AutoBackupRootDirectory);

        ErrorManager.Instance.OnErrorOccured += ErrorLogWriter.Instance.Write;
        ErrorManager.Instance.OnInternalErrorOccured += ErrorLogWriter.Instance.InternalWrite;

        _initialized = true;
    }

    /// <summary>
    /// バックアップから復元された後に呼び出され、設定・各データベースを再読み込みし検索インデックスを再構築します。
    /// 通常は <see cref="BackupManager"/> のイベントから自動で呼び出されます。
    /// </summary>
    public void OnBackupRestored()
    {
        RuntimeSettingsRepository.Load();

        ItemRepository.Load();
        CommonAvatarRepository.Load();
        TempAvatarRepository.Load();
        BulkImportPresetRepository.Load();
        VariationHashRepository.Load();

        ItemGroupService.RebuildIndices();
    }

    /// <summary>
    /// 設定が変更されたときに呼び出され、バックアップマネージャーの間隔と保存先を最新の設定に同期します。
    /// 通常は <see cref="RuntimeSettingsRepository"/> のイベントから自動で呼び出されます。
    /// </summary>
    /// <param name="runtimeSettings">変更後の設定。</param>
    public void OnRuntimeSettingsUpdated(RuntimeSettings runtimeSettings)
    {
        BackupManager.SetAutoBackupInterval(runtimeSettings.AutoBackupInterval);
        BackupManager.SetAutoBackupPath(runtimeSettings.AutoBackupRootDirectory);
    }

    /// <summary>一時フォルダ内のサブディレクトリをすべて削除します。削除に失敗した場合は内部エラーとして記録されます。</summary>
    public static void ClearTemp()
    {
        if (!Directory.Exists(SystemPath.TempFolderPath)) return;

        try
        {
            var failures = 0;
            Directory.GetDirectories(SystemPath.TempFolderPath)
                .ForEach(dir =>
                {
                    try { Directory.Delete(dir, true); }
                    catch { failures++; }
                });

            if (failures > 0)
            {
                ErrorManager.Instance.PostInternalError($"Failed to delete {failures} temp directories.");
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to clear temp folder.", ex);
        }
    }
}
