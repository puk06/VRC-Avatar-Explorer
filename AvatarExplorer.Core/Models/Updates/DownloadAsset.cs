namespace AvatarExplorer.Core.Models.Updates;

/// <summary>
/// アップデートでダウンロード可能なアセット（ファイル）を表します。
/// </summary>
public class DownloadAsset
{
    /// <summary>
    /// ダウンロード先の URL。
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// ファイルの SHA256 ハッシュ値（検証用）。
    /// </summary>
    public string Sha256 { get; set; } = string.Empty;
}
