namespace AvatarExplorer.Core.Models.Items;

public class ItemEditContext
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? AuthorId { get; set; }
    public int? BoothId { get; set; }
    public ItemType? ItemType { get; set; }
    public string? CustomCategory { get; set; }
    public IEnumerable<string>? SupportedAvatars { get; set; }
    public IEnumerable<string>? ImplementedAvatars { get; set; }
    public string? ItemMemo { get; set; }
    public string? ItemPath { get; set; }
    public IEnumerable<string>? Tags { get; set; }
}
