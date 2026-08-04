namespace AvatarExplorer.Core.Models.Items;

public class BulkImportItem(string itemId, string filePath)
{
    public string ItemId { get; set; } = itemId;
    public string FilePath { get; set; } = filePath;
}
