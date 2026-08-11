using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class TagEditorDialog : UserControl
{
    public TagEditorDialog()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.TagEditorVM;
    }
}
