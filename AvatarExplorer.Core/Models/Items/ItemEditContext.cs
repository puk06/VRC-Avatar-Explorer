using AvatarExplorer.Core.Services.Network;

namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// 既存アイテムの更新時に必要な情報を保持するコンテキストクラスです。<see cref="ItemRepository.Update"/> に渡して使用します。null のプロパティは変更されません。
/// </summary>
public class ItemEditContext
{
    /// <summary>新しいタイトル。null の場合は変更されません。</summary>
    public string? Title { get; set; }
    /// <summary>新しい作者名。null の場合は変更されません。</summary>
    public string? Author { get; set; }
    /// <summary>新しい作者ID。null の場合は変更されません。</summary>
    public string? AuthorId { get; set; }
    /// <summary>新しいサムネイルURL。null の場合は変更されません。</summary>
    public string? ThumbnailUrl { get; set; }
    /// <summary>新しいBooth商品ID。null の場合は変更されません。</summary>
    public int? BoothId { get; set; }
    /// <summary>新しいアイテムタイプ。null の場合は変更されません。</summary>
    public ItemType? ItemType { get; set; }
    /// <summary>新しいカスタムカテゴリ名。null の場合は変更されません。</summary>
    public string? CustomCategory { get; set; }
    /// <summary>新しい対応アバター一覧。null の場合は変更されません。</summary>
    public IEnumerable<string>? SupportedAvatars { get; set; }
    /// <summary>新しい実装済みアバター一覧。null の場合は変更されません。</summary>
    public IEnumerable<string>? ImplementedAvatars { get; set; }
    /// <summary>新しいメモ。null の場合は変更されません。</summary>
    public string? ItemMemo { get; set; }
    /// <summary>新しいアイテムパス。null の場合は変更されません。</summary>
    public string? ItemPath { get; set; }
    /// <summary>新しいタグ一覧。null の場合は変更されません。</summary>
    public IEnumerable<string>? Tags { get; set; }
    /// <summary>新しい非表示フラグ。null の場合は変更されません。</summary>
    public bool? IsHidden { get; set; }
    /// <summary>新しい共通素体チェック除外フラグ。null の場合は変更されません。</summary>
    public bool? ExcludeFromCommonAvatarCheck { get; set; }

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
