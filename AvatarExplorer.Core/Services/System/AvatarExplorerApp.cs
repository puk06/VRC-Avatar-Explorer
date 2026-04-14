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

public class AvatarExplorerApp
{
    public static readonly string CurrentVersion = "2.4.0-beta.3";

    public static AvatarExplorerApp Instance { get; private set; } = new AvatarExplorerApp();

    private bool _initialized = false;

    private readonly ItemDatabaseManager _itemDatabaseManager = new();
    private readonly CommonAvatarDatabaseManager _commonAvatarDatabaseManager = new();
    private readonly TempAvatarsDatabaseManager _tempAvatarsDatabaseManager = new();
    private readonly BulkImportPresetDatabaseManager _bulkImportPresetDatabaseManager = new();

    private readonly Dictionary<string, string> _itemSearchIndexDictionary = new();

    public Func<ArchivePasswordRequest, ValueTask<string?>>? PasswordProvider { get; set; }

    private readonly SelectionState _selectionState = new();
    private readonly Dictionary<ItemTagStates, Func<SelectionNode, ImmutableArray<ItemCountInfo>>> _stateHandlers;
    private RuntimeSettings _runtimeSettings = new();

    private AvatarExplorerApp()
    {
        _stateHandlers = new()
        {
            { ItemTagStates.SearchItem, HandleRootSelectedItem },
            { ItemTagStates.RootAvatar, HandleRootAvatar },
            { ItemTagStates.RootAuthor, HandleRootAuthor },
            { ItemTagStates.RootCategory, HandleRootCategory },
            { ItemTagStates.RootItem, HandleRootSelectedItem },
            { ItemTagStates.RootSelectedCategory, HandleRootSelectedCategory },
            { ItemTagStates.RootSelectedItem, HandleRootSelectedItem },
            { ItemTagStates.ItemFileCategory, HandleItemFileCategory }
        };
    }

    public void Initialize()
    {
        if (_initialized) return;

        LoadItemDatabase();
        LoadCommonAvatarDatabase();
        LoadBulkImportPresetDatabase();
        LoadTempAvatarsDatabase();
        LoadRuntimeSettings();
        StartAutoBackup();

        UpdateSearchIndex();

        ErrorManager.Instance.OnErrorOccured += ErrorLogWriter.Instance.Write;
        ErrorManager.Instance.OnInternalErrorOccured += ErrorLogWriter.Instance.InternalWrite;

        _initialized = true;
    }

    #region Database
    public void LoadItemDatabase(string? path = null)
    {
        string loadPath = path ?? SystemPath.ItemDatabasePath;
        ItemDatabaseMigrationService.MigrateThumbnailKey(loadPath);
        _itemDatabaseManager.Load(loadPath);
        UpdateSearchIndex();
    }

    public void LoadCommonAvatarDatabase(string? path = null) => _commonAvatarDatabaseManager.Load(path);
    public void LoadBulkImportPresetDatabase(string? path = null) => _bulkImportPresetDatabaseManager.Load(path);
    public void LoadTempAvatarsDatabase(string? path = null)
    {
        _tempAvatarsDatabaseManager.Load(path);
        UpdateSearchIndex();
    }

    public void SaveItemDatabase() => _itemDatabaseManager.Save();
    public void SaveCommonAvatarDatabase() => _commonAvatarDatabaseManager.Save();
    public void SaveBulkImportPresetDatabase() => _bulkImportPresetDatabaseManager.Save();
    public void SaveTempAvatarsDatabase() => _tempAvatarsDatabaseManager.Save();

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
    public void ResetBulkImportPresetDatabase()
    {
        _bulkImportPresetDatabaseManager.Clear();
        SaveBulkImportPresetDatabase();
    }
    public void ResetTempAvatarDatabase()
    {
        _tempAvatarsDatabaseManager.Clear();
        SaveTempAvatarsDatabase();
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
        _itemSearchIndexDictionary.Clear();

        Dictionary<string, string> avatarTitleMaps = ItemUtils.GetItemTitleMaps(_itemDatabaseManager.Items.Where(i => i.Type == ItemType.Avatar), _tempAvatarsDatabaseManager.Items);
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

        Dictionary<string, string> avatarNameMaps = ItemUtils.GetItemTitleMaps(_itemDatabaseManager.Items.Where(i => i.Type == ItemType.Avatar), _tempAvatarsDatabaseManager.Items);
        _itemSearchIndexDictionary[item.Id] = ItemSearchService.BuildItemSearchIndex(item, avatarNameMaps, _commonAvatarDatabaseManager.Items);
    }
    #endregion

    #region Resolve API
    public void ResolveTempAvatar(string tempAvatarId, string targetItemId)
    {
        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Select(i => i == tempAvatarId ? targetItemId : i).Distinct());
        }

        foreach (CommonAvatar commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Select(i => i == tempAvatarId ? targetItemId : i).Distinct());
        }

        SaveItemDatabase();
        SaveCommonAvatarDatabase();
        UpdateSearchIndex();
        
        RemoveTempAvatar(tempAvatarId);
    }
    #endregion

    #region Select API
    public void Select(ItemTagStates state, string key) => _selectionState.Push(state, key);
    public void SelectUndo() => _selectionState.Pop();
    public void SelectClear() => _selectionState.Clear();
    #endregion

    #region Get API
    public ImmutableArray<ItemCountInfo> GetAvatars(bool includeCommonAvatar = false, bool includeTempAvatar = false) => ItemAvatarAggregator.Aggregate(_itemDatabaseManager.Items, _commonAvatarDatabaseManager.Items, _tempAvatarsDatabaseManager.Items, _runtimeSettings, includeCommonAvatar, includeTempAvatar);
    public ImmutableArray<ItemCountInfo> GetAuthors() => ItemAuthorAggregator.Aggregate(_itemDatabaseManager.Items);
    public ImmutableArray<ItemCountInfo> GetCategories(bool includeEmptyCategory = false, bool includeAllCategory = false) => ItemCategoryAggregator.Aggregate(_itemDatabaseManager.Items, includeEmptyCategory, includeAllCategory);

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
        if (current == null)
        {
            return _itemDatabaseManager.Items
                .GetSortedItems(_runtimeSettings)
                .Select(i => new ItemCountInfo(i, 0))
                .ToImmutableArray();
        }

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

    public ImmutableArray<TempAvatar> GetAllTempAvatars() => _tempAvatarsDatabaseManager.Items;
    public TempAvatar? GetTempAvatarById(string? id)
    {
        if (id == null) return null;

        TempAvatar? tempAvatar = _tempAvatarsDatabaseManager.Items.FirstOrDefault(i => i.Id == id);
        if (tempAvatar == null) ErrorManager.Instance.PostInternalError($"The temp avatar with the specified ID '{id}' was not found.");

        return tempAvatar;
    }

    #region Current State Internal Handler
    private ImmutableArray<ItemCountInfo> HandleRootAvatar(SelectionNode selectionNode)
    {
        string avatarId = selectionNode.Key;
        return ItemCategoryAggregator.Aggregate(_itemDatabaseManager.Items.Where(i => AvatarStatusResolver.Resolve(i, avatarId, _commonAvatarDatabaseManager.Items, _runtimeSettings.TreatEmptySupportedAvatarAsNone).IsSupportedOrCommon));
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

                AvatarStatus avatarStatus = AvatarStatusResolver.Resolve(item, rootSelectionNode.Key, _commonAvatarDatabaseManager.Items, _runtimeSettings.TreatEmptySupportedAvatarAsNone);
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
        SelectionNode? fileSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem | ItemTagStates.RootItem);
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
        SelectionNode? itemSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem | ItemTagStates.RootItem);
        if (itemSelectionNode == null) return null;

        return _itemDatabaseManager.Items.FirstOrDefault(i => i.Id == itemSelectionNode.Key);
    }

    private static ImmutableArray<ItemCountInfo> GetCategoryItemsFromPathInternal(string itemPath)
    {
        if (!Directory.Exists(itemPath))
        {
            ErrorManager.Instance.PostInternalError(string.Format("Directory not found: '{0}'.", itemPath));
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

        foreach (string file in FileSystemService.EnumerateFiles(itemPath).SortByFileName())
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
            ErrorManager.Instance.PostInternalError(string.Format("Directory not found: '{0}'.", itemPath));
            return [];
        }
        
        List<ItemCountInfo> result = new();

        foreach (string file in FileSystemService.EnumerateFiles(itemPath).SortByFileName())
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

    public string GetSearchIndexByItemId(string itemId)
    {
        if (_itemSearchIndexDictionary.TryGetValue(itemId, out string? index)) return index ?? string.Empty;

        ErrorManager.Instance.PostInternalError($"Search index not found for item ID '{itemId}'.");
        return string.Empty;
    }
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
    public string AddCommonAvatar(string groupName, IEnumerable<string>? avatars = null)
    {
        CommonAvatar commonAvatar = new()
        {
            GroupName = groupName
        };

        if (avatars != null)
        {
            commonAvatar.UpdateAvatars(avatars);
        }

        _commonAvatarDatabaseManager.Add(commonAvatar);

        UpdateSearchIndex();

        SaveCommonAvatarDatabase();

        return commonAvatar.Id;
    }
    public string AddTempAvatar(string avatarName)
    {
        TempAvatar tempAvatar = new TempAvatar(avatarName);

        _tempAvatarsDatabaseManager.Add(tempAvatar);

        UpdateSearchIndex();

        SaveTempAvatarsDatabase();

        return tempAvatar.Id;
    }
    public string AddBulkImportPreset(string presetName, IEnumerable<BulkImportItem>? items = null)
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

        return bulkImportPreset.Id;
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
        if (itemCreationContext.ItemPaths.Count > 1)
        {
            ErrorOr<ExtractResult> addItemPathsResult = await AddItemPaths(item.Id, itemCreationContext.ItemPaths.Skip(1).ToArray());
            if (addItemPathsResult.IsError) return false;
            if (addItemPathsResult.Value.ProcessingFailedPaths.Count > 0) return false;
        }

        UpdateItemUpdatedDate(itemId);
        UpdateSearchIndex();

        SaveItemDatabase();

        return true;
    }

    public void EditCustomCategoryName(string previousName, string newName)
    {
        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Custom && i.CustomCategory == previousName))
        {
            item.CustomCategory = newName;
        }

        UpdateSearchIndex();

        SaveItemDatabase();
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

        item.ThumbnailFileName = Path.GetFileName(imageFilePath);
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

        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.SelectMany(i => i == internalId ? commonAvatar.AvatarsView : [i]).Distinct());
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }
    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupId)
    {
        CommonAvatar? commonAvatar = GetCommonAvatarById(groupId);
        if (commonAvatar == null) return;

        string internalId = commonAvatar.GetInternalId();

        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Select(i => commonAvatar.AvatarsView.Contains(i) ? internalId : i).Distinct());
        }

        UpdateSearchIndex();
        SaveItemDatabase();
    }

    public void ConvertDatabaseRelativePathsToFullPaths(string previousDataRootDirectory)
    {
        foreach (Item item in _itemDatabaseManager.Items)
        {
            string currentPath = ItemUtils.GetItemPath(previousDataRootDirectory, item.ItemPath);
            item.ItemPath = currentPath;
        }
        
        SaveItemDatabase();
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
    public bool RemoveItem(string id, bool removeAssetData = false)
    {
        if (removeAssetData)
        {
            Item? item = GetItemById(id);
            if (item != null)
            {
                // パスチェック：<sys>なのに..を含む場合はおかしい
                if (item.ItemPath.StartsWith("<sys>") && item.ItemPath.Contains(".."))
                {
                    ErrorManager.Instance.PostInternalError($"Corrupted item path detected: {item.ItemPath}");
                    return false;
                }

                FileSystemService.DeleteDirectory(ItemUtils.GetItemPath(_runtimeSettings.DataRootDirectory, item.ItemPath));
            }
        }

        bool removed = _itemDatabaseManager.Remove(id);
        
        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(i => i != id));
            item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(i => i != id));
        }

        foreach (CommonAvatar commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Where(i => i != id));
        }

        UpdateSearchIndex();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();

        return removed;
    }

    public bool RemoveCommonAvatar(string internalId)
    {
        string? id = CommonAvatar.GetGroupId(internalId);
        if (id == null) return false;

        bool removed = _commonAvatarDatabaseManager.Remove(id);

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(i => i != internalId));
            item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(i => i != internalId));
        }

        UpdateSearchIndex();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();
        
        return removed;
    }

    public bool RemoveBulkImportPreset(string id)
    {
        bool removed = _bulkImportPresetDatabaseManager.Remove(id);
        SaveBulkImportPresetDatabase();
        
        return removed;
    }

    public bool RemoveTempAvatar(string internalId)
    {
        string? id = TempAvatar.GetAvatarId(internalId);
        if (id == null) return false;

        bool removed = _tempAvatarsDatabaseManager.Remove(id);

        foreach (Item item in _itemDatabaseManager.Items)
        {
            item.UpdateSupportedAvatars(item.SupportedAvatarsView.Where(i => i != internalId));
            item.UpdateImplementedAvatars(item.ImplementedAvatarsView.Where(i => i != internalId));
        }

        foreach (CommonAvatar commonAvatar in _commonAvatarDatabaseManager.Items)
        {
            commonAvatar.UpdateAvatars(commonAvatar.AvatarsView.Where(i => i != internalId));
        }

        UpdateSearchIndex();
        SaveTempAvatarsDatabase();
        SaveItemDatabase();
        SaveCommonAvatarDatabase();
        
        return removed;
    }
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
            RuntimeSettings = _runtimeSettings
        };

        return ItemSearchService.ExecuteSearch(searchContext, searchFilter);
    }
    #endregion

    #region Save API
    public void SaveRuntimeSettings() => RuntimeSettingsService.Save(_runtimeSettings);
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
            RuntimeSettings = _runtimeSettings,
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
            RuntimeSettings = _runtimeSettings
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

        bool result = await ImageDownloader.Fetch(fetchResult.Value.ThumbnailUrl, Path.Combine(SystemPath.ItemThumbnailsPath, item.Id), true);
        if (!result) return Error.Failure(description: "Failed to fetch thumbnail.");
        
        item.ThumbnailFileName = item.Id;

        SaveItemDatabase();

        return Result.Success;
    }
    #endregion

    #region Backup API
    private readonly BackupManager _backupManager = new();
    public void StartAutoBackup() => _backupManager.StartAutoBackup(_runtimeSettings.AutoBackupInterval, _runtimeSettings.AutoBackupRootDirectory); // minutes
    public async Task StopAutoBackup() => await _backupManager.StopAutoBackup();
    public async Task<ErrorOr<Success>> ExecuteBackup(string path) => await _backupManager.ExecuteBackup(path);
    #endregion

    #region Merge API
    public void MergeItemCategories(ItemCategory sourceCategory, ItemCategory targetCategory)
    {
        foreach (Item item in _itemDatabaseManager.Items.Where(i => i.IsCategoryMatch(sourceCategory.CategoryName)))
        {
            item.Type = targetCategory.Type;
            item.CustomCategory = targetCategory.CustomCategory;
        }

        UpdateSearchIndex();

        SaveItemDatabase();
    }
    #endregion
}
