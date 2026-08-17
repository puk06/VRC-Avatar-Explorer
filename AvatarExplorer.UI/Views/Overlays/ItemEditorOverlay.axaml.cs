using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ItemEditorOverlay : UserControl
{
    public ItemEditorOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ItemEditorVM;
    }
}
