using System.Linq;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.UI.Services.ViewControl;

internal class PageManager(int defaultValue) : CacheManager<ItemTagStates, int>(defaultValue)
{
    private static readonly ItemTagStates[] _leftPanelStates =
    [
        ItemTagStates.RootAvatar,
        ItemTagStates.RootAuthor,
        ItemTagStates.RootCategory
    ];

    public override bool Remove(ItemTagStates key)
    {
        if (_leftPanelStates.Contains(key)) return false; // 左パネルのページ情報は消さないようにする
        return base.Remove(key);
    }

    public override void Clear()
    {
        foreach (var state in GetKeys()) Remove(state);
    }
}
