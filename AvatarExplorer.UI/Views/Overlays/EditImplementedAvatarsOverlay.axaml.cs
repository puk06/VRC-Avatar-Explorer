using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditImplementedAvatarsOverlay : UserControl
{
    public EditImplementedAvatarsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.EditImplementedAvatarsVM;
    }
}
