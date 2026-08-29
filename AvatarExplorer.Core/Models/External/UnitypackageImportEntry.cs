namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// Unitypackage のインポート（パス変更・統合）処理の1入力エントリを表します。
/// </summary>
public class UnitypackageImportEntry
{
    /// <summary>
    /// 処理対象の Unitypackage ファイルのパス。
    /// </summary>
    public string FilePath { get; init; } = string.Empty;

    /// <summary>
    /// パス変更時に挿入するカテゴリの表示名。空の場合はパス変更を行いません。
    /// </summary>
    public string CategoryDisplayName { get; init; } = string.Empty;
}
