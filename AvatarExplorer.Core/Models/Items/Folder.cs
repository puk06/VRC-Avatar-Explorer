using AvatarExplorer.Core.Interfaces;

namespace AvatarExplorer.Core.Models.Items;

public class Folder(string identifier) : INavigationable
{
    public string Title { get; set; } = string.Empty;
    public bool TitleLocalizable { get; set; } = false;
    public int ItemCount { get; set; } = 0;
    public string Identifier => identifier;
}
