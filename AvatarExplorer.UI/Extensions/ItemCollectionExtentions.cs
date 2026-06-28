using System.Collections;
using Avalonia.Controls;

namespace AvatarExplorer.UI.Extensions;

internal static class ItemCollectionExtentions
{
    internal static void AddRange(this ItemCollection itemCollection, IEnumerable values)
    {
        foreach (var value in values)
        {
            if (value == null) continue;
            itemCollection.Add(value);
        }
    }
}
