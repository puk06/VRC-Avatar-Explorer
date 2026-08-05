using System.Text.Json.Serialization;

namespace AvatarExplorer.Core.Models.Items;

public class BulkImportItem(string itemId, string filePath)
{
    [JsonInclude] public string ItemId { get; private set; } = itemId;
    [JsonInclude] public string FilePath { get; private set; } = filePath;

    public void UpdateItemId(string itemId) => ItemId = itemId;
    public void UpdateItemPath(string path) => FilePath = path;
}
