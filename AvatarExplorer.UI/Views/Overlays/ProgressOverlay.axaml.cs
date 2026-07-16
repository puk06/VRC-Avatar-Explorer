using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ProgressOverlay : UserControl
{
    public ProgressOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ProgressVM;
    }
}
