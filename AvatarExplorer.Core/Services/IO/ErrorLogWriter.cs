using System.Globalization;
using System.Text;
using AvatarExplorer.Core.Data.Paths;

namespace AvatarExplorer.Core.Services.IO;

/// <summary>
/// エラー情報をログファイルに書き込むためのクラスです。スレッドセーフな書き込みと、通常エラー・内部エラーの区別に対応します。
/// </summary>
public class ErrorLogWriter : IDisposable
{
    private bool _disposed = false;
    private StreamWriter? _writer;
    private readonly Lock _syncLock = new();
    /// <summary>アプリケーション全体で共有される <see cref="ErrorLogWriter"/> のシングルトンインスタンスを取得します。</summary>
    public static readonly ErrorLogWriter Instance = new();

    private ErrorLogWriter()
    {
        FileSystemService.PrepareFileDirectory(LogFilePath);
    }

    /// <summary>ログの書き出し先となるファイルパスを取得します。</summary>
    public string LogFilePath { get; } = Path.Combine(SystemPath.LogsFolderPath, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".log");

    /// <summary>
    /// ユーザー向けのエラーとしてログを書き込みます。
    /// </summary>
    /// <param name="title">エラーのタイトル（概要）。</param>
    /// <param name="exception">関連する例外。省略可能です。</param>
    /// <param name="tag">補足メッセージ（エラーの詳細タグ）。</param>
    public void Write(string title, Exception? exception, string tag)
    {
        WriteCore("Error", title, exception, tag);
    }

    /// <summary>
    /// 内部エラーとしてログを書き込みます（通常は予期しない例外や内部不具合の記録に使用します）。
    /// </summary>
    /// <param name="title">エラーのタイトル（概要）。</param>
    /// <param name="exception">関連する例外。省略可能です。</param>
    /// <param name="tag">補足メッセージ（エラーの詳細タグ）。</param>
    public void InternalWrite(string title, Exception? exception, string tag)
    {
        WriteCore("Internal Error", title, exception, tag);
    }

    /// <summary>
    /// ログ書き込み用のリソースを解放します。複数回呼び出しても安全です。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        lock (_syncLock)
        {
            if (_disposed) return;

            if (disposing)
            {
                _writer?.Dispose();
                _writer = null;
            }

            _disposed = true;
        }
    }

    private void InitializeWriter()
    {
        if (_writer != null) return;

        var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };
    }

    private void WriteCore(string category, string title, Exception? exception, string tag)
    {
        lock (_syncLock)
        {
            if (_disposed) return;

            try
            {
                InitializeWriter();
                if (_writer == null) return;

                _writer.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {category}: {title}");
                if (!string.IsNullOrEmpty(tag)) _writer.WriteLine($"Tag Message: {tag}");
                if (exception != null) _writer.WriteLine(exception.ToString());
                _writer.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
