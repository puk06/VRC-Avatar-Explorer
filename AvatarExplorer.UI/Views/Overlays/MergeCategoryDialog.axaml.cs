using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class MergeCategoryDialog : UserControl
{
    public MergeCategoryDialog()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.MergeCategoryVM;
    }
}
