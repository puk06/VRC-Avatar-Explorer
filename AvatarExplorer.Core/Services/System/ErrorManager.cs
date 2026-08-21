using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Services.System;

public sealed class ErrorManager
{
    public static ErrorManager Instance { get; } = new();

    private readonly List<ErrorContext> _errors = [];
    private readonly Lock _lock = new();

    public event Action<string, Exception?, string>? OnErrorOccured;
    public event Action<string, Exception?, string>? OnInternalErrorOccured;
    public event Action<ErrorContext>? OnErrorAdded;

    private ErrorManager()
    {
    }

    public IReadOnlyList<ErrorContext> GetErrors()
    {
        lock (_lock) return [.. _errors];
    }

    public void PostInternalError(string message, Exception? exception = null, string tag = "")
    {
        var context = new ErrorContext(true, message, exception, tag);
        lock (_lock) _errors.Add(context);
        OnErrorAdded?.Invoke(context);
        OnInternalErrorOccured?.Invoke(message, exception, tag);
    }

    public void PostError(string message, Exception? exception = null, string tag = "")
    {
        var context = new ErrorContext(false, message, exception, tag);
        lock (_lock) _errors.Add(context);
        OnErrorAdded?.Invoke(context);
        OnErrorOccured?.Invoke(message, exception, tag);
    }
}
