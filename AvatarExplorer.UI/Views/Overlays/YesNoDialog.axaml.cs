using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class YesNoDialogOverlay : UserControl
{
    public YesNoDialogOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.YesNoDialogVM;
    }
}
