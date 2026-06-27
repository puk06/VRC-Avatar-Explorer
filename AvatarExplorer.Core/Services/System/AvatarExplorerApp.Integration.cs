using System.Collections.Immutable;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

public partial class AvatarExplorerApp
{
    #region Booth API
    private DateTime _lastBoothApiGetTime;
    public bool IsApiCooldownNow => _lastBoothApiGetTime.AddSeconds(2) > DateTime.Now;
    public async Task WaitForApiCooldownAsync(int pollingIntervalMs = 100, CancellationToken cancellationToken = default)
    {
        if (pollingIntervalMs < 10) pollingIntervalMs = 10;

        while (IsApiCooldownNow)
        {
            await Task.Delay(pollingIntervalMs, cancellationToken);
        }
    }
    public async Task<ErrorOr<BoothItem>> GetBoothItem(string boothUrl)
    {
        if (string.IsNullOrEmpty(boothUrl)) return Error.Failure(description: "Invalid Url.");
        if (IsApiCooldownNow) return Error.Failure(description: "Booth API Cooldown Error.");

        string boothId = boothUrl.Split('/')[^1];

        _lastBoothApiGetTime = DateTime.Now;

        ErrorOr<BoothItem> result = await BoothService.GetItem(boothId);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch booth item.", tag: result.Errors.ToErrorString());
            return Error.Failure(description: "Failed to fetch booth item.");
        }

        return result.Value;
    }
    public async Task<ErrorOr<Success>> FetchAndUpdateThumbnailImage(string itemId)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        if (item.BoothId == -1) return Error.Validation(description: "Booth id not found.");

        if (IsApiCooldownNow) return Error.Failure(description: "API is on cooldown.");

        _lastBoothApiGetTime = DateTime.Now;
        ErrorOr<BoothItem> fetchResult = await BoothService.GetItem(item.BoothId.ToString());
        if (fetchResult.IsError) return Error.Failure(description: fetchResult.Errors.ToErrorString());

        bool result = await ImageDownloader.Fetch(fetchResult.Value.ThumbnailUrl, Path.Combine(SystemPath.ItemThumbnailsFolderPath, item.Id), true);
        if (!result) return Error.Failure(description: "Failed to fetch thumbnail.");

        item.ThumbnailFileName = item.Id;

        SaveItemDatabase();

        return Result.Success;
    }
    #endregion

    #region File API
    public static async Task<ModifiedUnitypackagesResult> ModifyUnitypackageFilePaths(Dictionary<string, string> itemPathCategoryDictionary, Func<(string, int), Task>? reportProgress = null) => await FileSystemService.ModifyUnitypackageFilePathsAsync(itemPathCategoryDictionary, reportProgress);
    #endregion

    #region Search API
    public ImmutableArray<Item> SearchItems(SearchFilter searchFilter)
    {
        SearchContext searchContext = new()
        {
            Items = _itemDatabaseManager.Items,
            CommonAvatars = _commonAvatarDatabaseManager.Items,
            TempAvatars = _tempAvatarsDatabaseManager.Items,
            SearchIndexDictionary = _itemSearchIndexDictionary,
            RuntimeSettings = RuntimeSettings
        };

        return ItemSearchService.ExecuteSearch(searchContext, searchFilter);
    }
    #endregion

    #region Data Importer API
    public async Task<ErrorOr<Success>> Import(DataImportType importType, string dataFolderPath, Dictionary<ItemType, string> localizedItemTypesMapping, bool copyAssetData, Func<(string, int), Task>? reportProgress = null)
    {
        ImportRequest importRequest = new()
        {
            ImportType = importType,
            DataFolderPath = dataFolderPath,
            LocalizedItemTypesMapping = localizedItemTypesMapping,
            CopyAssetData = copyAssetData,
            RuntimeSettings = RuntimeSettings,
            ReportProgress = reportProgress
        };

        ErrorOr<DataImportResult> result = await DataImporter.Import(importRequest);
        if (result.IsError) return Error.Failure(description: result.Errors.ToErrorString());

        _itemDatabaseManager.AddRange(result.Value.Items);
        _commonAvatarDatabaseManager.AddRange(result.Value.CommonAvatars);
        _tempAvatarsDatabaseManager.AddRange(result.Value.TempAvatars);

        UpdateSearchIndex();

        SaveItemDatabase();
        SaveCommonAvatarDatabase();
        SaveTempAvatarsDatabase();

        return Result.Success;
    }

    public async Task<ErrorOr<Success>> ImportThumbnail(ThumbnailImportType importType, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    {
        ErrorOr<Success> result = await DataImporter.ImportThumbnail(importType, _itemDatabaseManager.Items, dataFolderPath, reportProgress);
        if (result.IsError) return result;

        SaveItemDatabase();

        return Result.Success;
    }
    #endregion

    #region Data Exporter API
    public async Task<ErrorOr<Success>> Export(DataExportType exportType, string filePath, Dictionary<ItemType, string> localizedItemTypesMapping, bool includeCommonToSupported)
    {
        ExportContext exportContext = new()
        {
            Items = _itemDatabaseManager.Items,
            CommonAvatars = _commonAvatarDatabaseManager.Items,
            TempAvatars = _tempAvatarsDatabaseManager.Items,
            LocalizedItemTypesMapping = localizedItemTypesMapping,
            RuntimeSettings = RuntimeSettings
        };

        ExportRequest exportRequest = new()
        {
            ExportType = exportType,
            FilePath = filePath,
            IncludeCommonToSupported = includeCommonToSupported
        };

        return await DataExporter.Export(exportContext, exportRequest);
    }
    #endregion

    #region Clear API
    public static void ClearTemp() => FileSystemService.DeleteDirectory(SystemPath.TempFolderPath);
    #endregion

    #region Backup API
    public void StartAutoBackup() => _backupManager.StartAutoBackup(RuntimeSettings.AutoBackupInterval, RuntimeSettings.AutoBackupRootDirectory);
    public async Task StopAutoBackup() => await _backupManager.StopAutoBackup();
    public async Task<ErrorOr<Success>> ExecuteBackup(string path) => await _backupManager.ExecuteBackup(path);
    public async Task RestoreFromBackup(string backupFolderPath)
    {
        string itemDatabaseMigrationPath = Path.Join(backupFolderPath, SystemFileName.Database.ItemsDatabaseMigrationVersion);
        string itemDatabasePath = Path.Join(backupFolderPath, SystemFileName.Database.Items);
        string commonAvatarDatabasePath = Path.Join(backupFolderPath, SystemFileName.Database.CommonAvatars);
        string bulkImportPresetDatabasePath = Path.Join(backupFolderPath, SystemFileName.Database.BulkImportPresets);
        string tempAvatarsDatabasePath = Path.Join(backupFolderPath, SystemFileName.Database.TempAvatars);
        string runtimeSettingsFilePath = Path.Join(backupFolderPath, SystemFileName.Settings.Runtime);

        if (File.Exists(itemDatabasePath))
        {
            if (File.Exists(itemDatabaseMigrationPath))
            {
                int appliedMigrationVersion = ItemDatabaseMigrationService.ReadAppliedMigrationVersion(itemDatabasePath);
                ItemDatabaseMigrationService.WriteAppliedMigrationVersion(_itemDatabaseManager.DatabaseFilePath, appliedMigrationVersion);
            }
            else
            {
                ItemDatabaseMigrationService.WriteAppliedMigrationVersion(_itemDatabaseManager.DatabaseFilePath, 0);
            }

            await FileSystemService.CopyFileAsync(itemDatabasePath, _itemDatabaseManager.DatabaseFilePath);

            LoadItemDatabase();
            SaveItemDatabase();
        }

        if (File.Exists(commonAvatarDatabasePath))
        {
            LoadCommonAvatarDatabase(commonAvatarDatabasePath);
            SaveCommonAvatarDatabase();
        }

        if (File.Exists(bulkImportPresetDatabasePath))
        {
            LoadBulkImportPresetDatabase(bulkImportPresetDatabasePath);
            SaveBulkImportPresetDatabase();
        }

        if (File.Exists(tempAvatarsDatabasePath))
        {
            LoadTempAvatarsDatabase(tempAvatarsDatabasePath);
            SaveTempAvatarsDatabase();
        }

        if (File.Exists(runtimeSettingsFilePath))
        {
            LoadRuntimeSettings(runtimeSettingsFilePath);
            SaveRuntimeSettings();
        }

        UpdateSearchIndex();
        EnsureAllItemsDefaultPathExist();
    }
    #endregion
}
