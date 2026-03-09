using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External.Booth;

public record BoothItem
{
    [JsonPropertyName("name")]
    public string Title { get; init; } = string.Empty;

    public ShopInfo Shop { get; init; } = new();

    [JsonPropertyName("id")]
    public int BoothId { get; init; } = -1;

    [JsonPropertyName("images")]
    public List<ImageInfo> Thumbnails { get; init; } = new();

    public CategoryInfo Category { get; init; } = new();

    // これより下はAEの値
    [JsonIgnore]
    public ItemType EstimatedCategory { get; init; } = ItemType.None;

    [JsonIgnore]
    public string AuthorId { get; init; } = string.Empty;

    [JsonIgnore]
    public string ThumbnailUrl => Thumbnails.Count > 0 ? Thumbnails[0].Original : string.Empty;
}
