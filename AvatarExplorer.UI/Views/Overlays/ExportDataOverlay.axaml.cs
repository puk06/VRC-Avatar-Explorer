using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ExportDataOverlay : UserControl
{
    public ExportDataOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ExportDataVM;
    }
}
