using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

public class ItemNavigationService
{
    public const string AvatarPrefix = "avatar";
    public const string AuthorPrefix = "author";
    public const string TypePrefix = "type";
    public const string CustomPrefix = "custom";
    public const string ItemPrefix = "item";
    public const string FolderPrefix = "folder";
    public const string ExtensionPrefix = "extension";
    public const string FilePrefix = "file";

    private readonly ItemGroupService _items;
    private readonly SelectionState _state = new();
    private readonly Dictionary<string, string> _folderPathMap = new();
    private readonly Dictionary<string, string> _filePathMap = new();

    private readonly Dictionary<string, Func<string, INavigationable[]>> _handlers;

    public event Action<string>? FileOpenRequested = null;

    public static string GetPrefix(string prefix, string value) => $"{prefix}:{value}";

    internal ItemNavigationService(ItemGroupService itemGroupService)
    {
        _handlers = new()
        {
            { AvatarPrefix, HandleRoot },
            { AuthorPrefix, HandleRoot },
            { TypePrefix, HandleCategory },
            { CustomPrefix, HandleCategory },
            { ItemPrefix, HandleItem },
            { FolderPrefix, HandleFolder },
            { ExtensionPrefix, HandleExtension }
        };

        _items = itemGroupService;
    }

    public Guid? Select(string state)
    {
        if (TryParseState(state, out var prefix, out var value))
        {
            if (prefix == FilePrefix)
            {
                if (!_filePathMap.TryGetValue(value, out var path))
                {
                    PopulateFileCache();
                    _filePathMap.TryGetValue(value, out path);
                }

                FileOpenRequested?.Invoke(path ?? value);
            }
            else return _state.Push(state);
        }

        return null;
    }
    public SelectionNode? Undo() => _state.Pop();
    public void Clear()
    {
        _state.Clear();
        _folderPathMap.Clear();
        _filePathMap.Clear();
    }

    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _state.GetCurrentSelectionNodes();

    public string? ResolveFolderPath(string state)
    {
        if (!TryParseState(state, out _, out var hash)) return null;
        return _folderPathMap.TryGetValue(hash, out var path) ? path : null;
    }

    public string? ResolveFilePath(string state)
    {
        if (!TryParseState(state, out _, out var hash)) return null;
        return _filePathMap.TryGetValue(hash, out var path) ? path : null;
    }
    
    public INavigationable[] GetCurrentSelectionView()
    {
        var state = _state.Current?.Value;
        if (state == null) return _items.ItemRepository.GetAll().ToArray<INavigationable>();
        if (!TryParseState(state, out var key, out _)) return [];

        if (_handlers.TryGetValue(key, out var func)) return func(state);
        return [];
    }

    private INavigationable[] HandleRoot(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        List<Item>? items = null;

        if (prefix == AvatarPrefix) items = _items.GetItemsFromAvatar(value);
        else if (prefix == AuthorPrefix) items = _items.GetItemsFromAuthor(value);
        else return [];

        var categolized = _items.ItemRepository.CategorizeItems(items);

        return categolized.Select(i =>
        {
            var identifier = i.Key;
            var (displayName, isLocalizable) = ResolveCategoryDisplay(identifier);
            
            return new Folder(identifier)
            {
                Title = displayName,
                TitleLocalizable = isLocalizable,
                ItemCount = i.Value.Count
            };
        }).ToArray<INavigationable>();
    }

    private INavigationable[] HandleCategory(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        IEnumerable<Item> items;

        var root = _state.Root?.Value;
        if (root == null) return [];
        
        if (TryParseState(root, out var rootPrefix, out var rootValue) && rootPrefix == AvatarPrefix)
            items = _items.GetItemsFromAvatar(rootValue);
        else if (TryParseState(root, out rootPrefix, out rootValue) && rootPrefix == AuthorPrefix)
            items = _items.GetItemsFromAuthor(rootValue);
        else
            items = _items.ItemRepository.GetAll();

        if (prefix == TypePrefix)
        {
            if (!TryResolveItemType(value, out var itemType)) return [];

            return items
                .Where(i => itemType == ItemType.All || i.Type == itemType)
                .ToArray();
        }

        if (prefix != CustomPrefix) return [];

        return items
            .Where(i => i.Type == ItemType.Custom && i.CustomCategory == value)
            .ToArray();
    }

    public static string GetCategoryDisplayName(string groupKey) => ResolveCategoryDisplay(groupKey).displayName;

    private static (string displayName, bool isLocalizable) ResolveCategoryDisplay(string groupKey)
    {
        if (!TryParseState(groupKey, out var prefix, out var value)) return (groupKey, false);

        if (prefix == TypePrefix)
        {
            if (TryResolveItemType(value, out var itemType))
            {
                var key = itemType.GetLocalizationKey();
                return string.IsNullOrEmpty(key) ? (value, false) : (key, true);
            }

            return (value, false);
        }

        if (prefix == CustomPrefix) return (value, false);

        return (groupKey, false);
    }

    private INavigationable[] HandleItem(string state)
    {
        var itemFiles = _items.ItemRepository.EnumerateItemFiles(state);
        var folders = itemFiles.GroupBy(i => i.ParentFolderPath);

        return folders.Select(i =>
        {
            var hash = PathUtils.ComputeHash(i.Key);
            _folderPathMap[hash] = i.Key;
            return new Folder(GetPrefix(FolderPrefix, hash))
            {
                Title = Path.GetFileName(i.Key),
                TitleLocalizable = false,
                ItemCount = i.Count()
            };
        }).ToArray<INavigationable>();
    }

    private INavigationable[] HandleFolder(string state)
    {
        var itemState = _state.FirstOrDefault(ItemPrefix)?.Value;
        if (itemState == null) return [];

        if (!TryParseState(state, out _, out var hash)) return [];

        if (!_folderPathMap.TryGetValue(hash, out var selectedFolderPath))
        {
            PopulateFolderCache(itemState);
            if (!_folderPathMap.TryGetValue(hash, out selectedFolderPath)) return [];
        }

        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemState);
        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);

        var categolized = CategorizeFiles(files);

        return categolized.Select(i =>
        {
            var localizationKey = i.Key.GetLocalizationKey();
            return new Folder(GetPrefix(ExtensionPrefix, ((int)i.Key).ToString()))
            {
                Title = localizationKey ?? i.Key.ToString(),
                TitleLocalizable = localizationKey != null,
                ItemCount = i.Value.Count
            };
        }).ToArray<INavigationable>();
    }

    private static Dictionary<ItemFileCategoryType, List<ItemFile>> CategorizeFiles(IEnumerable<ItemFile> files)
    {
        var result = new Dictionary<ItemFileCategoryType, List<ItemFile>>();

        foreach (var file in files)
        {
            var category = ResolveFileCategory(file);
            if (!result.TryGetValue(category, out var list))
            {
                list = [];
                result[category] = list;
            }

            list.Add(file);
        }

        return result;
    }

    private static ItemFileCategoryType ResolveFileCategory(ItemFile file)
    {
        var extension = Path.GetExtension(file.FilePath).ToLowerInvariant();
        var fileName = file.FileName.ToLowerInvariant();

        var candidates = Enum.GetValues<ItemFileCategoryType>()
            .Where(c => c != ItemFileCategoryType.None && c != ItemFileCategoryType.Unknown);

        foreach (var category in candidates)
        {
            var extensions = category.GetExtensionFilters();
            if (extensions != null && extensions.Contains(extension))
            {
                var fileNames = category.GetFileNameFilters();
                if (fileNames == null || fileNames.Any(n => fileName.Contains(n.ToLowerInvariant())))
                    return category;
            }
        }

        return ItemFileCategoryType.Unknown;
    }

    private void PopulateFolderCache(string itemState)
    {
        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemState);
        foreach (var group in itemFiles.GroupBy(i => i.ParentFolderPath))
        {
            var hash = PathUtils.ComputeHash(group.Key);
            _folderPathMap[hash] = group.Key;
        }
    }

    private void PopulateFileCache()
    {
        var itemState = _state.FirstOrDefault(ItemPrefix)?.Value;
        if (itemState == null) return;

        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemState);
        foreach (var file in itemFiles)
        {
            var hash = PathUtils.ComputeHash(file.FilePath);
            _filePathMap[hash] = file.FilePath;
        }
    }

    private INavigationable[] HandleExtension(string state)
    {
        var itemState = _state.FirstOrDefault(ItemPrefix)?.Value;
        var folderState = _state.FirstOrDefault(FolderPrefix)?.Value;

        if (itemState == null || folderState == null) return [];
        if (!TryParseState(folderState, out _, out var hash)) return [];

        if (!_folderPathMap.TryGetValue(hash, out var selectedFolderPath))
        {
            PopulateFolderCache(itemState);
            if (!_folderPathMap.TryGetValue(hash, out selectedFolderPath)) return [];
        }

        if (!TryParseState(state, out _, out var categoryRaw)) return [];
        
        var categoryIndex = ValueParser.Int(categoryRaw);
        if (!Enum.IsDefined(typeof(ItemFileCategoryType), categoryIndex)) return [];

        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemState);
        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);

        var categolized = CategorizeFiles(files);
        if (!categolized.TryGetValue((ItemFileCategoryType)categoryIndex, out var categorizedFiles)) return [];

        foreach (var file in categorizedFiles)
        {
            var fileHash = PathUtils.ComputeHash(file.FilePath);
            _filePathMap[fileHash] = file.FilePath;
        }

        return categorizedFiles.ToArray();
    }

    public static bool TryParseState(string rawState, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var delimiterIndex = rawState.IndexOf(':');
        if (delimiterIndex < 0) return false;

        key = rawState[..delimiterIndex];
        value = rawState[(delimiterIndex + 1)..];
        return true;
    }

    public static bool TryResolveItemType(string raw, out ItemType itemType)
    {
        itemType = ItemType.None;

        var index = ValueParser.Int(raw);
        if (!Enum.IsDefined(typeof(ItemType),index)) return false;

        itemType = (ItemType)index;
        return true;
    }
}
