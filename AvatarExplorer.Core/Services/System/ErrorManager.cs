using AvatarExplorer.Core.Models.Common;

namespace AvatarExplorer.Core.Services.System;

/// <summary>アプリケーション内で発生したエラー（一般エラー・内部エラー）を収集し、イベントで通知するシングルトンマネージャー。</summary>
public sealed class ErrorManager
{
    /// <summary>エラーマネージャーのシングルトンインスタンス。</summary>
    public static ErrorManager Instance { get; } = new();

    private readonly List<ErrorContext> _errors = [];
    private readonly Lock _lock = new();

    /// <summary>一般エラーが発生したときに発生するイベント。(メッセージ, 例外, タグ) が渡されます。</summary>
    public event Action<string, Exception?, string>? OnErrorOccured;
    /// <summary>内部エラーが発生したときに発生するイベント。(メッセージ, 例外, タグ) が渡されます。</summary>
    public event Action<string, Exception?, string>? OnInternalErrorOccured;
    /// <summary>エラーが記録されたときに発生するイベント。詳細な <see cref="ErrorContext"/> が渡されます。</summary>
    public event Action<ErrorContext>? OnErrorAdded;

    private ErrorManager()
    {
    }

    /// <summary>これまでに記録された全エラーの読み取り専用リストを取得します。</summary>
    /// <returns>記録されたエラーコンテキストのリスト。</returns>
    public IReadOnlyList<ErrorContext> GetErrors()
    {
        lock (_lock) return [.. _errors];
    }

    /// <summary>内部エラーとして記録し、<see cref="OnInternalErrorOccured"/> および <see cref="OnErrorAdded"/> イベントを発火します。通常は予期しない処理失敗に使用します。</summary>
    /// <param name="message">エラーメッセージ。</param>
    /// <param name="exception">関連する例外（任意）。</param>
    /// <param name="tag">エラーを分類するためのタグ（任意）。</param>
    public void PostInternalError(string message, Exception? exception = null, string tag = "")
    {
        var context = new ErrorContext(true, message, exception, tag);
        lock (_lock) _errors.Add(context);
        OnErrorAdded?.Invoke(context);
        OnInternalErrorOccured?.Invoke(message, exception, tag);
    }

    /// <summary>一般エラーとして記録し、<see cref="OnErrorOccured"/> および <see cref="OnErrorAdded"/> イベントを発火します。ユーザーへの通知対象となるエラーに使用します。</summary>
    /// <param name="message">エラーメッセージ。</param>
    /// <param name="exception">関連する例外（任意）。</param>
    /// <param name="tag">エラーを分類するためのタグ（任意）。</param>
    public void PostError(string message, Exception? exception = null, string tag = "")
    {
        var context = new ErrorContext(false, message, exception, tag);
        lock (_lock) _errors.Add(context);
        OnErrorAdded?.Invoke(context);
        OnErrorOccured?.Invoke(message, exception, tag);
    }
}
