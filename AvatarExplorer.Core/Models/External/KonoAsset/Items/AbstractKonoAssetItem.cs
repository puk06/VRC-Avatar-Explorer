using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset.Items;

/// <summary>
/// KonoAsset のアイテムの基底クラス。各アイテム種別で継承されます。
/// </summary>
public abstract class AbstractKonoAssetItem
{
    /// <summary>
    /// アイテムの一意識別子。
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// アイテムの説明（メタデータ）。
    /// </summary>
    [JsonPropertyName("description")]
    public KonoAssetDescription Description { get; set; } = new KonoAssetDescription();

    /// <summary>
    /// この KonoAsset アイテムを AvatarExplorer の Item に変換します。
    /// </summary>
    /// <returns>変換された Item。</returns>
    public abstract Item ToItem();
}
