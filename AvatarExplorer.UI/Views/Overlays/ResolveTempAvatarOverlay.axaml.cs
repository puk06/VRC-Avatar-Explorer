using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ResolveTempAvatarOverlay : UserControl
{
    public ResolveTempAvatarOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ResolveTempAvatarVM;
    }
}
