using System.Text.Json.Serialization;

namespace AvatarExplorer.UI.Models.System;

public class ImportItemInfo
{
    [JsonPropertyName("item_id")]
    public string ItemID { get; set; } = string.Empty;

    [JsonPropertyName("variation_id")]
    public string VariationID { get; set; } = string.Empty;

    [JsonPropertyName("downloadable_filename")]
    public string DownloadableFilename { get; set; } = string.Empty;

    [JsonPropertyName("dlurl")]
    public string DownloadURL { get; set; } = string.Empty;
}
