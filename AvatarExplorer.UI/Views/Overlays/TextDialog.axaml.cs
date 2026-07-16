using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class TextDialogOverlay : UserControl
{
    public TextDialogOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.TextDialogVM;
    }
}
