using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.MainVM;

        RegisterSidePanelEvent();
        RegisterCategoryTabEvent();
    }

    private void RegisterSidePanelEvent()
    {
        SidePanelTabControl.Items
            .OfType<TabItem>()
            .ForEach(i => i.AddHandler(
                PointerPressedEvent,
                SidePanelButton_OnPointerPressed,
                RoutingStrategies.Tunnel
            ));
    }

    private void RegisterCategoryTabEvent()
    {
        CategoryTabControl.SelectionChanged += OnCategorySelectionChanged;
    }

    private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is TabControl tab)
        {
            vm.OnCategoryChanged(tab.SelectedIndex);
        }
    }

    private void SidePanelButton_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm && sender is TabItem tab)
        {
            int index = ValueParser.Int((string?)tab.Tag);
            vm.SidePanelButtonPressed(index);
        }
    }
}
