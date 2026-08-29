namespace AvatarExplorer.Core.Models.External;

/// <summary>
/// Unitypackage のインポートパス変更・統合処理の要求情報を表します。
/// </summary>
public class UnitypackageModifyRequest
{
    /// <summary>
    /// 処理対象となる Unitypackage の入力エントリ一覧。
    /// </summary>
    public required IReadOnlyList<UnitypackageImportEntry> Entries { get; init; }

    /// <summary>
    /// インポートパスを自動変更するかどうか。null の場合は RuntimeSettings の設定が使用されます。
    /// </summary>
    public bool? ChangeUnitypackagePath { get; init; }

    /// <summary>
    /// 処理の進捗を報告するコールバック。
    /// </summary>
    public Func<(string Message, int Percent), Task>? ReportProgress { get; init; }
}
