using Avalonia.Controls;
using Avalonia.Media.Imaging;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views;

public partial class HoverThumbnailWindow : Window
{
    public HoverThumbnailWindow()
    {
        InitializeComponent();
    }

    public void SetImage(Bitmap? image)
    {
        HoverImage.Source = image;
    }

    public void SetSize(int size)
    {
        var scale = MainWindowViewModel.Instance?.UiScale ?? 1.0;
        Width = Height = size * scale;
    }
}
