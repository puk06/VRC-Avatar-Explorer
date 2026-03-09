using System.Globalization;
using System.Text;
using AvatarExplorer.Core.Data.Paths;

namespace AvatarExplorer.Core.Services.IO;

public class ErrorLogWriter : IDisposable
{
    private bool _disposed = false;
    private StreamWriter? _writer;
    private readonly string _logFilePath = Path.Combine(SystemPath.LogsFolderPath, DateTime.Now.ToString("yyyy-MM-dd HH-mm-ss", CultureInfo.InvariantCulture) + ".log");
    public static readonly ErrorLogWriter Instance = new();

    private ErrorLogWriter()
    {
        FileSystemService.PrepareFileDirectory(_logFilePath);
    }

    public void Write(string title, Exception? exception, string tag)
    {
        try
        {
            if (_writer == null) InitializeWriter();
            if (_writer == null) return;

            _writer.WriteLine($"Error: {title}");
            if (!string.IsNullOrEmpty(tag)) _writer.WriteLine($"Tag Message: {tag}");
            if (exception != null) _writer.WriteLine(exception.ToString());
            _writer.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void InternalWrite(string title, Exception? exception, string tag)
    {
        try
        {
            if (_writer == null) InitializeWriter();
            if (_writer == null) return;
    
            _writer.WriteLine($"Internal Error: {title}");
            if (!string.IsNullOrEmpty(tag)) _writer.WriteLine($"Tag Message: {tag}");
            if (exception != null) _writer.WriteLine(exception.ToString());
            _writer.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;

        if (disposing)
        {
            _writer?.Dispose();
        }

        _disposed = true;
    }

    private void InitializeWriter()
    {
        if (_writer != null) return;
        _writer = new StreamWriter(_logFilePath, false, Encoding.UTF8) { AutoFlush = true };
    }
}
