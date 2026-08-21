using System.Text.Json.Serialization;

namespace AvatarExplorer.Core.Models.External.KonoAsset;

public class KonoAssetDescription
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("creator")]
    public string Creator { get; set; } = string.Empty;

    [JsonPropertyName("imageFilename")]
    public string? ImageFilename { get; set; } = null;

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    [JsonPropertyName("memo")]
    public string? Memo { get; set; } = null;

    [JsonPropertyName("boothItemId")]
    public int? BoothItemId { get; set; } = null;

    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];

    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; } = 0;

    [JsonPropertyName("publishedAt")]
    public long? PublishedAt { get; set; } = null;
}
