namespace AvatarExplorer.Core.Models.Items;

public class BulkImportItem(string itemId)
{
    public string Id { get; private set; } = Guid.NewGuid().ToString();
    public string ItemId { get; init; } = itemId;
    public string FilePath { get; set; } = string.Empty;
}
