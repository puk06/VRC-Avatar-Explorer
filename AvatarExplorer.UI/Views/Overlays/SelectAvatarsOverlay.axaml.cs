using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class SelectAvatarsOverlay : UserControl
{
    public SelectAvatarsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.SelectAvatarsVM;
    }
}
