using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
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

    private readonly Dictionary<string, Func<string, IIdentifiable[]>> _handlers;

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
    public void PopToState(string targetState)
    {
        var nodes = _state.GetCurrentSelectionNodes().ToList();
        if (nodes.All(n => n.Value != targetState)) return;

        while (_state.Current != null && _state.Current.Value != targetState)
        {
            _state.Pop();
        }
    }

    public SelectionNode? CurrentState => _state.Current;
    public IEnumerable<SelectionNode> GetCurrentSelectionNodes() => _state.GetCurrentSelectionNodes();

    public string? GetCurrentAvatarId()
    {
        var avatarNode = _state.LastOrDefault(AvatarPrefix);
        if (avatarNode == null) return null;

        if (!TryParseState(avatarNode.Value, out var _, out var avatarId)) return null;

        return avatarId;
    }
    public string? GetCurrentItemId() => _state.FirstOrDefault(ItemPrefix)?.Value;

    public string? ResolvePath(string state) => TryParseState(state, out _, out var hash) ? ResolvePathInternal(hash) : null;
    private string? ResolvePathInternal(string hash) => _pathCache.TryGetValue(hash, out var path) ? path : null;

    public IIdentifiable[] GetCurrentSelectionView()
    {
        var state = _state.Current?.Value;
        if (state == null)
        {
            return _items.ItemRepository.GetAll()
                .Where(i => !i.IsHidden)
                .ToArray<IIdentifiable>();
        }

        if (!TryParseState(state, out var key, out _)) return [];
        return _handlers.TryGetValue(key, out var func) ? func(state) : [];
    }
    private IIdentifiable[] HandleRoot(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        List<Item>? items = prefix switch
        {
            AvatarPrefix => _items.GetItemsFromAvatar(value),
            AuthorPrefix => _items.GetItemsFromAuthor(value),
            _ => null
        };

        if (items == null) return [];

        var categolized = ItemRepository.CategorizeItems(items)
        .Select(i =>
        {
            var category = ItemCategory.FromIdentifier(i.Key);
            return new Folder(i.Key)
            {
                Title = category.ToString(),
                TitleLocalizable = category.IsLocalizable,
                ItemCount = i.Value.Count
            };
        }).ToList<IIdentifiable>();

        // Hidden
        if (items.Any(i => i.IsHidden))
        {
            var hiddenCategory = new ItemCategory(ItemType.Hidden);
            categolized.Add(new Folder(hiddenCategory.Identifier)
            {
                Title = hiddenCategory.ToString(),
                TitleLocalizable = hiddenCategory.IsLocalizable,
                ItemCount = items.Count(i => i.IsHidden)
            });
        }

        return categolized.ToArray();
    }
    private IIdentifiable[] HandleCategory(string state)
    {
        var root = _state.Root?.Value;
        if (root == null) return [];

        IEnumerable<Item> items;
        if (TryParseState(root, out var rootPrefix, out var rootValue) && rootPrefix == AvatarPrefix)
            items = _items.GetItemsFromAvatar(rootValue);
        else if (TryParseState(root, out rootPrefix, out rootValue) && rootPrefix == AuthorPrefix)
            items = _items.GetItemsFromAuthor(rootValue);
        else
            items = _items.ItemRepository.GetAll();

        if (ItemCategory.FromIdentifier(state).Type == ItemType.All)
            return items.Where(i => !i.IsHidden).ToArray();

        if (ItemCategory.FromIdentifier(state).Type == ItemType.Hidden)
            return items.Where(i => i.IsHidden).ToArray();

        return items.Where(i => i.Category.Identifier == state && !i.IsHidden).ToArray();
    }
    private IIdentifiable[] HandleItem(string state)
    {
        var itemFiles = PopulatePathCache(state);
        var allFolders = _items.ItemRepository.EnumerateItemFolders(GetCurrentItemId() ?? string.Empty);

        foreach (var folder in allFolders)
            _pathCache[PathUtils.ComputeHash(folder)] = folder;

        return allFolders.Select(folder =>
        {
            var hash = PathUtils.ComputeHash(folder);
            return new Folder(GetPrefix(FolderPrefix, hash), folder)
            {
                Title = Path.GetFileName(folder),
                TitleLocalizable = false,
                ItemCount = itemFiles.Count(f => f.ParentFolderPath == folder)
            };
        }).ToArray<IIdentifiable>();
    }
    private IIdentifiable[] HandleFolder(string state)
    {
        if (!TryParseState(state, out _, out var hash)) return [];

        var itemId = GetCurrentItemId();
        if (itemId == null) return [];

        var itemFiles = PopulatePathCache(itemId);
        var selectedFolderPath = ResolvePathInternal(hash);
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
        }).ToArray<IIdentifiable>();
    }
    private IIdentifiable[] HandleExtension(string state)
    {
        if (!TryParseState(state, out _, out var categoryRaw)) return [];

        var categoryIndex = ValueParser.Int(categoryRaw);
        if (!Enum.IsDefined(typeof(ItemFileCategoryType), categoryIndex)) return [];

        var itemId = GetCurrentItemId();
        var folderState = _state.FirstOrDefault(FolderPrefix)?.Value;
        if (itemId == null || folderState == null) return [];
        if (!TryParseState(folderState, out _, out var folderHash)) return [];

        var itemFiles = PopulatePathCache(itemId);
        var selectedFolderPath = ResolvePathInternal(folderHash);
        if (selectedFolderPath == null) return [];

        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);
        var categolized = FileCategorizer.Categorize(files);

        return categolized.TryGetValue((ItemFileCategoryType)categoryIndex, out var categorizedFiles)
            ? categorizedFiles.ToArray()
            : [];
    }
    private void HandleFile(string hash)
    {
        var itemid = GetCurrentItemId();
        if (itemid == null) return;

        PopulatePathCache(itemid);

        var file = ResolvePathInternal(hash);
        if (file == null) return;

        FileOpenRequested?.Invoke(file);
    }

    public IIdentifiable[] SearchFilesForCurrentItem(string query)
    {
        var itemId = GetCurrentItemId();
        if (itemId == null) return [];

        var itemFiles = PopulatePathCache(itemId);
        var searchResults = _items.ItemRepository.SearchItemFiles(itemId, query);

        return searchResults
            .Select(f => itemFiles.FirstOrDefault(i => i.FilePath == f.FilePath))
            .Where(f => f != null)
            .Cast<IIdentifiable>()
            .ToArray();
    }

    private List<ItemFile> PopulatePathCache(string itemId)
    {
        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemId);

        foreach (var file in itemFiles)
            _pathCache[PathUtils.ComputeHash(file.FilePath)] = file.FilePath;

        return itemFiles;
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
}
