using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Databases;

public class KonoAssetAvatarDatabase
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 3;

    [JsonPropertyName("data")]
    public List<KonoAssetAvatarItem> Data { get; set; } = new List<KonoAssetAvatarItem>();
}
