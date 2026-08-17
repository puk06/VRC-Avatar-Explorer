using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class TextDialogOverlay : UserControl
{
    public TextDialogOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.TextDialogVM;
    }
}
