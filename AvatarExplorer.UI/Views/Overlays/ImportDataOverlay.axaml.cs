using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ImportDataOverlay : UserControl
{
    public ImportDataOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.ImportDataVM;
    }
}
