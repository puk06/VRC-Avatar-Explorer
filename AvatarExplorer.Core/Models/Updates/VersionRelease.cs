namespace AvatarExplorer.Core.Models.Updates;

/// <summary>
/// 特定のバージョンのリリース情報を表します。
/// </summary>
public class VersionRelease
{
    /// <summary>
    /// バージョン文字列（例: "2.8.0"）。
    /// </summary>
    public string Version { get; set; } = string.Empty;

    /// <summary>
    /// リリース日付。
    /// </summary>
    public string ReleaseDate { get; set; } = string.Empty;

    /// <summary>
    /// リリースページの URL。
    /// </summary>
    public string ReleaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// このリリースに含まれる変更履歴。
    /// </summary>
    public ChangeLog ChangeLogs { get; set; } = new();

    /// <summary>
    /// プラットフォーム等をキーとしたダウンロードアセットのマップ。
    /// </summary>
    public Dictionary<string, DownloadAsset> DownloadUrls { get; set; } = [];
}
