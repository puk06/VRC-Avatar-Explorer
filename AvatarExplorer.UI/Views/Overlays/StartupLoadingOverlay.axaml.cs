using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class StartupLoadingOverlay : UserControl
{
    public StartupLoadingOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.StartupLoadingVM;
    }
}
