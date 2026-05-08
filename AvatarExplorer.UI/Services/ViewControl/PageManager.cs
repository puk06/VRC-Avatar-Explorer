using System.Collections.Generic;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.UI.Services.ViewControl;

internal class PageManager(int defaultValue) : CacheManager<ItemTagStates, int>(defaultValue)
{
    private static readonly HashSet<ItemTagStates> _supportedPageStates =
    [
        ItemTagStates.SearchItem,
        ItemTagStates.RootAvatar,
        ItemTagStates.RootAuthor,
        ItemTagStates.RootCategory,
        ItemTagStates.RootItem,
        ItemTagStates.RootSelectedCategory,
        ItemTagStates.RootSelectedItem,
        ItemTagStates.ItemFolder,
        ItemTagStates.ItemFileCategoryOpen
    ];

    private static readonly HashSet<ItemTagStates> _leftPanelStates =
    [
        ItemTagStates.RootAvatar,
        ItemTagStates.RootAuthor,
        ItemTagStates.RootCategory
    ];

    public override int Get(ItemTagStates key)
    {
        if (ContainsKey(key)) return base.Get(key);
        return _supportedPageStates.Contains(key) ? 0 : base.Get(key);
    }

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
