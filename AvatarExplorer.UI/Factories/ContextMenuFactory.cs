using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
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
    internal static ContextMenu GetContextMenu(ContextMenuAction[] contextMenuActions, Action<ContextMenuAction>? onContextClick = null)
    {
        var fontFamily = new FontFamily($"avares://AvatarExplorer/Assets/Fonts#{Localizer.Instance[LocalizationKey.FontFamily]}");
        var contextMenu = new ContextMenu()
        {
            FontFamily = fontFamily
        };

        foreach (var contextMenuAction in contextMenuActions)
        {
            // TODO: これ、メモリリーク起こすかも（イベントが解除されない気がする）。まあこれはいつか改善します
            void SetClickHandlers(MenuItem item, ContextMenuAction tagData)
            {
                if (onContextClick != null)
                {
                    item.Click += (s, e) => onContextClick(tagData);
                }
            }
            
            var menuItem = new MenuItem()
            {
                Icon = GetMaterialIcon(contextMenuAction.ContextMenuIconType),
                Header = contextMenuAction.UseLocalization ? Localizer.Instance[contextMenuAction.DisplayName] : contextMenuAction.DisplayName,
                Tag = contextMenuAction,
                FontFamily = fontFamily,
                IsEnabled = contextMenuAction.IsEnabled
            };

            if (contextMenuAction.SubMenuItems.Count > 0)
            {
                var subMenus = new List<TemplatedControl>();

                foreach (var subAction in contextMenuAction.SubMenuItems)
                {
                    var subItem = new MenuItem()
                    {
                        Icon = GetMaterialIcon(subAction.ContextMenuIconType),
                        Header = subAction.UseLocalization ? Localizer.Instance[subAction.DisplayName] : subAction.DisplayName,
                        Tag = subAction,
                        FontFamily = fontFamily,
                        IsEnabled = subAction.IsEnabled
                    };

                    SetClickHandlers(subItem, subAction);
                    subMenus.Add(subItem);
                    if (subAction.AddSeparator) subMenus.Add(new Separator());
                }
                
                menuItem.ItemsSource = subMenus;
            }

            SetClickHandlers(menuItem, contextMenuAction);

            contextMenu.Items.Add(menuItem);

            if (contextMenuAction.AddSeparator) contextMenu.Items.Add(new Separator());
        }

        return contextMenu;
    }

    private static MaterialIcon? GetMaterialIcon(ContextMenuIconType contextMenuIconType, double size = 16)
    {
        var materialIconKind = contextMenuIconType.GetMaterialIconKind();
        if (materialIconKind == null) return null;

        return new MaterialIcon()
        {
            Kind = (MaterialIconKind)materialIconKind,
            Width = size,
            Height = size
        };
    }
}
