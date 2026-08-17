using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ErrorLogOverlay : UserControl
{
    public ErrorLogOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ErrorLogVM;
    }
}
