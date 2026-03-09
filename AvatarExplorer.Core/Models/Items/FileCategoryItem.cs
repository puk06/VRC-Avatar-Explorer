using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class FileCategoryItem(ItemFileCategoryType fileCategory = ItemFileCategoryType.None) : ISelectableItem
{
    public ItemFileCategoryType FileCategory { get; set; } = fileCategory;
    public List<string> FilePaths { get; } = new List<string>();
}
