using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class SelectAvatarsOverlay : UserControl
{
    public SelectAvatarsOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.SelectAvatarsVM;
    }
}
