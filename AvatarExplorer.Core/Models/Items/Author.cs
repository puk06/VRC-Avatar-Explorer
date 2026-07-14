using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class Author : ISelectableItem
{
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; } = 0;

    public string Identifier => "author:" + Name;
}
