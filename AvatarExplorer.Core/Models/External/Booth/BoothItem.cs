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
    public ItemCategory EstimatedCategory { get; init; } = new(ItemType.None);

    [JsonIgnore]
    public string ThumbnailUrl => Thumbnails.Count > 0 ? Thumbnails[0].Original : string.Empty;
}

public record CategoryInfo
{
    public string Name { get; init; } = string.Empty;
}

public record ImageInfo
{
    public string Original { get; init; } = string.Empty;
}

public record ShopInfo
{
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("subdomain")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("thumbnail_url")]
    public string ThumbnailUrl { get; init; } = string.Empty;
}
