using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Models.Items;

public class ItemFile(string folderPath, string filePath) : ISelectableItem
{
    public string ParentFolderPath { get; } = folderPath; // UI側で開くためにこれはフルパスが良いかも
    public string ParentFolderName { get; } = Path.GetFileName(folderPath) ?? string.Empty;
    
    public string FilePath { get; } = filePath;
    public string FileName { get; } = Path.GetFileName(filePath) ?? string.Empty;
    public string Extension { get; } = string.IsNullOrEmpty(Path.GetExtension(filePath)) ? string.Empty : Path.GetExtension(filePath)[1.. ].ToUpper();

    public string Identifier => "file:" + FilePath;
}
