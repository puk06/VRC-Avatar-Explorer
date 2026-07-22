using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ItemEditorOverlay : UserControl
{
    public ItemEditorOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ItemEditorVM;
    }
}
