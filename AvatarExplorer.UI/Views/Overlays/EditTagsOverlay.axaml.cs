using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditTagsOverlay : UserControl
{
    public EditTagsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.EditTagsVM;
    }
}
