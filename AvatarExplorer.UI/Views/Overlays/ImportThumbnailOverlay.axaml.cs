using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ImportThumbnailOverlay : UserControl
{
    public ImportThumbnailOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ImportThumbnailVM;
    }
}
