using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class InitialSetupOverlay : UserControl
{
    public InitialSetupOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.InitialSetupVM;
    }
}
