namespace AvatarExplorer.Core.Models.External.V1;

/// <summary>
/// AvatarExplorer V1 形式におけるアイテムのデータを表します。
/// </summary>
public class ItemV1
{
    /// <summary>
    /// アイテム名。
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// 作者名。
    /// </summary>
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// アイテムのメモ。
    /// </summary>
    public string ItemMemo { get; set; } = string.Empty;

    /// <summary>
    /// 作者 ID（Booth の subdomain）。
    /// </summary>
    public string AuthorId { get; set; } = string.Empty;

    /// <summary>
    /// Booth の商品 ID。
    /// </summary>
    public int BoothId { get; set; } = -1;

    /// <summary>
    /// アイテムのルートパス。
    /// </summary>
    public string ItemPath { get; set; } = string.Empty;

    /// <summary>
    /// マテリアルのパス。
    /// </summary>
    public string MaterialPath { get; set; } = string.Empty;

    /// <summary>
    /// サムネイルの URL。
    /// </summary>
    public string ThumbnailUrl { get; set; } = string.Empty;

    /// <summary>
    /// 画像のローカルパス。
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// 作者画像の URL。
    /// </summary>
    public string AuthorImageUrl { get; set; } = string.Empty;

    /// <summary>
    /// 作者画像のローカルパス。
    /// </summary>
    public string AuthorImageFilePath { get; set; } = string.Empty;

    /// <summary>
    /// アイテムのタイプ（ItemType の整数値）。
    /// </summary>
    public int Type { get; set; }

    /// <summary>
    /// カスタムカテゴリ名。
    /// </summary>
    public string CustomCategory { get; set; } = string.Empty;

    /// <summary>
    /// 対応アバターの識別子一覧。
    /// </summary>
    public List<string> SupportedAvatar { get; set; } = [];

    /// <summary>
    /// 作成日時（文字列）。
    /// </summary>
    public string CreatedDate { get; set; } = string.Empty;

    /// <summary>
    /// 更新日時（文字列）。
    /// </summary>
    public string UpdatedDate { get; set; } = string.Empty;

    /// <summary>
    /// 実装済みアバターの識別子一覧。
    /// </summary>
    public List<string> ImplementedAvatars { get; set; } = [];

    /// <summary>
    /// タグの一覧。
    /// </summary>
    public List<string> Tags { get; set; } = [];
}
