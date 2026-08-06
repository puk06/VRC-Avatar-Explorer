using Avalonia.Controls;
using Avalonia.Media.Imaging;

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
        Width = size;
        Height = size;
    }
}
