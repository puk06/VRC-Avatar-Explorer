using System.Text.Json.Serialization;

namespace AvatarExplorer.Core.Models.External.KonoAsset;

/// <summary>
/// KonoAsset のアイテム説明（メタデータ）を表します。
/// </summary>
public class KonoAssetDescription
{
    /// <summary>
    /// アイテム名。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 作成者名。
    /// </summary>
    [JsonPropertyName("creator")]
    public string Creator { get; set; } = string.Empty;

    /// <summary>
    /// 画像ファイル名（サムネイル等）。
    /// </summary>
    [JsonPropertyName("imageFilename")]
    public string? ImageFilename { get; set; } = null;

    /// <summary>
    /// タグの一覧。
    /// </summary>
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = [];

    /// <summary>
    /// メモ。
    /// </summary>
    [JsonPropertyName("memo")]
    public string? Memo { get; set; } = null;

    /// <summary>
    /// 関連する Booth の商品 ID。
    /// </summary>
    [JsonPropertyName("boothItemId")]
    public int? BoothItemId { get; set; } = null;

    /// <summary>
    /// 依存アセットの識別子一覧。
    /// </summary>
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = [];

    /// <summary>
    /// 作成日時（Unix タイムスタンプ）。
    /// </summary>
    [JsonPropertyName("createdAt")]
    public long CreatedAt { get; set; } = 0;

    /// <summary>
    /// 公開日時（Unix タイムスタンプ）。
    /// </summary>
    [JsonPropertyName("publishedAt")]
    public long? PublishedAt { get; set; } = null;
}
