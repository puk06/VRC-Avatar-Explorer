using Avalonia.Controls;
using Avalonia.Interactivity;
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

    private void OnCategoryToggleNegationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: ItemCategoryViewModel category } && DataContext is AdvancedSearchViewModel vm)
            vm.ToggleCategoryNegation(category);
    }

    private void OnTagToggleNegationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { DataContext: string tag } && DataContext is AdvancedSearchViewModel vm)
            vm.ToggleTagNegation(tag);
    }

    private void OnCategoryRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: ItemCategoryViewModel category } && DataContext is AdvancedSearchViewModel vm)
            vm.RemoveSelectedCategory(category);
    }

    private void OnTagRemoveClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Control { DataContext: string tag } && DataContext is AdvancedSearchViewModel vm)
            vm.RemoveSelectedTag(tag);
    }
}
