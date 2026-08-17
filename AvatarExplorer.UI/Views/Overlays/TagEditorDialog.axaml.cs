using Avalonia.Controls;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class TagEditorDialog : UserControl
{
    public TagEditorDialog()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.TagEditorVM;
    }
}
