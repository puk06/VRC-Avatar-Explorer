using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset;

public class KonoAssetDatabase<T>
    where T : AbstractKonoAssetItem
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 3;

    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = new List<T>();
}
