using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class Author : ISelectableItem
{
    public string Name { get; set; } = string.Empty;
    public string AuthorThumbnailFileName { get; set; } = string.Empty;
}
