using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External.V1;

public class ItemV1
{
    public string Title { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string ItemMemo { get; set; } = string.Empty;
    public string AuthorId { get; set; } = string.Empty;
    public int BoothId { get; set; } = -1;
    public string ItemPath { get; set; } = string.Empty;
    public string MaterialPath { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string AuthorImageUrl { get; set; } = string.Empty;
    public string AuthorImageFilePath { get; set; } = string.Empty;
    public ItemType Type { get; set; }
    public string CustomCategory { get; set; } = string.Empty;
    public List<string> SupportedAvatar { get; set; } = new List<string>();
    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public List<string> ImplementedAvatars { get; set; } = new List<string>();
    public List<string> Tags { get; set; } = new List<string>();
}
