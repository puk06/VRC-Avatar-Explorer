using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class Avatar(Item item) : INavigationable
{
    private readonly Item _item = item;
    public Item Item => _item;

    public string Identifier => "avatar:" + _item.Identifier;
}
