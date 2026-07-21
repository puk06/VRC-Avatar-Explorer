using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class FatalErrorOverlay : UserControl
{
    public FatalErrorOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.FatalErrorVM;
    }
}
