using Avalonia.Controls;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.ViewModels.Component;
using AvatarExplorer.UI.ViewModels.Panels;

namespace AvatarExplorer.UI.Views.Panels;

public partial class AdvancedSearch : UserControl
{
    public AdvancedSearch()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainView.AdvancedSearchVM;
    }

    private void OnCategorySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AdvancedSearchViewModel vm)
            vm.AddSelectedCategory();
    }

    private void OnTagSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is AdvancedSearchViewModel vm)
            vm.AddSelectedTag();
    }

    private void OnSelectedCategoryClick(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: ItemCategoryViewModel category } && DataContext is AdvancedSearchViewModel vm)
            vm.RemoveSelectedCategory(category);
    }

    private void OnSelectedTagClick(object? sender, Avalonia.Input.PointerPressedEventArgs e)
    {
        if (sender is Control { DataContext: string tag } && DataContext is AdvancedSearchViewModel vm)
            vm.RemoveSelectedTag(tag);
    }
}
