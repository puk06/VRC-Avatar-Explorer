using AvatarExplorer.UI.Attributes;
using Material.Icons;

namespace AvatarExplorer.UI.Models.ContextMenu;

internal enum ContextMenuIconType
{
    None,

    [MaterialIcon(MaterialIconKind.OpenInNew)]
    Open,

    [MaterialIcon(MaterialIconKind.ContentCopy)]
    Copy,

    [MaterialIcon(MaterialIconKind.Edit)]
    Edit,

    [MaterialIcon(MaterialIconKind.Add)]
    Add,

    [MaterialIcon(MaterialIconKind.Download)]
    Fetch,

    [MaterialIcon(MaterialIconKind.Delete)]
    Delete,

    [MaterialIcon(MaterialIconKind.LinkVariant)]
    Link,

    [MaterialIcon(MaterialIconKind.Merge)]
    Merge
}
