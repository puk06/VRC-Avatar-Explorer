using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class PdfViewerOverlay : UserControl
{
    public PdfViewerOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.PdfViewerVM;
    }
}
