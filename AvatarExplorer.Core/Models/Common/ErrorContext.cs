namespace AvatarExplorer.Core.Models.Common;

public class ErrorContext(bool isInternal, string message, Exception? exception, string tag)
{
    public DateTime Date { get; } = DateTime.Now;
    public bool IsInternalError { get; } = isInternal;
    public string Message { get; } = message;
    public Exception? Exception { get; } = exception;
    public string Tag { get; } = tag;
}
