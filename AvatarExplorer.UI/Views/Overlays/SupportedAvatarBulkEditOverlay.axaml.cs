using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class SupportedAvatarBulkEditOverlay : UserControl
{
    public SupportedAvatarBulkEditOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.SupportedAvatarBulkEditVM;
    }
}
