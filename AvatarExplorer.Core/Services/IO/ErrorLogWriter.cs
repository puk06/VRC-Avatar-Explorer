using System.Globalization;
using System.Text;
using AvatarExplorer.Core.Data.Paths;

namespace AvatarExplorer.Core.Services.IO;

public class ErrorLogWriter : IDisposable
{
    private bool _disposed = false;
    private StreamWriter? _writer;
    private readonly Lock _syncLock = new();
    public static readonly ErrorLogWriter Instance = new();

    private ErrorLogWriter()
    {
        FileSystemService.PrepareFileDirectory(LogFilePath);
    }

    public string LogFilePath { get; } = Path.Combine(SystemPath.LogsFolderPath, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".log");

    public void Write(string title, Exception? exception, string tag)
    {
        WriteCore("Error", title, exception, tag);
    }

    public void InternalWrite(string title, Exception? exception, string tag)
    {
        WriteCore("Internal Error", title, exception, tag);
    }

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
