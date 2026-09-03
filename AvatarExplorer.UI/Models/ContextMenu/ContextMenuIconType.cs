using AvatarExplorer.UI.Attributes;
using Material.Icons;

namespace AvatarExplorer.UI.Models.ContextMenu;

public enum ContextMenuIconType
{
    None,

    [MaterialIcon(MaterialIconKind.Reload)]
    Update,

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
    Merge,

    [MaterialIcon(MaterialIconKind.EyeOutline)]
    Visible,

    [MaterialIcon(MaterialIconKind.EyeOffOutline)]
    Hidden,

    [MaterialIcon(MaterialIconKind.AccountMultiple)]
    Avatars,

    [MaterialIcon(MaterialIconKind.AccountMultipleCheck)]
    IncludeCommonAvatarCheck,

    [MaterialIcon(MaterialIconKind.AccountMultipleRemove)]
    ExcludeCommonAvatarCheck,

    [MaterialIcon(MaterialIconKind.AccountCheck)]
    Implemented,

    [MaterialIcon(MaterialIconKind.AccountRemove)]
    NotImplemented,

    [MaterialIcon(MaterialIconKind.Tag)]
    Tag,

    [MaterialIcon(MaterialIconKind.NoteEdit)]
    NoteEdit,

    [MaterialIcon(MaterialIconKind.FilePlus)]
    AddFile,

    [MaterialIcon(MaterialIconKind.FolderPlus)]
    AddFolder,

    [MaterialIcon(MaterialIconKind.LinkPlus)]
    AddUrl
}
