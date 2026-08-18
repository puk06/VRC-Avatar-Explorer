using System.Collections.Generic;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.Views;

public class GridRow(IReadOnlyList<ItemViewModel> items, int columns)
{
    public IReadOnlyList<ItemViewModel> Items { get; } = items;
    public int Columns { get; } = columns;
}
