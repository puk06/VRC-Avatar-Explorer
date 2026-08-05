namespace AvatarExplorer.Core.Models.Items;

public class ItemCreationContext
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public int BoothId { get; set; } = -1;
    public ItemType ItemType { get; set; } = ItemType.Avatar;
    public string CustomCategory { get; set; } = string.Empty;
    public IEnumerable<string> SupportedAvatars { get; set; } = [];
    public string ItemMemo { get; set; } = string.Empty;
    public IEnumerable<string> Tags { get; set; } = [];
}
