using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ProgressOverlay : UserControl
{
    public ProgressOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ProgressVM;
    }
}
