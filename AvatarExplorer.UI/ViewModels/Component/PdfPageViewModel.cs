using System;
using Avalonia.Media.Imaging;

namespace AvatarExplorer.UI.ViewModels.Component;

public class PdfPageViewModel(Bitmap image, string title) : ViewModelBase, IDisposable
{
    public Bitmap Image { get; } = image;
    public string Title { get; } = title;

    public void Dispose() => Image.Dispose();
}
