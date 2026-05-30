using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvatarExplorer.Core.Localization;
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
        FontFamily fontFamily = new($"avares://AvatarExplorer/Assets/Fonts#{Localizer.Instance[LocalizationKey.FontFamily]}");
        ContextMenu contextMenu = new()
        {
            FontFamily = fontFamily
        };

        foreach (ContextMenuAction contextMenuAction in contextMenuActions)
        {
            MenuItem menuItem = new()
            {
                Icon = GetMaterialIcon(contextMenuAction.ContextMenuIconType),
                Header = contextMenuAction.UseLocalization ? Localizer.Instance[contextMenuAction.DisplayName] : contextMenuAction.DisplayName,
                Tag = contextMenuAction,
                FontFamily = fontFamily,
                IsEnabled = contextMenuAction.IsEnabled
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
