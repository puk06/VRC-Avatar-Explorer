using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class UnitypackageViewerOverlay : UserControl
{
    public UnitypackageViewerOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.UnitypackageViewerVM;
    }
}
