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
    public static readonly string CurrentVersion = "2.8.0";

    public static AvatarExplorerApp Instance { get; } = new();

    private bool _initialized = false;

    public ItemRepository ItemRepository { get; } = new();
    public CommonAvatarRepository CommonAvatarRepository { get; } = new();
    public TempAvatarRepository TempAvatarRepository { get; } = new();
    public BulkImportPresetRepository BulkImportPresetRepository { get; } = new();
    public ItemGroupService ItemGroupService { get; }
    public ItemNavigationService ItemNavigationService { get; }
    public RuntimeSettingsRepository RuntimeSettingsRepository { get; } = new();
    public VariationHashRepository VariationHashRepository { get; } = new();

    // Aliases for easier access
    public RuntimeSettings RuntimeSettings => RuntimeSettingsRepository.Settings;

    public Func<ArchivePasswordRequest, ValueTask<string?>>? ArchivePasswordProvider { get; set; }

    public readonly BackupManager BackupManager = new();

    private AvatarExplorerApp()
    {
        ItemGroupService = new(ItemRepository, CommonAvatarRepository, TempAvatarRepository, RuntimeSettingsRepository);
        ItemNavigationService = new(ItemGroupService);
    }

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

    public void OnRuntimeSettingsUpdated(RuntimeSettings runtimeSettings)
    {
        BackupManager.SetAutoBackupInterval(runtimeSettings.AutoBackupInterval);
        BackupManager.SetAutoBackupPath(runtimeSettings.AutoBackupRootDirectory);
    }

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
