using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class MergeCategoryDialog : UserControl
{
    public MergeCategoryDialog()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.MergeCategoryVM;
    }
}
