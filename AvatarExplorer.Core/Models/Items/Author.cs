using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class Author : IIdentifiable
{
    public string Name { get; set; } = string.Empty;
    public int ItemCount { get; set; } = 0;

    public string Identifier => "author:" + Name;
}
