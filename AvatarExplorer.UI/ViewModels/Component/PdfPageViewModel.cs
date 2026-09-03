using Avalonia.Media.Imaging;

namespace AvatarExplorer.UI.ViewModels.Component;

public class PdfPageViewModel(Bitmap image, string title) : ViewModelBase, IDisposable
{
    public Bitmap Image { get; } = image;
    public string Title { get; } = title;
    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        disposed = true;

        if (disposing)
        {
            Image.Dispose();
        }
    }
}
