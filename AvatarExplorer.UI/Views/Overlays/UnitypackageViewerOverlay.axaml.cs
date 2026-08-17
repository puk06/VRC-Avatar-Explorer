using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class UnitypackageViewerOverlay : UserControl
{
    public UnitypackageViewerOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.UnitypackageViewerVM;
    }
}
