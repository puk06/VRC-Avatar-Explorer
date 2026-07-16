using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class FetchAllThumbnailsOverlay : UserControl
{
    public FetchAllThumbnailsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.FetchAllThumbnailsVM;
    }
}
