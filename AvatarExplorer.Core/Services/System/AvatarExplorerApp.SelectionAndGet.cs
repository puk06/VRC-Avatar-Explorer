using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Items.Internal;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars.Internal;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.System;

public partial class AvatarExplorerApp
{
    #region Select API
    public void Select(ItemTagStates state, string key) => _selectionState.Push(state, key);
    public void SelectUndo() => _selectionState.Pop();
    public void SelectClear() => _selectionState.Clear();
    #endregion

    #region Get API
    public ImmutableArray<ItemCountInfo> GetAvatars(bool includeCommonAvatar = false, bool includeTempAvatar = false) => ItemAvatarAggregator.Aggregate(_itemDatabaseManager.Items, _commonAvatarDatabaseManager.Items, _tempAvatarsDatabaseManager.Items, RuntimeSettings, includeCommonAvatar, includeTempAvatar);
    public ImmutableArray<ItemCountInfo> GetAuthors() => ItemAuthorAggregator.Aggregate(_itemDatabaseManager.Items);
    public ImmutableArray<ItemCountInfo> GetCategories(bool includeEmptyCategory = false, bool includeAllCategory = false) => ItemCategoryAggregator.Aggregate(_itemDatabaseManager.Items, includeEmptyCategory, includeAllCategory);

    public ImmutableArray<Item> GetAllItems() => _itemDatabaseManager.Items;
    public Item? GetItemById(string? itemId)
    {
        if (itemId == null) return null;

        Item? item = _itemDatabaseManager.GetById(itemId);
        if (item == null) ErrorManager.Instance.PostInternalError($"The item with the specified ID '{itemId}' was not found.");

        return item;
    }
    public ImmutableArray<ItemCountInfo> GetItemsForCurrentState()
    {
        SelectionNode? current = _selectionState.Current;
        if (current == null)
        {
            return _itemDatabaseManager.Items
                .GetSortedItems(RuntimeSettings)
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

        CommonAvatar? commonAvatar = _commonAvatarDatabaseManager.GetById(groupId);
        if (commonAvatar == null) ErrorManager.Instance.PostInternalError($"The common avatar group with the specified ID '{groupId}' was not found.");

        return commonAvatar;
    }

    public ImmutableArray<BulkImportPreset> GetAllBulkImportPresets() => _bulkImportPresetDatabaseManager.Items;
    public BulkImportPreset? GetBulkImportPresetById(string? id)
    {
        if (id == null) return null;

        BulkImportPreset? bulkImportPreset = _bulkImportPresetDatabaseManager.GetById(id);
        if (bulkImportPreset == null) ErrorManager.Instance.PostInternalError($"The bulk import preset with the specified ID '{id}' was not found.");

        return bulkImportPreset;
    }

    public ImmutableArray<TempAvatar> GetAllTempAvatars() => _tempAvatarsDatabaseManager.Items;
    public TempAvatar? GetTempAvatarById(string? id)
    {
        if (id == null) return null;

        TempAvatar? tempAvatar = _tempAvatarsDatabaseManager.GetById(id);
        if (tempAvatar == null) ErrorManager.Instance.PostInternalError($"The temp avatar with the specified ID '{id}' was not found.");

        return tempAvatar;
    }

    #region Current State Internal Handler
    private ImmutableArray<ItemCountInfo> HandleRootAvatar(SelectionNode selectionNode)
    {
        string avatarId = selectionNode.Key;
        return GetItemCategoriesFromAvatarIdInternal(avatarId);
    }
    private ImmutableArray<ItemCountInfo> HandleRootAuthor(SelectionNode selectionNode)
    {
        string authorName = selectionNode.Key;
        return GetItemCategoriesFromAuthorInternal(authorName);
    }
    private ImmutableArray<ItemCountInfo> HandleRootCategory(SelectionNode selectionNode)
    {
        string category = selectionNode.Key;
        return GetMatchedItemsByCategoryInternal(category);
    }
    private ImmutableArray<ItemCountInfo> HandleRootSelectedCategory(SelectionNode selectionNode)
    {
        SelectionNode? rootSelectionNode = GetRootNode();
        if (rootSelectionNode == null) return [];

        string category = selectionNode.Key;
        return GetMatchedItemsByCategoryInternal(rootSelectionNode, category);
    }
    private ImmutableArray<ItemCountInfo> HandleRootSelectedItem(SelectionNode selectionNode)
    {
        Item? item = GetItemById(selectionNode.Key);
        if (item == null) return [];

        string rootPath = ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath);

        return GetFoldersFromPathsInternal(rootPath, item.ItemPaths);
    }
    private ImmutableArray<ItemCountInfo> HandleItemFolder(SelectionNode selectionNode)
    {
        SelectionNode? itemSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem | ItemTagStates.RootItem);
        if (itemSelectionNode == null) return [];

        Item? item = GetItemById(itemSelectionNode.Key);
        if (item == null) return [];

        bool isRecursive = selectionNode.Key != ItemFolder.RootNodeName; // Rootだとアイテム直下のみ、そうでなければサブフォルダも含める
        string rootFolder = ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath);
        string folder = selectionNode.Key == ItemFolder.RootNodeName ? rootFolder : selectionNode.Key;

        return GetCategoryItemsFromPathInternal(folder, isRecursive: isRecursive);
    }
    private ImmutableArray<ItemCountInfo> HandleItemFileCategory(SelectionNode selectionNode)
    {
        SelectionNode? itemSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem | ItemTagStates.RootItem);
        if (itemSelectionNode == null) return [];

        SelectionNode? folderSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.ItemFolder);
        if (folderSelectionNode == null) return [];

        Item? item = GetItemById(itemSelectionNode.Key);
        if (item == null) return [];

        bool isRecursive = folderSelectionNode.Key != ItemFolder.RootNodeName; // Rootだとアイテム直下のみ、そうでなければサブフォルダも含める
        string rootFolder = ItemUtils.GetItemPath(RuntimeSettings.DataRootDirectory, item.ItemPath);
        string folder = folderSelectionNode.Key == ItemFolder.RootNodeName ? rootFolder : folderSelectionNode.Key;
        
        return GetFilesFromPathInternal(folder, selectionNode.Key, isRecursive: isRecursive);
    }

    private ImmutableArray<ItemCountInfo> GetItemCategoriesFromAvatarIdInternal(string avatarId)
    {
        return ItemCategoryAggregator
            .Aggregate(
                _itemDatabaseManager.Items
                    .Where(i => AvatarStatusResolver.Resolve(i, avatarId, _commonAvatarDatabaseManager.Items, RuntimeSettings.TreatEmptySupportedAvatarAsNone).IsSupportedOrCommon)
            );
    }
    private ImmutableArray<ItemCountInfo> GetItemCategoriesFromAuthorInternal(string author)
    {
        return ItemCategoryAggregator
            .Aggregate(
                _itemDatabaseManager.Items
                    .Where(i => i.Author == author)
            );
    }
    private ImmutableArray<ItemCountInfo> GetMatchedItemsByCategoryInternal(string category)
    {
        return _itemDatabaseManager.Items
            .Where(i => i.IsCategoryMatch(category))
            .GetSortedItems(RuntimeSettings)
            .Select(i => new ItemCountInfo(i, 0))
            .ToImmutableArray();
    }
    private ImmutableArray<ItemCountInfo> GetMatchedItemsByCategoryInternal(SelectionNode rootSelectionNode, string category)
    {
        if (rootSelectionNode.State == ItemTagStates.RootAvatar)
        {
            string rootAvatarId = rootSelectionNode.Key;

            List<ItemCountInfo> filteredResult = new();

            foreach (Item item in _itemDatabaseManager.Items)
            {
                if (!item.IsCategoryMatch(category)) continue;

                AvatarStatus avatarStatus = AvatarStatusResolver.Resolve(item, rootAvatarId, _commonAvatarDatabaseManager.Items, RuntimeSettings.TreatEmptySupportedAvatarAsNone);
                if (!avatarStatus.IsSupportedOrCommon) continue;

                filteredResult.Add(new ItemCountInfo(item, 0, avatarStatus.IsOnlyCommon ? [avatarStatus.CommonAvatarName] : null));
            }

            return filteredResult
                .GetSortedItemsFromCountInfo(RuntimeSettings)
                .ToImmutableArray();
        }
        else if (rootSelectionNode.State == ItemTagStates.RootAuthor)
        {
            string authorName = rootSelectionNode.Key;

            return _itemDatabaseManager.Items
                .Where(i => i.IsCategoryMatch(category) && i.Author == authorName)
                .GetSortedItems(RuntimeSettings)
                .Select(i => new ItemCountInfo(i, 0))
                .ToImmutableArray();
        }

        return [];
    }
    private ImmutableArray<ItemCountInfo> GetCategoryItemsFromPathInternal(string itemPath, bool isRecursive)
    {
        if (!Directory.Exists(itemPath))
        {
            ErrorManager.Instance.PostInternalError(string.Format("Directory not found: '{0}'.", itemPath));
            return [];
        }

        IEnumerable<ItemFileCategoryType> allCategories = Enum.GetValues<ItemFileCategoryType>();
        IEnumerable<ItemFileCategoryType> validCategories = allCategories.Where(c => !string.IsNullOrEmpty(c.GetLocalizationKey()));

        Dictionary<ItemFileCategoryType, List<string>> buckets = new();
        foreach (var c in validCategories) buckets[c] = new List<string>();

        IEnumerable<ItemFileCategoryType> categoriesWithFilters = validCategories
            .Where(c => c != ItemFileCategoryType.Unknown && c.GetExtensionFilters() != null);

        foreach (string file in FileSystemService.EnumerateFiles(itemPath, isRecursive: isRecursive).SortByFileName())
        {
            string extension = Path.GetExtension(file);
            string fileName = Path.GetFileNameWithoutExtension(file);

            bool matchedAny = false;

            foreach (var c in categoriesWithFilters)
            {
                string[]? exts = c.GetExtensionFilters();
                string[]? names = c.GetFileNameFilters();
                if (exts != null && exts.Contains(extension) && (names == null || names.Any(f => fileName.Contains(f, StringComparison.CurrentCultureIgnoreCase))))
                {
                    buckets[c].Add(Path.GetFullPath(file));
                    matchedAny = true;
                }
            }

            if (!matchedAny && validCategories.Contains(ItemFileCategoryType.Unknown))
            {
                buckets[ItemFileCategoryType.Unknown].Add(Path.GetFullPath(file));
            }
        }

        var resultBuilder = ImmutableArray.CreateBuilder<ItemCountInfo>();
        foreach (var kv in buckets)
        {
            if (kv.Value.Count == 0) continue;

            FileCategoryItem item = new(kv.Key);
            item.FilePaths.AddRange(kv.Value);

            resultBuilder.Add(new ItemCountInfo(item, kv.Value.Count));
        }

        return resultBuilder.ToImmutable();
    }
    private ImmutableArray<ItemCountInfo> GetFoldersFromPathsInternal(string rootPath, IEnumerable<string> itemPaths)
    {
        var resultBuilder = ImmutableArray.CreateBuilder<ItemCountInfo>();

        if (Directory.Exists(rootPath))
        {
            foreach (string folder in Directory.GetDirectories(rootPath).SortByFileName())
            {
                resultBuilder.Add(new ItemCountInfo(new ItemFolder(Path.GetFullPath(folder)), FileSystemService.EnumerateFiles(folder).Count()));
            }

            IEnumerable<string> rootFiles = FileSystemService.EnumerateFiles(rootPath, isRecursive: false);
            if (rootFiles.Any())
            {
                resultBuilder.Add(new ItemCountInfo(new ItemFolder(Path.GetFullPath(rootPath), isRoot: true), rootFiles.Count()));
            }
        }

        foreach (string itemPath in itemPaths)
        {
            if (!Directory.Exists(itemPath))
            {
                ErrorManager.Instance.PostInternalError(string.Format("Directory not found: '{0}'.", itemPath));
                continue;
            }

            resultBuilder.Add(new ItemCountInfo(new ItemFolder(itemPath), FileSystemService.EnumerateFiles(itemPath).Count()));
        }

        return resultBuilder.ToImmutable();
    }
    private ImmutableArray<ItemCountInfo> GetFilesFromPathInternal(string itemPath, string category, bool isRecursive)
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

        var resultBuilder = ImmutableArray.CreateBuilder<ItemCountInfo>();

        if (targetCategory == ItemFileCategoryType.Unknown)
        {
            var categoriesWithFilters = Enum.GetValues<ItemFileCategoryType>()
                .Where(c => c != ItemFileCategoryType.Unknown && c.GetExtensionFilters() != null);

            foreach (string file in FileSystemService.EnumerateFiles(itemPath, isRecursive: isRecursive).SortByFileName())
            {
                string extension = Path.GetExtension(file);
                string fileName = Path.GetFileNameWithoutExtension(file);

                bool matched = categoriesWithFilters.Any(c =>
                {
                    string[]? exts = c.GetExtensionFilters();
                    string[]? names = c.GetFileNameFilters();
                    return exts != null && exts.Contains(extension) && (names == null || names.Any(f => fileName.Contains(f, StringComparison.CurrentCultureIgnoreCase)));
                });

                if (!matched) resultBuilder.Add(new ItemCountInfo(new ItemFile(Path.GetFullPath(file)), 0));
            }
        }
        else
        {
            foreach (string file in FileSystemService.EnumerateFiles(itemPath, isRecursive: isRecursive).SortByFileName())
            {
                string extension = Path.GetExtension(file);
                string fileName = Path.GetFileNameWithoutExtension(file);

                if (extensionFilters != null && extensionFilters.Contains(extension) && (fileNameFilters == null || fileNameFilters.Any(f => fileName.Contains(f, StringComparison.CurrentCultureIgnoreCase))))
                {
                    resultBuilder.Add(new ItemCountInfo(new ItemFile(Path.GetFullPath(file)), 0));
                }
            }
        }

        return resultBuilder.ToImmutable();
    }
    #endregion

    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _selectionState.GetCurrentSelectionNodes();
    public SelectionNode? GetCurrentNode() => _selectionState.Current;
    public SelectionNode? GetRootNode() => _selectionState.Root;

    public Item? GetSelectedItem()
    {
        SelectionNode? itemSelectionNode = _selectionState.FirstOrDefault(ItemTagStates.RootSelectedItem | ItemTagStates.SearchItem | ItemTagStates.RootItem);
        if (itemSelectionNode == null) return null;

        return _itemDatabaseManager.GetById(itemSelectionNode.Key);
    }

    public RuntimeSettings GetRuntimeSettings() => RuntimeSettings;

    public string GetSearchIndexByItemId(string itemId)
    {
        if (_itemSearchIndexDictionary.TryGetValue(itemId, out string? index)) return index ?? string.Empty;

        ErrorManager.Instance.PostInternalError($"Search index not found for item ID '{itemId}'.");
        return string.Empty;
    }
    #endregion

    #region Event
    public event Action? OnSelectionNodeChanged
    {
        add => _selectionState.SelectionChanged += value;
        remove => _selectionState.SelectionChanged -= value;
    }
    #endregion
}
