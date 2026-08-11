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
    public ImageInfo[] Thumbnails { get; init; } = [];

    [JsonPropertyName("variations")]
    public Variation[] Variations { get; init; } = [];

    public CategoryInfo Category { get; init; } = new();

    // これより下はAEの値
    [JsonIgnore]
    public ItemCategory EstimatedCategory { get; init; } = new(ItemType.None);

    [JsonIgnore]
    public string ThumbnailUrl => Thumbnails.Length > 0 ? Thumbnails[0].Original : string.Empty;
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

public record Variation
{
    [JsonPropertyName("id")]
    public int Id { get; init; } = -1;

    [JsonPropertyName("name")]
    public string? Name { get; init; } = null;

    [JsonPropertyName("price")]
    public int Price { get; init; } = -1;

    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    [JsonPropertyName("downloadables")]
    public Downloadables[] Downloadables { get; init; } = [];
}

public record Downloadables
{
    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = string.Empty;

    [JsonPropertyName("file_extension")]
    public string FileExtension { get; init; } = string.Empty;

    [JsonPropertyName("file_size")]
    public string FileSize { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; init; } = string.Empty;

    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; init; } = -1;

    public override string ToString()
    {
        return $"Name: {Name}, FileName: {FileName}, FileExtension: {FileExtension}, FileSize: {FileSize}, CreatedAt: {CreatedAt}, UpdatedAt: {UpdatedAt}, DisplayOrder: {DisplayOrder}";
    }
}
