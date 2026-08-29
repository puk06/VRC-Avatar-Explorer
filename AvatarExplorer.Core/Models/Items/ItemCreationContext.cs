using AvatarExplorer.Core.Services.Network;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// 新規アイテムの作成時に必要な情報を保持するコンテキストクラスです。<see cref="ItemRepository.Create"/> に渡して使用します。
/// </summary>
public class ItemCreationContext
{
    /// <summary>アイテムのタイトル（名前）です。</summary>
    public string Title { get; set; } = string.Empty;
    /// <summary>作者名です。</summary>
    public string Author { get; set; } = string.Empty;
    /// <summary>作者のBoothサブドメインIDです。</summary>
    public string AuthorId { get; set; } = string.Empty;
    /// <summary>サムネイル画像のURLです。指定すると作成時に自動でダウンロードされます。</summary>
    public string ThumbnailUrl { get; set; } = string.Empty;
    /// <summary>Boothの商品IDです。未設定の場合は -1 です。</summary>
    public int BoothId { get; set; } = -1;
    /// <summary>アイテムのタイプ（カテゴリ）です。</summary>
    public ItemType ItemType { get; set; } = ItemType.Avatar;
    /// <summary>カスタムカテゴリ名です。空の場合は ItemType が使用されます。</summary>
    public string CustomCategory { get; set; } = string.Empty;
    /// <summary>対応アバターの識別子（Identifier）一覧です。</summary>
    public IEnumerable<string> SupportedAvatars { get; set; } = [];
    /// <summary>アイテムのメモです。</summary>
    public string ItemMemo { get; set; } = string.Empty;
    /// <summary>タグの一覧です。</summary>
    public IEnumerable<string> Tags { get; set; } = [];

    /// <summary>指定した宛先にサムネイル画像をダウンロードします。</summary>
    /// <param name="destPath">ダウンロード先のファイルパス。</param>
    /// <param name="overwrite">既存ファイルを上書きする場合は true。</param>
    /// <returns>ダウンロードに成功した、または ThumbnailUrl が空でスキップされた場合は true、それ以外は false。</returns>
    public async Task<bool> FetchThumbnailAsync(string destPath, bool overwrite = false)
    {
        if (string.IsNullOrEmpty(ThumbnailUrl)) return false;
        return await Downloader.Fetch(ThumbnailUrl, destPath, overwrite);
    }
}
