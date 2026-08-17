using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class EditMemoOverlay : UserControl
{
    public EditMemoOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.EditMemoVM;
    }
}
