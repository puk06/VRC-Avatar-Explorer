using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditMemoOverlay : UserControl
{
    public EditMemoOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.EditMemoVM;
    }
}
