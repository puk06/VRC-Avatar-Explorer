using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class FetchAllThumbnailsOverlay : UserControl
{
    public FetchAllThumbnailsOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.FetchAllThumbnailsVM;
    }
}
