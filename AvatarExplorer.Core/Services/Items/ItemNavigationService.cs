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
    private readonly Dictionary<string, string> _pathCache = new();

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
        if (!TryParseState(state, out var prefix, out var value)) return null;

        if (prefix == FilePrefix)
        {
            HandleFile(value);
            return null;
        }

        return _state.Push(state);
    }

    public SelectionNode? Undo() => _state.Pop();

    public void Clear()
    {
        _state.Clear();
        _pathCache.Clear();
    }

    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _state.GetCurrentSelectionNodes();

    public string? ResolveFolderPath(string state) => TryParseState(state, out _, out var hash) ? ResolvePath(hash) : null;
    public string? ResolveFilePath(string state) => TryParseState(state, out _, out var hash) ? ResolvePath(hash) : null;

    public INavigationable[] GetCurrentSelectionView()
    {
        var state = _state.Current?.Value;
        if (state == null) return _items.ItemRepository.GetAll().ToArray<INavigationable>();
        if (!TryParseState(state, out var key, out _)) return [];
        return _handlers.TryGetValue(key, out var func) ? func(state) : [];
    }

    private INavigationable[] HandleRoot(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        List<Item>? items = prefix switch
        {
            AvatarPrefix => _items.GetItemsFromAvatar(value),
            AuthorPrefix => _items.GetItemsFromAuthor(value),
            _ => null
        };

        if (items == null) return [];

        var categolized = _items.ItemRepository.CategorizeItems(items);
        return categolized.Select(i =>
        {
            var (displayName, isLocalizable) = ResolveCategoryDisplay(i.Key);
            return new Folder(i.Key)
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

        var root = _state.Root?.Value;
        if (root == null) return [];

        IEnumerable<Item> items;
        if (TryParseState(root, out var rootPrefix, out var rootValue) && rootPrefix == AvatarPrefix)
            items = _items.GetItemsFromAvatar(rootValue);
        else if (TryParseState(root, out rootPrefix, out rootValue) && rootPrefix == AuthorPrefix)
            items = _items.GetItemsFromAuthor(rootValue);
        else
            items = _items.ItemRepository.GetAll();

        if (prefix == TypePrefix)
        {
            if (!TryResolveItemType(value, out var itemType)) return [];
            return items.Where(i => itemType == ItemType.All || i.Type == itemType).ToArray();
        }

        if (prefix != CustomPrefix) return [];
        return items.Where(i => i.Type == ItemType.Custom && i.CustomCategory == value).ToArray();
    }

    private INavigationable[] HandleItem(string state)
    {
        var itemFiles = PopulatePathCache(state);

        return itemFiles.GroupBy(i => i.ParentFolderPath).Select(i =>
        {
            var hash = PathUtils.ComputeHash(i.Key);
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
        if (!TryParseState(state, out _, out var hash)) return [];

        var itemId = GetItemId();
        if (itemId == null) return [];

        var itemFiles = PopulatePathCache(itemId);
        var selectedFolderPath = ResolvePath(hash);
        if (selectedFolderPath == null) return [];

        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);
        var categolized = FileCategorizer.Categorize(files);

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

    private INavigationable[] HandleExtension(string state)
    {
        if (!TryParseState(state, out _, out var categoryRaw)) return [];

        var categoryIndex = ValueParser.Int(categoryRaw);
        if (!Enum.IsDefined(typeof(ItemFileCategoryType), categoryIndex)) return [];

        var itemId = GetItemId();
        var folderState = _state.FirstOrDefault(FolderPrefix)?.Value;
        if (itemId == null || folderState == null) return [];
        if (!TryParseState(folderState, out _, out var folderHash)) return [];

        var itemFiles = PopulatePathCache(itemId);
        var selectedFolderPath = ResolvePath(folderHash);
        if (selectedFolderPath == null) return [];

        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);
        var categolized = FileCategorizer.Categorize(files);

        return categolized.TryGetValue((ItemFileCategoryType)categoryIndex, out var categorizedFiles)
            ? categorizedFiles.ToArray()
            : [];
    }

    private void HandleFile(string hash)
    {
        var itemid = GetItemId();
        if (itemid == null) return;

        PopulatePathCache(itemid);

        var file = ResolvePath(hash);
        if (file == null) return;

        FileOpenRequested?.Invoke(file);
    }

    private string? ResolvePath(string hash) => _pathCache.TryGetValue(hash, out var path) ? path : null;

    private string? GetItemId() => _state.FirstOrDefault(ItemPrefix)?.Value;

    private List<ItemFile> PopulatePathCache(string itemId)
    {
        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemId);

        foreach (var file in itemFiles)
            _pathCache[PathUtils.ComputeHash(file.FilePath)] = file.FilePath;

        foreach (var group in itemFiles.GroupBy(i => i.ParentFolderPath))
            _pathCache[PathUtils.ComputeHash(group.Key)] = group.Key;

        return itemFiles;
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
        if (!Enum.IsDefined(typeof(ItemType), index)) return false;
        itemType = (ItemType)index;
        return true;
    }
}
