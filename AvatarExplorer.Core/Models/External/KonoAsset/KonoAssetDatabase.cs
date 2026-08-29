using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.External.KonoAsset.Items;

namespace AvatarExplorer.Core.Models.External.KonoAsset;

/// <summary>
/// KonoAsset のデータベースファイル（JSON）をシリアライズするためのコンテナ。
/// </summary>
/// <typeparam name="T">KonoAsset のアイテム型（AbstractKonoAssetItem の派生型）。</typeparam>
public class KonoAssetDatabase<T>
    where T : AbstractKonoAssetItem
{
    /// <summary>
    /// データベースのスキーマバージョン。
    /// </summary>
    [JsonPropertyName("version")]
    public int Version { get; set; } = 3;

    /// <summary>
    /// 格納されている KonoAsset アイテムの一覧。
    /// </summary>
    [JsonPropertyName("data")]
    public List<T> Data { get; set; } = [];
}
