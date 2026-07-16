using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class PdfViewerOverlay : UserControl
{
    public PdfViewerOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.PdfViewerVM;
    }
}
