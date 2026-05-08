using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class ItemFolder(string folderPath) : ISelectableItem
{
    public string FullPath { get; } = folderPath;
    public string FolderName { get; } = Path.GetFileName(folderPath);
}
