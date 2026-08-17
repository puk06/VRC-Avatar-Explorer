using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ArchivePasswordDialogOverlay : UserControl
{
    public ArchivePasswordDialogOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ArchivePasswordDialogVM;
    }
}
