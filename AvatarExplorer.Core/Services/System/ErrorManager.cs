using System.Collections.Immutable;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Services.System;

public class ErrorManager
{
    private readonly List<ErrorContext> _errorContexts = new();
    public static ErrorManager Instance { get; } = new();
    public ImmutableArray<ErrorContext> ErrorContexts => _errorContexts.ToImmutableArray();

    public event Action<string, Exception?, string>? OnErrorOccured;
    public event Action<string, Exception?, string>? OnInternalErrorOccured;

    private ErrorManager()
    {
    }

    // 内部処理のエラー
    public void PostInternalError(string message, Exception? exception = null, string tag = "")
    {
        _errorContexts.Add(new(true, message, exception, tag));
        OnInternalErrorOccured?.Invoke(message, exception, tag);
    }

    // AvaloniaなどのUIやLauncherのエラー
    public void PostError(string message, Exception? exception = null, string tag = "")
    {
        _errorContexts.Add(new(false, message, exception, tag));
        OnErrorOccured?.Invoke(message, exception, tag);
    }
}
