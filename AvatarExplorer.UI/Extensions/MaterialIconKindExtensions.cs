using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.Attributes;
using AvatarExplorer.UI.Models.ContextMenu;
using Material.Icons;

namespace AvatarExplorer.UI.Extensions;

internal static class MaterialIconKindExtensions
{
    internal static MaterialIconKind? GetMaterialIconKind(this ContextMenuIconType contextMenuIconType)
    {
        return contextMenuIconType.GetAttribute<MaterialIconAttribute>()?.MaterialIconKind;
    }
}
