using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public enum AvatarType
{
    None,
    Item,
    CommonAvatar,
    TempAvatar
}

public class Avatar : INavigationable
{
    public AvatarType Type { get; private set; } = AvatarType.None;
    public INavigationable Item { get; }

    public Avatar(INavigationable navigationable)
    {
        Item = navigationable;

        if (navigationable is Item) Type = AvatarType.Item;
        else if (navigationable is CommonAvatar) Type = AvatarType.CommonAvatar;
        else if (navigationable is TempAvatar) Type = AvatarType.TempAvatar;
    }

    public string Identifier => "avatar:" + Item.Identifier;
}
