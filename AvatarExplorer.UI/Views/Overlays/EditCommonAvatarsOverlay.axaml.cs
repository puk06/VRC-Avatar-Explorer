using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditCommonAvatarsOverlay : UserControl
{
    public EditCommonAvatarsOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.EditCommonAvatarsVM;
    }
}
