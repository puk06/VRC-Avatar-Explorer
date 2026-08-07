using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public enum AvatarType
{
    None,
    Item,
    CommonAvatar,
    TempAvatar
}

public class Avatar : IIdentifiable
{
    public AvatarType Type { get; private set; } = AvatarType.None;
    public IIdentifiable Item { get; }
    public bool RawIdentifier { get; } // avatar:を付けるかどうかです

    public Avatar(IIdentifiable navigationable, bool rawIdentifier = false)
    {
        Item = navigationable;
        RawIdentifier = rawIdentifier;

        if (navigationable is Item) Type = AvatarType.Item;
        else if (navigationable is CommonAvatar) Type = AvatarType.CommonAvatar;
        else if (navigationable is TempAvatar) Type = AvatarType.TempAvatar;
    }

    public string Identifier => RawIdentifier ? Item.Identifier : "avatar:" + Item.Identifier;
}
