using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Services.Avatars;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Services.Items;

public static class SearchIndexBuilder
{
    public static Dictionary<string, ItemSearchIndex> BuildItemIndices(
        IEnumerable<Item> items,
        IEnumerable<TempAvatar> tempAvatars,
        IEnumerable<CommonAvatar> commonAvatars)
    {
        var avatarTitleMaps = ItemUtils.GetItemTitleMaps(items.Where(i => i.Category.Type == ItemType.Avatar), tempAvatars);
        var commonAvatarList = commonAvatars.ToList();
        var result = new Dictionary<string, ItemSearchIndex>();

        foreach (var item in items)
        {
            var supportedAvatarIds = AvatarService.GetAllSupportedAvatarIds(
                item.SupportedAvatars, commonAvatarList, includeCommonAvatarToSupported: true);

            var supportedAvatarNames = supportedAvatarIds
                .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, id))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();

            var implementedAvatarNames = item.ImplementedAvatars
                .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, id))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();

            var notImplementedAvatarNames = avatarTitleMaps.Keys
                .Except(item.ImplementedAvatars)
                .Select(id => ItemUtils.GetTitleFromDictionary(avatarTitleMaps, id))
                .Where(name => !string.IsNullOrEmpty(name))
                .ToArray();

            var commonAvatarNames = commonAvatarList
                .Where(ca => ca.Avatars.Any(a => item.SupportedAvatars.Contains(a)))
                .Select(ca => ca.GroupName)
                .ToArray();

            var fileNames = item.EnumerateFiles()
                .Select(Path.GetFileName)
                .Where(n => !string.IsNullOrEmpty(n))
                .Cast<string>()
                .ToArray();

            result[item.Identifier] = ItemSearchIndex.Build(
                item,
                supportedAvatarNames,
                implementedAvatarNames,
                notImplementedAvatarNames,
                commonAvatarNames,
                fileNames);
        }

        return result;
    }

    public static Dictionary<string, CommonAvatarSearchIndex> BuildCommonAvatarIndices(IEnumerable<CommonAvatar> commonAvatars)
    {
        var result = new Dictionary<string, CommonAvatarSearchIndex>();

        foreach (var commonAvatar in commonAvatars)
        {
            result[commonAvatar.Identifier] = CommonAvatarSearchIndex.Build(commonAvatar);
        }

        return result;
    }

    public static Dictionary<string, TempAvatarSearchIndex> BuildTempAvatarIndices(IEnumerable<TempAvatar> tempAvatars)
    {
        var result = new Dictionary<string, TempAvatarSearchIndex>();

        foreach (var tempAvatar in tempAvatars)
        {
            result[tempAvatar.Identifier] = TempAvatarSearchIndex.Build(tempAvatar);
        }

        return result;
    }

    public static SearchContext BuildSearchContext(
        IEnumerable<Item> items,
        IEnumerable<CommonAvatar> commonAvatars,
        IEnumerable<TempAvatar> tempAvatars,
        RuntimeSettings runtimeSettings)
    {
        return new SearchContext
        {
            Items = items,
            CommonAvatars = commonAvatars,
            TempAvatars = tempAvatars,
            ItemSearchIndices = BuildItemIndices(items, tempAvatars, commonAvatars),
            CommonAvatarSearchIndices = BuildCommonAvatarIndices(commonAvatars),
            TempAvatarSearchIndices = BuildTempAvatarIndices(tempAvatars),
            RuntimeSettings = runtimeSettings
        };
    }
}
