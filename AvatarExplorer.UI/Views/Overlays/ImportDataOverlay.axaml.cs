using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class ImportDataOverlay : UserControl
{
    public ImportDataOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.ImportDataVM;
    }
}
