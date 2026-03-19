using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class Author(string name) : ISelectableItem
{
    public string Name { get; set; } = name;
}
