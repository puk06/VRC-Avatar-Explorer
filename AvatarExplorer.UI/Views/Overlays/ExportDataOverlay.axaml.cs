using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ExportDataOverlay : UserControl
{
    public ExportDataOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ExportDataVM;
    }
}
