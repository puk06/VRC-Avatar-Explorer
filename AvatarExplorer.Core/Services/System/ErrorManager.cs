using System.Collections.ObjectModel;
using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Services.System;

public class ErrorManager
{
    public static ErrorManager Instance { get; } = new();
    public ObservableCollection<ErrorContext> ErrorContexts { get; } = [];

    public event Action<string, Exception?, string>? OnErrorOccured;
    public event Action<string, Exception?, string>? OnInternalErrorOccured;

    private ErrorManager()
    {
    }

    // 内部処理のエラー
    public void PostInternalError(string message, Exception? exception = null, string tag = "")
    {
        ErrorContexts.Add(new(true, message, exception, tag));
        OnInternalErrorOccured?.Invoke(message, exception, tag);
    }

    // AvaloniaなどのUIやLauncherのエラー
    public void PostError(string message, Exception? exception = null, string tag = "")
    {
        ErrorContexts.Add(new(false, message, exception, tag));
        OnErrorOccured?.Invoke(message, exception, tag);
    }
}
