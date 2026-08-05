using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ErrorLogOverlay : UserControl
{
    public ErrorLogOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ErrorLogVM;
    }
}
