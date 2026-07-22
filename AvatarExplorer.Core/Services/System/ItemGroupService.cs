using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Services.Avatars.Internal;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System.Repositories;
using ErrorOr;

namespace AvatarExplorer.Core.Services.System;

public enum QueryType
{
    Avatar,
    Author,
    Category
}

[Flags]
public enum SearchQueryTypes
{
    Item,
    CommonAvatar,
    TempAvatar
}

public class ItemGroupService(ItemRepository items, CommonAvatarRepository commonAvatars, TempAvatarRepository tempAvatars, RuntimeSettingsRepository settings)
{
    private readonly ItemRepository _items = items;
    private readonly CommonAvatarRepository _commonAvatars = commonAvatars;
    private readonly TempAvatarRepository _tempAvatars = tempAvatars;
    private readonly RuntimeSettingsRepository _runtimesettings = settings;

    public ItemRepository ItemRepository => _items;
    public CommonAvatarRepository CommonAvatarRepository => _commonAvatars;
    public TempAvatarRepository TempAvatarRepository => _tempAvatars;
    public RuntimeSettingsRepository RuntimeSettings => _runtimesettings;

    public List<INavigationable> GetQueryFilters(QueryType type)
    {
        return type switch
        {
            QueryType.Avatar => GetAvatars(includeTempAvatar: true),
            QueryType.Author => GetAuthors(),
            QueryType.Category => GetCategories(includeAllCategory: true),
            _ => []
        };
    }
    public List<INavigationable> GetAvatars(bool includeCommonAvatar = false, bool includeTempAvatar = false)
    {
        var avatars = new List<INavigationable>();

        if (includeCommonAvatar) avatars.AddRange(_commonAvatars.GetAll().Select(i => new Avatar(i)));
        avatars.AddRange(_items.GetAll().Where(i => i.Type == ItemType.Avatar).Select(i => new Avatar(i)));
        if (includeTempAvatar) avatars.AddRange(_tempAvatars.GetAll().Select(i => new Avatar(i)));

        return avatars;
    }
    public List<INavigationable> GetAuthors()
    {
        return _items.GetAll()
            .GroupBy(i => i.Author)
            .Select(i => new Author()
            {
                Name = i.Key,
                ItemCount = i.Count()
            })
            .ToList<INavigationable>();
    }
    public List<INavigationable> GetCategories(bool includeEmptyCategory = false, bool includeAllCategory = false)
    {
        var categories = new List<Folder>();
        
        var items = _items.GetAll();
        
        if (includeAllCategory)
        {
            categories.Add(new Folder("type:" + (int)ItemType.All)
            {
                Title = ItemType.All.GetLocalizationKey() ?? string.Empty,
                TitleLocalizable = true,
                ItemCount = items.Count
            });
        }

        var itemsByType = items
            .GroupBy(i => i.Type)
            .ToDictionary(g => g.Key, g => g.Count());

        var itemsByCustomCategory = items
            .Where(i => !string.IsNullOrEmpty(i.CustomCategory))
            .GroupBy(i => i.CustomCategory)
            .ToDictionary(g => g.Key, g => g.Count());

        var existCategories = items.Select(i => i.Type).Distinct();
        var existCustomCategories = items.Where(i => i.Type == ItemType.Custom).Select(i => i.CustomCategory).Distinct();

        categories.AddRange(
            Enum.GetValues<ItemType>()
                .Where(i => i.IsSelectable())
                .Where(i => includeEmptyCategory || existCategories.Contains(i))
                .Select(i =>
                {
                    return new Folder("type:" + (int)i)
                    {
                        Title = i.GetLocalizationKey() ?? string.Empty,
                        TitleLocalizable = true,
                        ItemCount = itemsByType.TryGetValue(i, out int count) ? count : 0
                    };
                })
        );

        categories.AddRange(existCustomCategories.Select(i =>
        {
            return new Folder("custom:" + i)
            {
                Title = i,
                TitleLocalizable = false,
                ItemCount = itemsByCustomCategory[i]
            };
        }));

        return categories.ToList<INavigationable>();
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
            ));
        _items.Save();

        _commonAvatars.GetAll()
            .ForEach(c => c.UpdateAvatars(
                c.Avatars
                    .Select(i => i == tempAvatarId ? targetItemId : i)
                    .Distinct()
            ));
        _commonAvatars.Save();

        _tempAvatars.Remove(tempAvatarId);
        _tempAvatars.Save();
    }

    public string[] GetAllSupportedAvatarsIds(IEnumerable<string> avatars, bool includeCommonAvatarToSupported = false)
    {
        return AvatarService.GetAllSupportedAvatarIds(avatars, _commonAvatars.GetAll(), includeCommonAvatarToSupported);
    }

    // TODO: 逆も作る
    public void ReplaceSupportedAvatarsToCommonAvatarGroup(string groupIdentifier)
    {
        var commonAvatar = _commonAvatars.Get(groupIdentifier);
        if (commonAvatar == null) return;

        foreach (var item in _items.GetAll().Where(i => i.Type == ItemType.Clothing))
        {
            item.UpdateSupportedAvatars(item.SupportedAvatars.Select(i => commonAvatar.Avatars.Contains(i) ? commonAvatar.Identifier : i).Distinct());
        }
    }

    public List<INavigationable> Search(SearchFilter filter, SearchQueryTypes queryType)
    {
        // それぞれのSearchFilterを取得し、Matchを実行する
        return [];
    }

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
}
