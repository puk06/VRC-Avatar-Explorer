using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;

namespace AvatarExplorer.Core.Services.Items;

internal static class ItemAvatarAggregator
{
    internal static IReadOnlyList<ItemCountInfo> Aggregate(IReadOnlyList<Item> items, IReadOnlyList<CommonAvatar> commonAvatars, RuntimeSettings runtimeSettings, bool includeCommonAvatar)
    {
        List<ItemCountInfo> avatars = new();

        // 共通素体グループをアバターとして追加して返すかどうか
        if (includeCommonAvatar)
        {
            avatars.AddRange(commonAvatars.Select(i => new ItemCountInfo(i, i.AvatarsView.Count)));
        }

        avatars.AddRange(
            items
                .Where(i => i.Type == ItemType.Avatar)
                .GetSortedItems(runtimeSettings)
                .Select(i => new ItemCountInfo(i, 0))
        );

        return avatars;
    }
}
