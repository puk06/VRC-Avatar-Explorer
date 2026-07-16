using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Panels;

public partial class AdvancedSearch : UserControl
{
    public AdvancedSearch()
    {
        InitializeComponent();
        DataContext = MainViewModel.Instance.AdvancedSearchVM;
    }
}
