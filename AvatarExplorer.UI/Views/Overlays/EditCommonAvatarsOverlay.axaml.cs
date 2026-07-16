using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditCommonAvatarsOverlay : UserControl
{
    public EditCommonAvatarsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.EditCommonAvatarsVM;
    }
}
