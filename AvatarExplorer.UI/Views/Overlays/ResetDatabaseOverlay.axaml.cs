using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ResetDatabaseOverlay : UserControl
{
    public ResetDatabaseOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ResetDatabaseVM;
    }
}
