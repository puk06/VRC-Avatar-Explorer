using System.Windows.Input;
using Avalonia.Controls;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.UI.ViewModels.Panels;

namespace AvatarExplorer.UI.Views.Panels;

public partial class BulkImportPreset : UserControl
{
    public event Action? SelectionInvoked;

    private void OnPresetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: ItemViewModel item } || DataContext is not BulkImportPresetViewModel vm)
            return;

        if (vm.SelectItemCommand is not ICommand command || !command.CanExecute(item)) return;

        command.Execute(item);
        SelectionInvoked?.Invoke();
    }

    public BulkImportPreset()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.BulkImportPresetVM;
    }
}
