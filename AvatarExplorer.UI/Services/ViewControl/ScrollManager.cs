using System.Collections.Generic;
using Avalonia;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Services.System;

namespace AvatarExplorer.UI.Services.ViewControl;

internal class ScrollManager(Vector defaultValue) : CacheManager<ItemTagStates, Vector>(defaultValue)
{
    private static readonly HashSet<ItemTagStates> _supportedScrollStates =
    [
        ItemTagStates.SearchItem,
        ItemTagStates.RootItem,
        ItemTagStates.RootSelectedCategory,
        ItemTagStates.RootSelectedItem,
        ItemTagStates.ItemFileCategory,
        ItemTagStates.ItemFileCategoryOpen
    ];

    public override void Add(ItemTagStates key, Vector value)
    {
        if (!_supportedScrollStates.Contains(key)) return;
        base.Add(key, value);
    }
}
