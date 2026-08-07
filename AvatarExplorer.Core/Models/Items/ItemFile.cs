using AvatarExplorer.Core.Interfaces;
using AvatarExplorer.Core.Utils;

namespace AvatarExplorer.Core.Models.Items;

public class ItemFile(string folderPath, string filePath) : IIdentifiable
{
    public string ParentFolderPath { get; } = folderPath;
    public string ParentFolderName { get; } = Path.GetFileName(folderPath) ?? string.Empty;
    
    public string FilePath { get; } = filePath;
    public string FileName { get; } = Path.GetFileName(filePath) ?? string.Empty;
    public string Extension { get; } = string.IsNullOrEmpty(Path.GetExtension(filePath)) ? string.Empty : Path.GetExtension(filePath)[1.. ].ToUpper();

    public string Identifier => "file:" + PathUtils.ComputeHash(FilePath);
}
