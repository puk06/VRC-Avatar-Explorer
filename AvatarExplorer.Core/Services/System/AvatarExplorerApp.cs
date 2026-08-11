using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System.Repositories;

namespace AvatarExplorer.Core.Services.System;

public class AvatarExplorerApp
{
    public static readonly string CurrentVersion = "2.7.0-beta.12";

    private static readonly AvatarExplorerApp _instance = new();
    public static AvatarExplorerApp Instance => _instance;

    private bool _initialized = false;

    public ItemRepository Items { get; } = new();
    public CommonAvatarRepository CommonAvatars { get; } = new();
    public TempAvatarRepository TempAvatars { get; } = new();
    public BulkImportPresetRepository BulkImportPresets { get; } = new();
    public ItemGroupService ItemGroupService { get; }
    public ItemNavigationService ItemNavigationService { get; }
    public RuntimeSettingsRepository RuntimeSettings { get; } = new();
    public VariationHashRepository VariationHashes { get; } = new();

    public Func<ArchivePasswordRequest, ValueTask<string?>>? ArchivePasswordProvider { get; set; }

    public readonly BackupManager BackupManager = new();

    private AvatarExplorerApp()
    {
        ItemGroupService = new(Items, CommonAvatars, TempAvatars, RuntimeSettings);
        ItemNavigationService = new(ItemGroupService);
    }

    public void Initialize()
    {
        if (_initialized) return;

        RuntimeSettings.Load();

        Items.Load();
        CommonAvatars.Load();
        TempAvatars.Load();
        BulkImportPresets.Load();
        VariationHashes.Load();

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

        RuntimeSettings.OnSettingsChanged += OnRuntimeSettingsUpdated;
        BackupManager.StartAutoBackup(RuntimeSettings.Settings.AutoBackupInterval, RuntimeSettings.Settings.AutoBackupRootDirectory);

        ErrorManager.Instance.OnErrorOccured += ErrorLogWriter.Instance.Write;
        ErrorManager.Instance.OnInternalErrorOccured += ErrorLogWriter.Instance.InternalWrite;

        _initialized = true;
    }

    public void OnBackupRestored()
    {
        RuntimeSettings.Load();

        Items.Load();
        CommonAvatars.Load();
        TempAvatars.Load();
        BulkImportPresets.Load();
        VariationHashes.Load();

        ItemGroupService.RebuildIndices();
    }

    public void OnRuntimeSettingsUpdated(RuntimeSettings runtimeSettings)
    {
        BackupManager.SetAutoBackupInterval(runtimeSettings.AutoBackupInterval);
        BackupManager.SetAutoBackupPath(runtimeSettings.AutoBackupRootDirectory);
    }

    public static void ClearTemp() => FileSystemService.DeleteDirectory(SystemPath.TempFolderPath);
}
