using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.Core.Services.Items;

public class ItemNavigationService
{
    private const string AvatarPrefix = "avatar";
    private const string AuthorPrefix = "author";
    private const string TypePrefix = "type";
    private const string CustomPrefix = "custom";
    private const string ItemPrefix = "item";
    private const string FolderPrefix = "folder";
    private const string ExtensionPrefix = "extension";
    private const string FilePrefix = "file";

    private readonly ItemGroupService _items;
    private readonly SelectionState _state = new();

    private readonly Dictionary<string, Func<string, ISelectableItem[]>> _handlers;

    public event Action<string>? FileOpenRequested = null;

    public ItemNavigationService(ItemGroupService itemGroupService)
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

    public void Select(string state)
    {
        if (TryParseState(state, out var prefix, out var value))
        {
            if (prefix == FilePrefix) FileOpenRequested?.Invoke(value);
            else _state.Push(state);
        }
    }
    public string? Undo() => _state.Pop();
    public void Clear() => _state.Clear();
    public IEnumerable<string> GetCurrentSelectionNodes() => _state.GetCurrentSelectionNodes();

    private ISelectableItem[] HandleRoot(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        List<Item>? items = null;

        if (prefix == AvatarPrefix) items = _items.GetItemsFromAvatar(value);
        else if (prefix == AuthorPrefix) items = _items.GetItemsFromAuthor(value);
        else return [];

        var categolized = _items.ItemRepository.CategorizeItems(items);

        return categolized.Select(i =>
        {
            var category = ResolveCategoryDisplay(i.Key);
            return new Folder(i.Key)
            {
                Title = category.displayName,
                TitleLocalizable = category.isLocalizable,
                ItemCount = i.Value.Count
            };
        }).ToArray<ISelectableItem>();
    }

    private ISelectableItem[] HandleCategory(string state)
    {
        if (!TryParseState(state, out var prefix, out var value)) return [];

        IEnumerable<Item> items;

        var root = _state.Root;
        if (root == null) return [];
        
        if (TryParseState(root, out var rootPrefix, out var rootValue) && rootPrefix == AvatarPrefix)
        {
            items = _items.GetItemsFromAvatar(rootValue);
        }
        else if (TryParseState(root, out rootPrefix, out rootValue) && rootPrefix == AuthorPrefix)
        {
            items = _items.GetItemsFromAuthor(rootValue);
        }
        else
        {
            items = _items.ItemRepository.GetAll();
        }

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

    private ISelectableItem[] HandleItem(string state)
    {
        var itemFiles = _items.ItemRepository.EnumerateItemFiles(state);
        var folders = itemFiles.GroupBy(i => i.ParentFolderPath);

        return folders.Select(i =>
        {
            return new Folder("folder:" + i.Key)
            {
                Title = Path.GetFileName(i.Key),
                TitleLocalizable = false,
                ItemCount = i.Count()
            };
        }).ToArray<ISelectableItem>();
    }

    private ISelectableItem[] HandleFolder(string state)
    {
        var itemState = _state.FirstOrDefault("item:");
        if (itemState == null) return [];

        if (!TryParseState(state, out _, out var selectedFolderPath)) return [];

        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemState);
        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);

        var categolized = CategorizeFiles(files);

        return categolized.Select(i =>
        {
            var localizationKey = i.Key.GetLocalizationKey();
            return new Folder("extension:" + i.Key)
            {
                Title = localizationKey ?? i.Key.ToString(),
                TitleLocalizable = !string.IsNullOrEmpty(localizationKey),
                ItemCount = i.Value.Count
            };
        }).ToArray<ISelectableItem>();
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

    private ISelectableItem[] HandleExtension(string state)
    {
        var itemState = _state.FirstOrDefault("item:");
        var folderState = _state.FirstOrDefault("folder:");

        if (itemState == null || folderState == null) return [];
        if (!TryParseState(folderState, out _, out var selectedFolderPath)) return [];
        if (!TryParseState(state, out _, out var categoryRaw)) return [];
        if (!Enum.TryParse<ItemFileCategoryType>(categoryRaw, out var categoryType)) return [];

        var itemFiles = _items.ItemRepository.EnumerateItemFiles(itemState);
        var files = itemFiles.Where(i => i.ParentFolderPath == selectedFolderPath);

        var categolized = CategorizeFiles(files);
        return categolized.TryGetValue(categoryType, out var categorizedFiles)
            ? categorizedFiles.ToArray()
            : [];
    }

    private static bool TryParseState(string rawState, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var delimiterIndex = rawState.IndexOf(':');
        if (delimiterIndex < 0) return false;

        key = rawState[..delimiterIndex];
        value = rawState[(delimiterIndex + 1)..];
        return true;
    }

    private static bool TryResolveItemType(string raw, out ItemType itemType)
    {
        if (Enum.TryParse(raw, out itemType))
            return true;

        foreach (var candidate in Enum.GetValues<ItemType>())
        {
            var localizationKey = candidate.GetLocalizationKey();
            if (string.Equals(localizationKey, raw, StringComparison.Ordinal))
            {
                itemType = candidate;
                return true;
            }
        }

        itemType = ItemType.None;
        return false;
    }

    public ISelectableItem[] GetCurrentSelectionView()
    {
        var state = _state.Current;
        if (state == null) return _items.ItemRepository.GetAll().ToArray<ISelectableItem>();
        if (!TryParseState(state, out var key, out _)) return [];

        if (_handlers.TryGetValue(key, out var func)) return func(state);
        return [];
    }
}
