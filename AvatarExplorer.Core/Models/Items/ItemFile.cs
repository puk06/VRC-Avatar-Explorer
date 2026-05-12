using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class ItemFile(string filePath) : ISelectableItem
{
    public string FullPath { get; } = filePath;
    public string FileName { get; } = Path.GetFileName(filePath);
    public string Extension { get; } = string.IsNullOrEmpty(Path.GetExtension(filePath)) ? string.Empty : Path.GetExtension(filePath)[1.. ].ToUpper();
}
