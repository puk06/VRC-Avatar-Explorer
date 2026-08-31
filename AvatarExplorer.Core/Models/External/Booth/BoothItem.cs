using System.Text.Json.Serialization;
using AvatarExplorer.Core.Models.Items;

namespace AvatarExplorer.Core.Models.External.Booth;

/// <summary>
/// Booth の商品情報を表します。BoothService.Fetch で取得されます。
/// </summary>
public record BoothItem
{
    /// <summary>
    /// 商品名。
    /// </summary>
    [JsonPropertyName("name")]
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// 販売ショップの情報。
    /// </summary>
    public ShopInfo Shop { get; init; } = new();

    /// <summary>
    /// Booth の商品 ID。
    /// </summary>
    [JsonPropertyName("id")]
    public int BoothId { get; init; } = -1;

    /// <summary>
    /// サムネイル画像の一覧。
    /// </summary>
    [JsonPropertyName("images")]
    public ImageInfo[] Thumbnails { get; init; } = [];

    /// <summary>
    /// バリエーションの一覧。
    /// </summary>
    [JsonPropertyName("variations")]
    public Variation[] Variations { get; init; } = [];

    /// <summary>
    /// 商品のカテゴリ情報。
    /// </summary>
    public CategoryInfo Category { get; init; } = new();

    // これより下はAEの値
    /// <summary>
    /// 商品名とカテゴリから推定された AvatarExplorer のアイテムカテゴリ。
    /// </summary>
    [JsonIgnore]
    public ItemCategory EstimatedCategory { get; init; } = ItemCategory.Get(ItemType.None);

    /// <summary>
    /// サムネイル画像のうち最初の画像の URL。画像が存在しない場合は空文字列。
    /// </summary>
    [JsonIgnore]
    public string ThumbnailUrl => Thumbnails.Length > 0 ? Thumbnails[0].Original : string.Empty;
}

/// <summary>
/// Booth のカテゴリ情報を表します。
/// </summary>
public record CategoryInfo
{
    /// <summary>
    /// カテゴリ名。
    /// </summary>
    public string Name { get; init; } = string.Empty;
}

/// <summary>
/// Booth の画像情報を表します。
/// </summary>
public record ImageInfo
{
    /// <summary>
    /// 元画像の URL。
    /// </summary>
    public string Original { get; init; } = string.Empty;
}

/// <summary>
/// Booth の販売ショップ情報を表します。
/// </summary>
public record ShopInfo
{
    /// <summary>
    /// ショップ名。
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// ショップの subdomain（作者 ID）。
    /// </summary>
    [JsonPropertyName("subdomain")]
    public string Id { get; init; } = string.Empty;

    /// <summary>
    /// ショップのサムネイル画像 URL。
    /// </summary>
    [JsonPropertyName("thumbnail_url")]
    public string ThumbnailUrl { get; init; } = string.Empty;
}

/// <summary>
/// Booth の商品バリエーションを表します。
/// </summary>
public record Variation
{
    /// <summary>
    /// バリエーション ID。
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; init; } = -1;

    /// <summary>
    /// バリエーション名。
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; } = null;

    /// <summary>
    /// 価格（円）。
    /// </summary>
    [JsonPropertyName("price")]
    public int Price { get; init; } = -1;

    /// <summary>
    /// バリエーションの種別。
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;

    /// <summary>
    /// ダウンロード可能なファイルの一覧。
    /// </summary>
    [JsonPropertyName("downloadables")]
    public Downloadables[] Downloadables { get; init; } = [];
}

/// <summary>
/// Booth のダウンロード可能なファイル情報を表します。
/// </summary>
public record Downloadables
{
    /// <summary>
    /// ファイル名。
    /// </summary>
    [JsonPropertyName("file_name")]
    public string FileName { get; init; } = string.Empty;

    /// <summary>
    /// ファイルの拡張子。
    /// </summary>
    [JsonPropertyName("file_extension")]
    public string FileExtension { get; init; } = string.Empty;

    /// <summary>
    /// ファイルサイズ。
    /// </summary>
    [JsonPropertyName("file_size")]
    public string FileSize { get; init; } = string.Empty;

    /// <summary>
    /// 表示名。
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 作成日時。
    /// </summary>
    [JsonPropertyName("created_at")]
    public string CreatedAt { get; init; } = string.Empty;

    /// <summary>
    /// 更新日時。
    /// </summary>
    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; init; } = string.Empty;

    /// <summary>
    /// 表示順序。
    /// </summary>
    [JsonPropertyName("display_order")]
    public int DisplayOrder { get; init; } = -1;

    /// <summary>
    /// このダウンロードファイルの内容を文字列として返します。
    /// </summary>
    public override string ToString()
    {
        return $"Name: {Name}, FileName: {FileName}, FileExtension: {FileExtension}, FileSize: {FileSize}, CreatedAt: {CreatedAt}, UpdatedAt: {UpdatedAt}, DisplayOrder: {DisplayOrder}";
    }
}
