using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ResolveTempAvatarOverlay : UserControl
{
    public ResolveTempAvatarOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ResolveTempAvatarVM;
    }

    private void OnItemImageLoaded(object? sender, RoutedEventArgs e)
    {
        if (sender is not Image image) return;

        var bitmapInterpolationMode = InstanceRepository.UserPreferences.AntiAliasingMode.GetInterpolationMode();
        if (bitmapInterpolationMode != BitmapInterpolationMode.None && bitmapInterpolationMode != BitmapInterpolationMode.Unspecified)
            RenderOptions.SetBitmapInterpolationMode(image, bitmapInterpolationMode);
    }
}
