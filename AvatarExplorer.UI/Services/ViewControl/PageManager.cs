using System.Collections.Generic;
using System.Linq;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.UI.Services.ViewControl;

internal class PageManager
{
    private readonly Dictionary<ItemTagStates, int> _currentPageStates = new()
    {
        { ItemTagStates.SearchItem, 0 },
        { ItemTagStates.RootAvatar, 0 },
        { ItemTagStates.RootAuthor, 0 },
        { ItemTagStates.RootCategory, 0 },
        { ItemTagStates.RootItem, 0 },
        { ItemTagStates.RootSelectedCategory, 0 },
        { ItemTagStates.RootSelectedItem, 0 },
        { ItemTagStates.ItemFileCategoryOpen, 0 }
    };

    private static readonly ItemTagStates[] _leftPanelStates =
    [
        ItemTagStates.RootAvatar,
        ItemTagStates.RootAuthor,
        ItemTagStates.RootCategory
    ];

    internal bool IsPageSupported(ItemTagStates itemTagState) => _currentPageStates.ContainsKey(itemTagState);
    internal bool IsStateResetSupported(ItemTagStates itemTagState) => !_leftPanelStates.Contains(itemTagState);

    internal int GetPage(ItemTagStates itemTagState) => IsPageSupported(itemTagState) ? _currentPageStates[itemTagState] : -1;
    internal void SetPage(ItemTagStates itemTagState, int value)
    {
        if (!IsPageSupported(itemTagState)) return;
        _currentPageStates[itemTagState] = value;
    }

    internal void ResetPageValue(ItemTagStates itemTagState)
    {
        if (!IsPageSupported(itemTagState) || !IsStateResetSupported(itemTagState)) return;
        SetPage(itemTagState, 0);
    }
    internal void ResetAllPageValues()
    {
        foreach (ItemTagStates key in GetKeys().Where(IsStateResetSupported))
            ResetPageValue(key);
    }

    internal ItemTagStates[] GetKeys() => _currentPageStates.Keys.ToArray();
}
