using System.Collections.Immutable;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.External.Booth;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Items.Internal;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars.Internal;
using AvatarExplorer.Core.Services.Database;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.Network;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

public partial class AvatarExplorerApp
{
    public static readonly string CurrentVersion = "2.0.0-beta.3";

    private readonly ItemDatabaseManager _itemDatabaseManager = new();
    private readonly CommonAvatarDatabaseManager _commonAvatarDatabaseManager = new();
    private readonly BulkImportPresetDatabaseManager _bulkImportPresetDatabaseManager = new();

    private readonly Dictionary<string, string> _itemSearchIndexDictionary = new();

    private readonly SelectionState _selectionState = new();
    private readonly Dictionary<ItemTagStates, Func<SelectionNode, ImmutableArray<ItemCountInfo>>> _stateHandlers;
    private RuntimeSettings _runtimeSettings = new();

    public AvatarExplorerApp()
    {
        _stateHandlers = new()
        {
            { ItemTagStates.SearchItem, HandleRootSelectedItem },
            { ItemTagStates.RootAvatar, HandleRootAvatar },
            { ItemTagStates.RootAuthor, HandleRootAuthor },
            { ItemTagStates.RootCategory, HandleRootCategory },
            { ItemTagStates.RootSelectedCategory, HandleRootSelectedCategory },
            { ItemTagStates.RootSelectedItem, HandleRootSelectedItem },
            { ItemTagStates.ItemFileCategory, HandleItemFileCategory }
        };

        ErrorManager.Instance.OnErrorOccured += ErrorLogWriter.Instance.Write;
        ErrorManager.Instance.OnInternalErrorOccured += ErrorLogWriter.Instance.InternalWrite;
    }

    #region Database
    public void LoadItemDatabase(string? path = null)
    {
        string loadPath = path ?? _itemDatabaseManager.DatabaseFilePath;
        IEnumerable<Item> database = DatabaseService<Item>.Load(loadPath);

        _itemDatabaseManager.Update(database);

        UpdateSearchIndex();
    }

    public void LoadCommonAvatarDatabase(string? path = null)
    {
        string loadPath = path ?? _commonAvatarDatabaseManager.DatabaseFilePath;
        IEnumerable<CommonAvatar> database = DatabaseService<CommonAvatar>.Load(loadPath);

        _commonAvatarDatabaseManager.Update(database);
    }

    public void LoadBulkImportPresetDatabase(string? path = null)
    {
        string loadPath = path ?? _bulkImportPresetDatabaseManager.DatabaseFilePath;
        IEnumerable<BulkImportPreset> database = DatabaseService<BulkImportPreset>.Load(loadPath);

        _bulkImportPresetDatabaseManager.Update(database);
    }

    public void SaveItemDatabase() => DatabaseService<Item>.Save(_itemDatabaseManager.Items, _itemDatabaseManager.DatabaseFilePath);
    public void SaveCommonAvatarDatabase() => DatabaseService<CommonAvatar>.Save(_commonAvatarDatabaseManager.Items, _commonAvatarDatabaseManager.DatabaseFilePath);
    public void SaveBulkImportPresetDatabase() => DatabaseService<BulkImportPreset>.Save(_bulkImportPresetDatabaseManager.Items, _bulkImportPresetDatabaseManager.DatabaseFilePath);

    public void ResetItemDatabase()
    {
        _itemDatabaseManager.Clear();
        SaveItemDatabase();
    }
    public void ResetCommonAvatarDatabase()
    {
        _commonAvatarDatabaseManager.Clear();
        SaveCommonAvatarDatabase();
    }
    #endregion

    #region Runtime Settings
    public void LoadRuntimeSettings(string? path = null)
    {
        string loadPath = path ?? SystemPath.RuntimeSettingsFilePath;
        _runtimeSettings =  RuntimeSettingsService.Load(loadPath);
    }
    #endregion

    #region Update API
    public void UpdateSearchIndex()
    {
        Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(_itemDatabaseManager.Items.Where(i => i.Type == ItemType.Avatar));
        foreach (Item item in _itemDatabaseManager.Items)
        {
            string index = ItemSearchService.BuildItemSearchIndex(item, avatarTitleMaps, _commonAvatarDatabaseManager.Items);
            _itemSearchIndexDictionary[item.Id] = index;
        }
    }
    public void UpdateSearchIndex(string itemId)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return;

        Dictionary<string, string> avatarNameMaps = ItemUtils.GetItemTitleMaps(_itemDatabaseManager.Items.Where(i => i.Type == ItemType.Avatar));
        _itemSearchIndexDictionary[item.Id] = ItemSearchService.BuildItemSearchIndex(item, avatarNameMaps, _commonAvatarDatabaseManager.Items);
    }
    #endregion

    #region Select API
    public void Select(ItemTagStates state, string key) => _selectionState.Push(state, key);
    public void SelectUndo() => _selectionState.Pop();
    public void SelectClear() => _selectionState.Clear();
    #endregion

    #region Get API
    public ImmutableArray<ItemCountInfo> GetAvatars(bool includeCommonAvatar = false) => ItemAvatarAggregator.Aggregate(_itemDatabaseManager.Items, _commonAvatarDatabaseManager.Items, _runtimeSettings, includeCommonAvatar);
    public ImmutableArray<ItemCountInfo> GetAuthors() => ItemAuthorAggregator.Aggregate(_itemDatabaseManager.Items);
    public ImmutableArray<ItemCountInfo> GetCategories(bool includeEmptyCategory = false) => ItemCategoryAggregator.Aggregate(_itemDatabaseManager.Items, includeEmptyCategory);

    public ImmutableArray<Item> GetAllItems() => _itemDatabaseManager.Items;
    public Item? GetItemById(string? itemId)
    {
        if (itemId == null) return null;

        Item? item = _itemDatabaseManager.Items.FirstOrDefault(i => i.Id == itemId);
        if (item == null) ErrorManager.Instance.PostInternalError($"The item with the specified ID '{itemId}' was not found.");

        return item;
    }
    public ImmutableArray<ItemCountInfo> GetItemsForCurrentState()
    {
        SelectionNode? current = _selectionState.Current;
        if (current == null) return [];

        if (_stateHandlers.TryGetValue(current.State, out var handler))
            return handler(current);

        return [];
    }

    public ImmutableArray<CommonAvatar> GetAllCommonAvatars() => _commonAvatarDatabaseManager.Items;
    public CommonAvatar? GetCommonAvatarById(string? groupId)
    {
        if (groupId == null) return null;

        CommonAvatar? commonAvatar = _commonAvatarDatabaseManager.Items.FirstOrDefault(i => i.Id == groupId);
        if (commonAvatar == null) ErrorManager.Instance.PostInternalError($"The common avatar group with the specified ID '{groupId}' was not found.");

        return commonAvatar;
    }

    public ImmutableArray<BulkImportPreset> GetAllBulkImportPresets() => _bulkImportPresetDatabaseManager.Items;

    public BulkImportPreset? GetBulkImportPresetById(string? id)
    {
        if (id == null) return null;

        BulkImportPreset? bulkImportPreset = _bulkImportPresetDatabaseManager.Items.FirstOrDefault(i => i.Id == id);
        if (bulkImportPreset == null) ErrorManager.Instance.PostInternalError($"The bulk import preset with the specified ID '{id}' was not found.");

        return bulkImportPreset;
    }

    #region Current State Internal Handler
    private ImmutableArray<ItemCountInfo> HandleRootAvatar(SelectionNode selectionNode)
    {
        string avatarId = selectionNode.Key;
        return ItemCategoryAggregator.Aggregate(_itemDatabaseManager.Items.Where(i => AvatarStatusResolver.Resolve(i, avatarId, _commonAvatarDatabaseManager.Items).IsSupportedOrCommon));
    }
    private ImmutableArray<ItemCountInfo> HandleRootAuthor(SelectionNode selectionNode)
    {
        string authorName = selectionNode.Key;
        return ItemCategoryAggregator.Aggregate(_itemDatabaseManager.Items.Where(i => i.Author == authorName));
    }
    private ImmutableArray<ItemCountInfo> HandleRootCategory(SelectionNode selectionNode)
    {
        string category = selectionNode.Key;
        return _itemDatabaseManager.Items
            .Where(i => i.IsCategoryMatch(category))
            .GetSortedItems(_runtimeSettings)
            .Select(i => new ItemCountInfo(i, 0))
            .ToImmutableArray();
    }
    private ImmutableArray<ItemCountInfo> HandleRootSelectedCategory(SelectionNode selectionNode)
    {
        SelectionNode? rootSelectionNode = _selectionState.Root;
        if (rootSelectionNode == null) return [];

        if (rootSelectionNode.State == ItemTagStates.RootAvatar)
        {
            List<ItemCountInfo> filteredResult = new();

            foreach (Item item in _itemDatabaseManager.Items)
            {
                if (!item.IsCategoryMatch(selectionNode.Key)) continue;

                AvatarStatus avatarStatus = AvatarStatusResolver.Resolve(item, rootSelectionNode.Key, _commonAvatarDatabaseManager.Items);
                if (!avatarStatus.IsSupportedOrCommon) continue;

                filteredResult.Add(new ItemCountInfo(item, 0, avatarStatus.IsOnlyCommon ? [avatarStatus.CommonAvatarName] : null));
            }

            return filteredResult
                .GetSortedItemsFromCountInfo(_runtimeSettings)
                .ToImmutableArray();
        }
        else if (rootSelectionNode.State == ItemTagStates.RootAuthor)
        {
            return _itemDatabaseManager.Items
                .Where(i => i.IsCategoryMatch(selectionNode.Key) && i.Author == rootSelectionNode.Key)
                .GetSortedItems(_runtimeSettings)
                .Select(i => new ItemCountInfo(i, 0))
                .ToImmutableArray();
        }

        return [];
    }
    private ImmutableArray<ItemCountInfo> HandleRootSelectedItem(SelectionNode selectionNode)
    {
        Item? item = GetItemById(selectionNode.Key);
        if (item == null) return [];

        return GetCategoryItemsFromPathInternal(ItemUtils.GetItemPath(_runtimeSettings.DataRootDirectory, item.ItemPath));
    }
    private ImmutableArray<ItemCountInfo> HandleItemFileCategory(SelectionNode selectionNode)
    {
        SelectionNode? fileSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem);
        if (fileSelectionNode == null) return [];

        Item? item = GetItemById(fileSelectionNode.Key);
        if (item == null) return [];
        
        return GetFilesFromPathInternal(ItemUtils.GetItemPath(_runtimeSettings.DataRootDirectory, item.ItemPath), selectionNode.Key);
    }
    #endregion

    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _selectionState.GetCurrentSelectionNodes();
    public SelectionNode? GetCurrentNode() => _selectionState.Current;
    public SelectionNode? GetRootNode() => _selectionState.Root;

    public Item? GetSelectedItem()
    {
        SelectionNode? itemSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem);
        if (itemSelectionNode == null) return null;

        return _itemDatabaseManager.Items.FirstOrDefault(i => i.Id == itemSelectionNode.Key);
    }

    private static ImmutableArray<ItemCountInfo> GetCategoryItemsFromPathInternal(string itemPath)
    {
        if (!Directory.Exists(itemPath))
        {
            ErrorManager.Instance.PostInternalError(string.Join("Directory not found: '{0}'.", itemPath));
            return [];
        }

        List<ItemFileCategoryDefinition> categoryDefinitions = Enum.GetValues<ItemFileCategoryType>()
            .Select(c => new ItemFileCategoryDefinition()
            {
                FileCategory = c,
                ExtensionFilters = c.GetExtensionFilters(),
                FilenameFilters = c.GetFileNameFilters(),
                Item = new FileCategoryItem(c)
            })
            .Where(x => x.ExtensionFilters != null)
            .ToList();

        List<string> unknownFiles = new();

        foreach (string file in FileSystemService.EnumerateFiles(itemPath))
        {
            string extension = Path.GetExtension(file);
            string fileName = Path.GetFileNameWithoutExtension(file);
            ItemFileCategoryDefinition? matched = categoryDefinitions.FirstOrDefault(def => def.ExtensionFilters!.Contains(extension) && (def.FilenameFilters == null || def.FilenameFilters.Any(f => fileName.Contains(f, StringComparison.CurrentCultureIgnoreCase))));

            if (matched != null) matched.Item.FilePaths.Add(file);
            else unknownFiles.Add(file);
        }

        List<ItemCountInfo> result = categoryDefinitions
            .Where(x => x.Item.FilePaths.Count > 0)
            .Select(x => new ItemCountInfo(x.Item, x.Item.FilePaths.Count))
            .ToList();

        if (unknownFiles.Count > 0)
        {
            FileCategoryItem unknownItem = new(ItemFileCategoryType.Unknown);
            unknownItem.FilePaths.AddRange(unknownFiles);
            result.Add(new ItemCountInfo(unknownItem, unknownFiles.Count));
        }

        return result.ToImmutableArray();
    }
    private static ImmutableArray<ItemCountInfo> GetFilesFromPathInternal(string itemPath, string category)
    {
        ItemFileCategoryType targetCategory = Enum.GetValues<ItemFileCategoryType>()
            .FirstOrDefault(i => i.GetLocalizationKey() == category);

        if (targetCategory == default) return [];

        string[]? extensionFilters = targetCategory.GetExtensionFilters();
        string[]? fileNameFilters = targetCategory.GetFileNameFilters();

        if (!Directory.Exists(itemPath))
        {
            ErrorManager.Instance.PostInternalError(string.Join("Directory not found: '{0}'.", itemPath));
            return [];
        }
        
        List<ItemCountInfo> result = new();

        foreach (string file in FileSystemService.EnumerateFiles(itemPath))
        {
            bool isMatch;

            if (targetCategory == ItemFileCategoryType.Unknown)
            {
                isMatch = !Enum.GetValues<ItemFileCategoryType>()
                    .Where(c => c != ItemFileCategoryType.Unknown && c.GetExtensionFilters() != null)
                    .Any(c =>
                    {
                        string[]? exts = c.GetExtensionFilters();
                        string[]? names = c.GetFileNameFilters();
                        return exts!.Contains(Path.GetExtension(file)) && (names == null || names.Any(f => Path.GetFileNameWithoutExtension(file).Contains(f, StringComparison.CurrentCultureIgnoreCase)));
                    });
            }
            else
            {
                string extension = Path.GetExtension(file);
                string fileName = Path.GetFileNameWithoutExtension(file);
                isMatch = extensionFilters != null && extensionFilters.Contains(extension) && (fileNameFilters == null || fileNameFilters.Any(f => fileName.Contains(f, StringComparison.CurrentCultureIgnoreCase)));
            }

            if (isMatch) result.Add(new ItemCountInfo(new ItemFile(Path.GetFullPath(file)), 0));
        }

        return result.ToImmutableArray();
    }

    public RuntimeSettings GetRuntimeSettings() => _runtimeSettings;
    #endregion

    #region Set API
    public void SetRuntimeSettings(RuntimeSettings runtimeSettings)
    {
        _runtimeSettings = runtimeSettings;

        _backupManager.SetAutoBackupPath(_runtimeSettings.AutoBackupRootDirectory);
        _backupManager.SetAutoBackupInterval(_runtimeSettings.AutoBackupInterval);
    }
    #endregion

    #region Add API
    public void AddCommonAvatar(string groupName, IEnumerable<string>? avatars = null)
    {
        CommonAvatar commonAvatar = new()
        {
            GroupName = groupName
        };

        if (avatars != null)
        {
            commonAvatar.UpdateAvatars(avatars);
            UpdateSearchIndex();
        }

        _commonAvatarDatabaseManager.Add(commonAvatar);

        SaveCommonAvatarDatabase();
    }
    public void AddBulkImportPreset(string presetName, IEnumerable<BulkImportItem>? items = null)
    {
        BulkImportPreset bulkImportPreset = new()
        {
            PresetName = presetName
        };

        if (items != null)
        {
            bulkImportPreset.UpdateItems(items);
        }

        _bulkImportPresetDatabaseManager.Add(bulkImportPreset);

        SaveBulkImportPresetDatabase();
    }
    public async Task<ErrorOr<ItemCreationResult>> AddItem(ItemCreationContext itemCreationContext)
    {
        ErrorOr<ItemCreationResult> itemCreationResult = await ItemCreator.FromItemCreationContext(itemCreationContext, _runtimeSettings);
        if (itemCreationResult.IsError) return Error.Failure(description: itemCreationResult.Errors.ToErrorString());

        if (itemCreationResult.Value.Item == null) return itemCreationResult;

        string currentUnixTime = DatetimeUtils.GetCurrentUnixTime();
        itemCreationResult.Value.Item.CreatedDate = currentUnixTime;
        itemCreationResult.Value.Item.UpdatedDate = currentUnixTime;

        _itemDatabaseManager.Add(itemCreationResult.Value.Item);
        UpdateSearchIndex(itemCreationResult.Value.Item.Id);

        SaveItemDatabase();

        return itemCreationResult;
    }
    public async Task<ErrorOr<ExtractResult>> AddItemPaths(string itemId, string[] paths)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        ExtractResult extractResult = await FileSystemService.ExtractItemPaths(ItemUtils.GetItemPath(_runtimeSettings.DataRootDirectory, item.ItemPath), paths, _runtimeSettings);

        UpdateItemUpdatedDate(itemId);
        SaveItemDatabase();

        return extractResult;
    }
    #endregion

    #region Edit API
    public async Task<bool> EditItem(string itemId, ItemCreationContext itemCreationContext)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return false;

        item.SetValuesFromCreationContext(itemCreationContext);

        // １個より多い場合は追加のアイテムとしてインポートしてあげる(0がRootフォルダー想定)
        if (itemCreationContext.ItemPaths.Count > 1) await AddItemPaths(item.Id, itemCreationContext.ItemPaths.Skip(1).ToArray());

        UpdateItemUpdatedDate(itemId);
        UpdateSearchIndex();

        SaveItemDatabase();

        return true;
    }
    #endregion

    #region Update API
    public void UpdateItemUpdatedDate(string id)
    {
        Item? item = GetItemById(id);
        if (item == null) return;

        item.UpdatedDate = DatetimeUtils.GetCurrentUnixTime();
    }
    #endregion

    #region Update Thumbnail API
    public async Task<ErrorOr<Success>> UpdateItemThumbnail(string itemId, string imageFilePath)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        ErrorOr<Success> result = await FileSystemService.CopyFileAsync(imageFilePath, Path.Combine(SystemPath.ItemThumbnailsPath, Path.GetFileName(imageFilePath)));
        if (result.IsError) return Error.Failure(description: result.Errors.ToErrorString());

        item.ThumbnmailFileName = Path.GetFileName(imageFilePath);
        UpdateItemUpdatedDate(itemId);
        
        SaveItemDatabase();

        return Result.Success;
    }
    public async Task<ErrorOr<Success>> UpdateAuthorThumbnail(string itemId, string imageFilePath)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        ErrorOr<Success> result = await FileSystemService.CopyFileAsync(imageFilePath, Path.Combine(SystemPath.AuthorThumbnailsPath, Path.GetFileName(imageFilePath)));
        if (result.IsError) return Error.Failure(description: result.Errors.ToErrorString());

        item.AuthorThumbnmailFileName = Path.GetFileName(imageFilePath);
        UpdateItemUpdatedDate(itemId);

        SaveItemDatabase();

        return Result.Success;
    }
    #endregion

    #region Replace API
    public void ReplaceCommonAvatarGroupToSupportedAvatars(string groupId)
    {
        CommonAvatar? commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        string internalId = commonAvatar.GetInternalId();

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.SelectMany(i => i == internalId ? commonAvatar.AvatarsView : [i]).Distinct());
        }
    }
    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupId)
    {
        CommonAvatar? commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        string internalId = commonAvatar.GetInternalId();

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Select(i => commonAvatar.AvatarsView.Contains(i) ? internalId : i).Distinct());
        }
    }
    #endregion

    #region Booth API
    private DateTime _lastBoothApiGetTime;
    public bool IsApiCooldownNow => _lastBoothApiGetTime.AddSeconds(5) > DateTime.Now;
    public async Task<ErrorOr<BoothItem>> GetBoothItem(string boothUrl)
    {
        if (string.IsNullOrEmpty(boothUrl)) return Error.Failure(description: "Invalid Url.");
        if (IsApiCooldownNow) return Error.Failure(description: "Booth API Cooldown Error.");

        string boothId = boothUrl.Split('/')[^1];

        _lastBoothApiGetTime = DateTime.Now; // 時間を更新する

        ErrorOr<BoothItem> result = await BoothService.GetItem(boothId);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to fetch booth item.", tag: result.Errors.ToErrorString());
            return Error.Failure(description: "Failed to fetch booth item.");
        }

        return result.Value;
    }
    #endregion

    #region File API
    public static async Task<ModifiedUnitypackagesResult> ModifyUnitypackageFilePaths(Dictionary<string, string> itemPathCategoryDictionary, Func<(string, int), Task>? reportProgress = null) => await FileSystemService.ModifyUnitypackageFilePathsAsync(itemPathCategoryDictionary, reportProgress);
    #endregion

    #region Remove API
    public bool RemoveItem(string itemId, bool removeItemFromSupportedAndImplemented = false)
    {
        bool removed = _itemDatabaseManager.Remove(itemId);
        if (removeItemFromSupportedAndImplemented)
        {
            foreach (Item item in _itemDatabaseManager.Items)
            {
                item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(a => a != itemId));
                item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(a => a != itemId));
            }
        }

        SaveItemDatabase();

        return removed;
    }

    public bool RemoveCommonAvatar(string commonAvatarId)
    {
        bool removed = _commonAvatarDatabaseManager.Remove(commonAvatarId);
        SaveCommonAvatarDatabase();
        
        return removed;
    }

    public bool RemoveBulkImportPreset(string id)
    {
        bool removed = _bulkImportPresetDatabaseManager.Remove(id);
        SaveBulkImportPresetDatabase();
        
        return removed;
    }
    #endregion

    #region Search API
    public ImmutableArray<Item> SearchItems(SearchFilter searchFilter) => ItemSearchService.ExecuteSearch(_itemDatabaseManager.Items, _commonAvatarDatabaseManager.Items, _itemSearchIndexDictionary, _runtimeSettings, searchFilter);
    #endregion

    #region Save API
    public void SaveRuntimeSettings() => RuntimeSettingsService.Save(_runtimeSettings);
    #endregion

    #region Data Importer API
    public async Task<ErrorOr<Success>> Import(DataImportType importType, string dataFolderPath, Func<(string, int), Task>? reportProgress = null)
    {
        ErrorOr<DataImportResult> result = await DataImporter.Import(importType, dataFolderPath, _runtimeSettings, reportProgress);
        if (result.IsError) return Error.Failure(description: result.Errors.ToErrorString());

        _itemDatabaseManager.AddRange(result.Value.Items);
        _commonAvatarDatabaseManager.AddRange(result.Value.CommonAvatars);

        SaveItemDatabase();
        SaveCommonAvatarDatabase();

        return Result.Success;
    }
    #endregion

    #region Data Exporter API
    public async Task<ErrorOr<Success>> Export(DataExportType exportType, string filePath, Dictionary<ItemType, string> localizedItemTypesMapping, bool includeCommonToSupported) => await DataExporter.Export(exportType, _itemDatabaseManager.Items, _commonAvatarDatabaseManager.Items, localizedItemTypesMapping, _runtimeSettings, filePath, includeCommonToSupported);
    #endregion

    #region Clear API
    public static void ClearTemp() => FileSystemService.DeleteDirectory(SystemPath.TempFolderPath);
    #endregion

    #region Execute Context Menu Command
    public async Task<ErrorOr<Success>> FetchAndUpdateThumbnailImage(string itemId)
    {
        Item? item = GetItemById(itemId);
        if (item == null) return Error.NotFound(description: "Item not found.");

        if (item.BoothId == -1) return Error.Validation(description: "Booth id not found.");

        if (IsApiCooldownNow) return Error.Failure(description: "API is on cooldown.");

        _lastBoothApiGetTime = DateTime.Now; // 時間を更新する
        ErrorOr<BoothItem> fetchResult = await BoothService.GetItem(item.BoothId.ToString());
        if (fetchResult.IsError) return Error.Failure(description: fetchResult.Errors.ToErrorString());

        string itemThumbnailFileName = item.BoothId + ".png";
        bool result = await ImageDownloader.Fetch(fetchResult.Value.ThumbnailUrl, Path.Combine(SystemPath.ItemThumbnailsPath, itemThumbnailFileName), true);
        if (result) item.ThumbnmailFileName = itemThumbnailFileName;

        return Result.Success;
    }
    #endregion

    #region Backup API
    private readonly BackupManager _backupManager = new();
    public void StartAutoBackup() => _backupManager.StartAutoBackup(_runtimeSettings.AutoBackupInterval, _runtimeSettings.AutoBackupRootDirectory); // minutes
    public async Task StopAutoBackup() => await _backupManager.StopAutoBackup();
    public async Task<ErrorOr<Success>> ExecuteBackup(string path) => await _backupManager.ExecuteBackup(path);
    #endregion
}
