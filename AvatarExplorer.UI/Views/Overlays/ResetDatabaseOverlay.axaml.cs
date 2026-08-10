using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ResetDatabaseOverlay : UserControl
{
    public ResetDatabaseOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ResetDatabaseVM;
    }
}
