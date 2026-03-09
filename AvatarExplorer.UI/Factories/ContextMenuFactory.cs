using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using Material.Icons;
using Material.Icons.Avalonia;

namespace AvatarExplorer.UI.Factories;

internal static class ContextMenuFactory
{
    internal static ContextMenu GetContextMenu(ContextMenuAction[] contextMenuActions, EventHandler<RoutedEventArgs>? onClick = null)
    {
        ContextMenu contextMenu = new();

        foreach (ContextMenuAction contextMenuAction in contextMenuActions)
        {
            MenuItem menuItem = new()
            {
                Icon = GetMaterialIcon(contextMenuAction.ContextMenuIconType),
                Header = Localizer.Instance[contextMenuAction.DisplayName],
                Tag = contextMenuAction
            };

            if (onClick != null) menuItem.Click += onClick;

            contextMenu.Items.Add(menuItem);

            if (contextMenuAction.AddSeparator) contextMenu.Items.Add(new Separator());
        }

        return contextMenu;
    }

    private static MaterialIcon? GetMaterialIcon(ContextMenuIconType contextMenuIconType, double size = 16)
    {
        MaterialIconKind? materialIconKind = contextMenuIconType.GetMaterialIconKind();
        if (materialIconKind == null) return null;

        return new MaterialIcon()
        {
            Kind = (MaterialIconKind)materialIconKind,
            Width = size,
            Height = size
        };
    }
}
