using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class InitialSetupOverlay : UserControl
{
    public InitialSetupOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.InitialSetupVM;
    }
}
