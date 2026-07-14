using System.Collections.Immutable;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.Avatars.Internal;
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

[Flags]
public enum SearchQueryTypes
{
    Item,
    CommonAvatar,
    TempAvatar
}

public record struct FilterQuery(QueryType Type, string Value);

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

    public void ResolveTempAvatar(string tempAvatarId, string targetItemId)
    {
        _items.GetAll()
            .ForEach(i => i.UpdateSupportedAvatars(
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

    public List<ISelectableItem> GetQueryFilters(QueryType type)
    {
        return type switch
        {
            QueryType.Avatar => GetAvatars(),
            QueryType.Author => GetAuthors(),
            QueryType.Category => GetCategories(),
            _ => []
        };
    }
    public List<ISelectableItem> GetAvatars(bool includeCommonAvatar = false, bool includeTempAvatar = false)
    {
        var avatars = new List<ISelectableItem>();

        if (includeCommonAvatar) avatars.AddRange(_commonAvatars.GetAll());
        avatars.AddRange(_items.GetAll().Where(i => i.Type == ItemType.Avatar));
        if (includeTempAvatar) avatars.AddRange(_tempAvatars.GetAll());

        return avatars;
    }
    public List<ISelectableItem> GetAuthors()
    {
        return _items.GetAll()
            .GroupBy(i => i.Author)
            .Select(i => new Author()
            {
                Name = i.Key,
                ItemCount = i.Count()
            })
            .ToList<ISelectableItem>();
    }
    public List<ISelectableItem> GetCategories(bool includeEmptyCategory = false, bool includeAllCategory = false)
    {
        var categories = new List<Folder>();
        
        var items = _items.GetAll();
        
        if (includeAllCategory)
        {
            categories.Add(new Folder("type:" + ItemType.All)
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
                    return new Folder("type:" + i)
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

        return categories.ToList<ISelectableItem>();
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

    public List<ISelectableItem> Search(SearchFilter filter, SearchQueryTypes queryType)
    {
        // それぞれのSearchFilterを取得し、Matchを実行する
        return [];
    }

    public async Task<ErrorOr<Success>> Export(DataExportType exportType, string filePath, Dictionary<ItemType, string> localizedItemTypesMapping, bool includeCommonToSupported)
    {
        var exportContext = new ExportContext()
        {
            Items = _items.GetAll(),
            CommonAvatars = _commonAvatars.GetAll(),
            TempAvatars = _tempAvatars.GetAll(),
            LocalizedItemTypesMapping = localizedItemTypesMapping,
            RuntimeSettings = _runtimesettings.Settings
        };

        var exportRequest = new ExportRequest()
        {
            ExportType = exportType,
            FilePath = filePath,
            IncludeCommonToSupported = includeCommonToSupported
        };

        return await DataExporter.Export(exportContext, exportRequest);
    }
}
