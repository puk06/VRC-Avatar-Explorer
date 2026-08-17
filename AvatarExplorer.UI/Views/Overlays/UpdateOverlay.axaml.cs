using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class UpdateDialogOverlay : UserControl
{
    public UpdateDialogOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.UpdateDialogVM;
    }
}
