using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class UpdateDialogOverlay : UserControl
{
    public UpdateDialogOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.UpdateDialogVM;
    }
}
