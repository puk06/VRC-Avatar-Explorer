using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ArchivePasswordDialogOverlay : UserControl
{
    public ArchivePasswordDialogOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ArchivePasswordDialogVM;
    }
}
