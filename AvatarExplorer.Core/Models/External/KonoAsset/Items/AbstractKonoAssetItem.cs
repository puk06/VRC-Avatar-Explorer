using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

public abstract class AbstractKonoAssetItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public KonoAssetDescription Description { get; set; } = new KonoAssetDescription();

    public abstract Item ToItem();
}
