using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class YesNoDialogOverlay : UserControl
{
    public YesNoDialogOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.YesNoDialogVM;
    }
}
