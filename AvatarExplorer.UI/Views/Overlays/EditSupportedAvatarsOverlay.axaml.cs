using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditSupportedAvatarsOverlay : UserControl
{
    public EditSupportedAvatarsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.EditSupportedAvatarsVM;
    }
}
