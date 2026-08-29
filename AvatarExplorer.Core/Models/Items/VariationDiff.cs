namespace AvatarExplorer.Core.Models.Items;

/// <summary>
/// バリエーション間のファイルの差分を表すレコードです。追加・削除・変更の各ファイル一覧を保持します。
/// </summary>
/// <param name="Added">追加されたファイルの一覧。</param>
/// <param name="Removed">削除されたファイルの一覧。</param>
/// <param name="Changed">変更されたファイルの一覧。</param>
public record VariationDiff(
    IReadOnlyList<DownloadableFile> Added,
    IReadOnlyList<DownloadableFile> Removed,
    IReadOnlyList<DownloadableFile> Changed
)
{
    /// <summary>追加・削除・変更のいずれかが1件でも存在する（差分がある）かどうかを示します。</summary>
    public bool HasChanges => Added.Count > 0 || Removed.Count > 0 || Changed.Count > 0;
}
