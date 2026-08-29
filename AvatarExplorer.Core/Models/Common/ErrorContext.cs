namespace AvatarExplorer.Core.Models.Common;

/// <summary>
/// 発生したエラーを保持するコンテキスト。内部エラーかどうかや発生タグと共に記録されます。
/// </summary>
public class ErrorContext(bool isInternal, string message, Exception? exception, string tag)
{
    /// <summary>
    /// エラーが発生した日時。
    /// </summary>
    public DateTime Date { get; } = DateTime.Now;

    /// <summary>
    /// 内部エラー（バグ等）かどうか。
    /// </summary>
    public bool IsInternalError { get; } = isInternal;

    /// <summary>
    /// エラーメッセージ。
    /// </summary>
    public string Message { get; } = message;

    /// <summary>
    /// 例外情報（存在する場合）。
    /// </summary>
    public Exception? Exception { get; } = exception;

    /// <summary>
    /// エラーの発生元を示すタグ。
    /// </summary>
    public string Tag { get; } = tag;
}
