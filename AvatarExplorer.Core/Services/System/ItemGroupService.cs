using System.Collections.Concurrent;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.Search;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.Core.Utils;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

public enum QueryType
{
    Avatar,
    Author,
    Category
}

public class ItemGroupService
{
    private readonly ItemRepository _items;
    private readonly CommonAvatarRepository _commonAvatars;
    private readonly TempAvatarRepository _tempAvatars;
    private readonly RuntimeSettingsRepository _runtimesettings;

    private readonly ConcurrentDictionary<string, ItemSearchIndex> _itemSearchIndices = new();
    private readonly ConcurrentDictionary<string, CommonAvatarSearchIndex> _commonAvatarSearchIndices = new();
    private readonly ConcurrentDictionary<string, TempAvatarSearchIndex> _tempAvatarSearchIndices = new();
    private bool _indicesBuilt;
    private readonly Lock _indicesLock = new();

    public ItemGroupService(ItemRepository items, CommonAvatarRepository commonAvatars, TempAvatarRepository tempAvatars, RuntimeSettingsRepository settings)
    {
        _items = items;
        _commonAvatars = commonAvatars;
        _tempAvatars = tempAvatars;
        _runtimesettings = settings;

        _items.OnUpdated += ItemUpdated;
        _commonAvatars.OnUpdated += CommonAvatarUpdated;
        _tempAvatars.OnUpdated += TempAvatarUpdated;
    }

    public List<IIdentifiable> GetQueryFilters(QueryType type)
    {
        return type switch
        {
            QueryType.Avatar => GetAvatars(includeTempAvatar: true),
            QueryType.Author => GetAuthors(),
            QueryType.Category => GetCategoryFolders(includeAllCategory: true),
            _ => []
        };
    }
    public List<IIdentifiable> GetAvatars(bool includeCommonAvatar = false, bool includeTempAvatar = false, bool rawIdentifier = false)
    {
        var avatars = new List<IIdentifiable>();

        if (includeCommonAvatar) avatars.AddRange(_commonAvatars.GetAll());
        avatars.AddRange(_items.GetAll().Where(i => i.Category.Type == ItemType.Avatar));
        if (includeTempAvatar) avatars.AddRange(_tempAvatars.GetAll());

        return avatars
            .Select(i => new Avatar(i, rawIdentifier))
            .ToList<IIdentifiable>();
    }
    public List<IIdentifiable> GetAuthors()
    {
        return _items.GetAll()
            .GroupBy(i => i.Author)
            .Select(i => new Author()
            {
                Name = i.Key,
                ItemCount = i.Count()
            })
            .OrderBy(i => i.Name)
            .ToList<IIdentifiable>();
    }
    public List<IIdentifiable> GetCategoryFolders(bool includeEmptyCategory = false, bool includeAllCategory = false)
    {
        var categories = new List<Folder>();
        
        var items = _items.GetAll();
        
        if (includeAllCategory)
        {
            categories.Add(new Folder(new ItemCategory(ItemType.All).Identifier)
            {
                Title = ItemType.All.GetLocalizationKey() ?? string.Empty,
                TitleLocalizable = true,
                ItemCount = items.Count
            });
        }

        var itemsByType = items
            .GroupBy(i => i.Category.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var itemsByCustomCategory = items
            .Where(i => i.Category.Type == ItemType.Custom && !string.IsNullOrEmpty(i.Category.CustomCategory))
            .GroupBy(i => i.Category.CustomCategory)
            .ToDictionary(g => g.Key, g => g.Count());

        var existCategories = items.Select(i => i.Category.Type).Distinct();
        var existCustomCategories = items.Where(i => i.Category.Type == ItemType.Custom).Select(i => i.Category.CustomCategory).Distinct();

        categories.AddRange(
            Enum.GetValues<ItemType>()
                .Where(i => i.IsSelectable())
                .Where(i => includeEmptyCategory || existCategories.Contains(i))
                .Select(i =>
                {
                    return new Folder(new ItemCategory(i).Identifier)
                    {
                        Title = i.GetLocalizationKey() ?? string.Empty,
                        TitleLocalizable = true,
                        ItemCount = itemsByType.TryGetValue(i, out int count) ? count : 0
                    };
                })
        );

        categories.AddRange(existCustomCategories.Select(i =>
        {
            return new Folder(new ItemCategory(i).Identifier)
            {
                Title = i,
                TitleLocalizable = false,
                ItemCount = itemsByCustomCategory[i]
            };
        }));

        return categories.ToList<IIdentifiable>();
    }
    public List<Item> GetItemsFromAvatar(string id)
    {
        var items = _items.GetAll();
        var commonAvatars = _commonAvatars.GetAll();
        var treatEmptyAsNone = _runtimesettings.Settings.TreatEmptySupportedAvatarAsNone;

        return items
            .Where(i =>
            {
                var result = AvatarStatusResolver.Resolve(i, id, commonAvatars, treatEmptyAsNone);
                return result.IsSupportedOrCommon;
            })
            .ToList();
    }
    public List<Item> GetItemsFromAuthor(string author)
    {
        return _items.GetAll()
            .Where(i => i.Author == author)
            .ToList();
    }

    public void ResolveTempAvatar(string tempAvatarId, string targetItemId)
    {
        _items.GetAll()
            .ForEach(i =>
                i.UpdateSupportedAvatars(
                    i.SupportedAvatars
                        .Select(i => i == tempAvatarId ? targetItemId : i)
                        .Distinct()
                )
            );
        
        _items.MarkAsChanged();
        _items.Save();

        _commonAvatars.GetAll()
            .ForEach(c => c.UpdateAvatars(
                c.Avatars
                    .Select(i => i == tempAvatarId ? targetItemId : i)
                    .Distinct()
            ));
        _commonAvatars.MarkAsChanged();
        _commonAvatars.Save();

        _tempAvatars.Remove(tempAvatarId);
        _tempAvatars.Save();
    }

    public string[] GetAllSupportedAvatarsIds(IEnumerable<string> avatars, bool includeCommonAvatarToSupported = false)
    {
        return AvatarService.GetAllSupportedAvatarIds(avatars, _commonAvatars.GetAll(), includeCommonAvatarToSupported);
    }

    /// <summary>
    /// アイテムを削除し、他のアイテムの対応アバター・実装アバターからもそのIDを削除します。
    /// </summary>
    public void RemoveItem(string identifier, bool removeFolder = false)
    {
        _items.GetAll()
            .Where(i => i.Identifier != identifier)
            .ForEach(i =>
            {
                var updatedSupported = i.SupportedAvatars.Where(a => a != identifier).ToArray();
                if (updatedSupported.Length != i.SupportedAvatars.Length)
                    i.UpdateSupportedAvatars(updatedSupported);

                var updatedImplemented = i.ImplementedAvatars.Where(a => a != identifier).ToArray();
                if (updatedImplemented.Length != i.ImplementedAvatars.Length)
                    i.UpdateImplementedAvatars(updatedImplemented);
            });
        _items.Remove(identifier, removeFolder);
        _items.Save();

        _commonAvatars.GetAll()
            .ForEach(c =>
            {
                var updatedAvatars = c.Avatars.Where(a => a != identifier).ToArray();
                if (updatedAvatars.Length != c.Avatars.Length)
                    c.UpdateAvatars(updatedAvatars);
            });
        _commonAvatars.Save();
        _commonAvatars.MarkAsChanged();
    }

    /// <summary>
    /// 仮アバターを削除し、アイテムの対応アバター・共通素体のアバターからもそのIDを削除します。
    /// </summary>
    public void RemoveTempAvatar(string identifier)
    {
        _items.GetAll()
            .ForEach(i =>
            {
                var updatedSupported = i.SupportedAvatars.Where(a => a != identifier).ToArray();
                if (updatedSupported.Length != i.SupportedAvatars.Length)
                    i.UpdateSupportedAvatars(updatedSupported);
            });
        _items.Save();
        _items.MarkAsChanged();

        _commonAvatars.GetAll()
            .ForEach(c =>
            {
                var updatedAvatars = c.Avatars.Where(a => a != identifier).ToArray();
                if (updatedAvatars.Length != c.Avatars.Length)
                    c.UpdateAvatars(updatedAvatars);
            });
        _commonAvatars.Save();
        _commonAvatars.MarkAsChanged();

        _tempAvatars.Remove(identifier);
        _tempAvatars.Save();
    }

    /// <summary>
    /// 共通素体を削除し、アイテムの対応アバターからもそのIDを削除します。
    /// </summary>
    public void RemoveCommonAvatar(string identifier, bool replaceToAvatars)
    {
        var group = _commonAvatars.Get(identifier);
        if (group == null) return;

        var itemsUpdated = false;

        _items.GetAll()
            .ForEach(i =>
            {
                var containsGroup = i.SupportedAvatars.Contains(identifier);
                if (!containsGroup) return;

                var updatedSupported = i.SupportedAvatars.Where(a => a != identifier).ToList();
                if (replaceToAvatars) updatedSupported.AddRange(group.Avatars);

                i.UpdateSupportedAvatars(updatedSupported);

                itemsUpdated = true;
            });

        if (itemsUpdated)
        {
            _items.Save();
            _items.MarkAsChanged();
        }

        _commonAvatars.Remove(identifier);
        _commonAvatars.Save();
    }

    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupIdentifier)
    {
        var commonAvatar = _commonAvatars.Get(groupIdentifier);
        if (commonAvatar == null) return;

        var updatedIdentifiers = new List<string>();
        foreach (var item in _items.GetAll().Where(i => i.Category.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Select(i => commonAvatar.Avatars.Contains(i) ? commonAvatar.Identifier : i).Distinct());
            updatedIdentifiers.Add(item.Identifier);
        }

        _items.Save();
        _items.MarkAsChanged();
    }

    #region Search

    /// <summary>
    /// 検索インデックスを再構築します。
    /// アプリ起動時やデータベースを一括で読み込んだ後に呼び出してください。
    /// </summary>
    public void RebuildIndices()
    {
        lock (_indicesLock)
        {
            _itemSearchIndices.Clear();
            _commonAvatarSearchIndices.Clear();
            _tempAvatarSearchIndices.Clear();
            BuildAllIndices();
            _indicesBuilt = true;
        }
    }

    /// <summary>
    /// 検索を実行し、一致した識別子を返します。
    /// </summary>
    /// <param name="searchString">検索文字列。スペース区切りで AND 検索。~ で始まると除外。FieldName="値" でフィールド指定。</param>
    /// <param name="types">検索対象のグループ。</param>
    /// <param name="locKeyProvider">カテゴリ検索時に表示名を LocalizationKey に変換する関数。</param>
    /// <returns>一致したアイテムなどの Identifier 一覧。</returns>
    public string[] SearchItems(string searchString, SearchResultType types, Func<string, string>? locKeyProvider = null)
    {
        EnsureIndicesBuilt();

        var query = SearchQueryParser.Parse(searchString);
        var result = new List<string>();

        if (types.HasFlag(SearchResultType.Items))
        {
            foreach (var item in _items.GetAll())
            {
                if (_itemSearchIndices.TryGetValue(item.Identifier, out var index) && MatchesQuery(index, query, locKeyProvider))
                    result.Add(item.Identifier);
            }
        }

        if (types.HasFlag(SearchResultType.CommonAvatar))
        {
            foreach (var commonAvatar in _commonAvatars.GetAll())
            {
                if (_commonAvatarSearchIndices.TryGetValue(commonAvatar.Identifier, out var index) && MatchesQuery(index, query, locKeyProvider))
                    result.Add(commonAvatar.Identifier);
            }
        }

        if (types.HasFlag(SearchResultType.TempAvatar))
        {
            foreach (var tempAvatar in _tempAvatars.GetAll())
            {
                if (_tempAvatarSearchIndices.TryGetValue(tempAvatar.Identifier, out var index) && MatchesQuery(index, query, locKeyProvider))
                    result.Add(tempAvatar.Identifier);
            }
        }

        return result.ToArray();
    }

    private void EnsureIndicesBuilt()
    {
        if (_indicesBuilt) return;
        lock (_indicesLock)
        {
            if (_indicesBuilt) return;
            BuildAllIndices();
            _indicesBuilt = true;
        }
    }

    private void BuildAllIndices()
    {
        var avatarTitleMap = ItemUtils.GetItemTitleMaps(_items.GetAll().Where(i => i.Category.Type == ItemType.Avatar), _tempAvatars.GetAll());
        var commonAvatarList = _commonAvatars.GetAll().ToList();

        foreach (var item in _items.GetAll())
        {
            BuildItemIndex(item, avatarTitleMap, commonAvatarList);
        }

        foreach (var commonAvatar in _commonAvatars.GetAll())
        {
            var targetItemIndices = commonAvatar.Avatars
                .Select(a => _itemSearchIndices.TryGetValue(a, out var index) ? index : null);
            _commonAvatarSearchIndices[commonAvatar.Identifier] = CommonAvatarSearchIndex.Build(commonAvatar, targetItemIndices);
        }

        foreach (var tempAvatar in _tempAvatars.GetAll())
        {
            _tempAvatarSearchIndices[tempAvatar.Identifier] = TempAvatarSearchIndex.Build(tempAvatar);
        }
    }

    private void BuildItemIndex(Item item, Dictionary<string, string> avatarTitleMap, List<CommonAvatar> commonAvatarList)
    {
        var supportedAvatarIds = AvatarService.GetAllSupportedAvatarIds(
            item.SupportedAvatars, commonAvatarList, includeCommonAvatarToSupported: true);

        var supportedAvatarNames = supportedAvatarIds
            .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMap, id))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        var implementedAvatarNames = item.ImplementedAvatars
            .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMap, id))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        var notImplementedAvatarNames = avatarTitleMap.Keys
            .Except(item.ImplementedAvatars)
            .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMap, id))
            .Where(name => !string.IsNullOrEmpty(name))
            .ToArray();

        var commonAvatarNames = commonAvatarList
            .Where(ca => ca.Avatars.Any(a => item.SupportedAvatars.Contains(a)))
            .Select(ca => ca.GroupName)
            .ToArray();

        _itemSearchIndices[item.Identifier] = ItemSearchIndex.Build(
            item,
            supportedAvatarNames,
            implementedAvatarNames,
            notImplementedAvatarNames,
            commonAvatarNames);
    }

    private void ItemUpdated()
    {
        if (!_indicesBuilt) return;
        RebuildIndices();
    }

    private void CommonAvatarUpdated()
    {
        if (!_indicesBuilt) return;
        RebuildIndices();
    }

    private void TempAvatarUpdated()
    {
        if (!_indicesBuilt) return;
        RebuildIndices();
    }

    private static bool MatchesQuery(ISearchIndex index, SearchQuery query, Func<string, string>? locKeyProvider)
    {
        return query.IsOr ? MatchesAny(index, query, locKeyProvider) : MatchesAll(index, query, locKeyProvider);
    }

    private static bool MatchesAll(ISearchIndex index, SearchQuery query, Func<string, string>? locKeyProvider)
    {
        foreach (var token in query.Tokens)
        {
            if (!index.IsMatch(token, locKeyProvider)) return false;
        }

        return true;
    }

    private static bool MatchesAny(ISearchIndex index, SearchQuery query, Func<string, string>? locKeyProvider)
    {
        foreach (var token in query.Tokens)
        {
            if (index.IsMatch(token, locKeyProvider)) return true;
        }

        return false;
    }

    #endregion

    public async Task<ErrorOr<Success>> Export(DataExportType exportType, string folderPath, Func<ItemType, ValueTask<string?>>? itemTypeLocalizer, bool includeCommonToSupported)
    {
        var exportContext = new ExportContext()
        {
            Items = _items.GetAll(),
            CommonAvatars = _commonAvatars.GetAll(),
            TempAvatars = _tempAvatars.GetAll(),
            ItemTypeLocalizer = itemTypeLocalizer,
            RuntimeSettings = _runtimesettings.Settings
        };

        var exportRequest = new ExportRequest()
        {
            ExportType = exportType,
            FolderPath = folderPath,
            IncludeCommonToSupported = includeCommonToSupported
        };

        return await DataExporter.Export(exportContext, exportRequest);
    }

    public async Task<ErrorOr<Success>> Import(ImportRequest importRequest)
    {
        var importer = new DataImporter(_items, _commonAvatars, _tempAvatars);
        return await importer.Import(importRequest);
    }
}
