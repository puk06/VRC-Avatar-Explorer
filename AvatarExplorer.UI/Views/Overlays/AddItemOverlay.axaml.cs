using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class AddItemOverlay : UserControl
{
    public AddItemOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.AddItemVM;
    }
}
