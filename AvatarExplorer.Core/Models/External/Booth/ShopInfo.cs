using System.Text.Json.Serialization;

namespace AvatarExplorer.Core.Models.External.Booth;

public record ShopInfo
{
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("subdomain")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("thumbnail_url")]
    public string ThumbnailUrl { get; init; } = string.Empty;
}
