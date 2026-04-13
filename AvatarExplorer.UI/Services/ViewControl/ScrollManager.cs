using System.Collections.Generic;
using System.Linq;
using Avalonia;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.UI.Services.ViewControl;

internal class ScrollManager
{
    private static readonly Vector Empty = new();
    private readonly Dictionary<ItemTagStates, Vector> _currentScrollValues = new()
    {
        { ItemTagStates.SearchItem, Empty },
        { ItemTagStates.RootSelectedCategory, Empty },
        { ItemTagStates.RootSelectedItem, Empty },
        { ItemTagStates.ItemFileCategory, Empty },
        { ItemTagStates.ItemFileCategoryOpen, Empty }
    };

    internal bool IsScrollSupported(ItemTagStates itemTagState) => _currentScrollValues.ContainsKey(itemTagState);

    internal Vector GetScrollValue(ItemTagStates itemTagState) => IsScrollSupported(itemTagState) ? _currentScrollValues[itemTagState] : new();
    internal void SetScroll(ItemTagStates itemTagState, Vector value)
    {
        if (!IsScrollSupported(itemTagState)) return;
        _currentScrollValues[itemTagState] = value;
    }

    internal void ResetScrollValue(ItemTagStates itemTagState)
    {
        if (!IsScrollSupported(itemTagState)) return;
        SetScroll(itemTagState, Empty);
    }
    internal void ResetAllScrollValues()
    {
        foreach (ItemTagStates key in GetKeys())
            ResetScrollValue(key);
    }

    internal ItemTagStates[] GetKeys() => _currentScrollValues.Keys.ToArray();
}
