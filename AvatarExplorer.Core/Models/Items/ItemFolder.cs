using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class ItemFolder(string folderPath, bool isRoot = false) : ISelectableItem
{
    public string FullPath { get; } = folderPath;
    public string FolderName { get; } = Path.GetFileName(folderPath);
    public bool IsRoot { get; } = isRoot;

    public static readonly string RootNodeName = "<sys>root";
}
