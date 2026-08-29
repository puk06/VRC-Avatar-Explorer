namespace AvatarExplorer.Core.Data.Links;

/// <summary>
/// AvatarExplorer のリポジトリやアップデート確認等のソフトウェア関連リンク定数を提供します。
/// </summary>
public static class SoftwareLink
{
    /// <summary>
    /// GitHub リポジトリの URL。
    /// </summary>
    public const string RepositoryURL = "https://github.com/puk06/VRC-Avatar-Explorer";

    /// <summary>
    /// アップデート確認用の URL。
    /// </summary>
    public const string UpdateCheckURL = "https://update.pukosrv.net/check/avatarexplorerv2";

    /// <summary>
    /// 最新リリースページの URL。
    /// </summary>
    public const string LatestReleasePageURL = RepositoryURL + "/releases/latest";

    /// <summary>
    /// Issue 登録ページの URL。
    /// </summary>
    public const string IssuesURL = RepositoryURL + "/issues";

    /// <summary>
    /// 特定バージョンのリリースページの URL フォーマット（引数: バージョン）。
    /// </summary>
    public const string ReleasePageURL = RepositoryURL + "/releases/tag/v{0}";
}
