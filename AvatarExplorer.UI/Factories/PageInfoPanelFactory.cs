using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Navigation;
using Material.Icons;
using Material.Icons.Avalonia;

namespace AvatarExplorer.UI.Factories;

internal static class PageInfoPanelFactory
{
    private const string ButtonClass = "button";
    private const string PageButtonClass = "pagebutton";

    internal static Panel? CreatePageInfoPanel(ItemTagStates itemTagState, int currentPageValue, int itemsPerPage, int totalItemCount, EventHandler<RoutedEventArgs>? onClick = null)
    {
        int totalPages = (int)Math.Ceiling((double)totalItemCount / itemsPerPage);
        if (totalPages <= 0) return null;

        var pageInfoPanel = new Panel();

        var pageInfo = CreatePageInfoPanel(currentPageValue, totalPages, itemsPerPage, totalItemCount);

        var pageButtonGrid = new Grid() { ColumnDefinitions = new("Auto,Auto,*,Auto,Auto"), ColumnSpacing = 10, Margin = new(15, 0, 15, 0) };
        AddNavigationButtons(pageButtonGrid, itemTagState, currentPageValue, totalPages, onClick);

        pageInfoPanel.Children.Add(pageButtonGrid);
        pageInfoPanel.Children.Add(pageInfo);

        return pageInfoPanel;
    }

    private static StackPanel CreatePageInfoPanel(int currentPage, int totalPages, int itemsPerPage, int totalCount)
    {
        var panel = new StackPanel() { Orientation = Orientation.Vertical, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };

        var pageText = Localizer.Instance.Get(LocalizationKey.ItemWindow.CurrentPage, [(currentPage + 1).ToString(), totalPages.ToString()]);
        panel.Children.Add(new TextBlock { Text = pageText, FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center });

        int start = (currentPage * itemsPerPage) + 1;
        int end = Math.Min(start + itemsPerPage - 1, totalCount);
        var rangeText = Localizer.Instance.Get(LocalizationKey.ItemWindow.PageItemCount, [start.ToString(), end.ToString(), totalCount.ToString()]);
        panel.Children.Add(new TextBlock { Text = rangeText, FontSize = 15, HorizontalAlignment = HorizontalAlignment.Center });

        return panel;
    }

    private static void AddNavigationButtons(Grid grid, ItemTagStates state, int current, int total, EventHandler<RoutedEventArgs>? onClick)
    {
        if (current > 0) grid.Children.Add(CreateButton(GetMaterialIcon(MaterialIconKind.FirstPage), 0, new(state, PageButtonState.First, 0), onClick));
        if (current > 0) grid.Children.Add(CreateButton(GetMaterialIcon(MaterialIconKind.ChevronLeft), 1, new(state, PageButtonState.Back, current - 1), onClick));
        if (current < total - 1) grid.Children.Add(CreateButton(GetMaterialIcon(MaterialIconKind.ChevronRight), 3, new(state, PageButtonState.Next, current + 1), onClick));
        if (current < total - 1) grid.Children.Add(CreateButton(GetMaterialIcon(MaterialIconKind.LastPage), 4, new(state, PageButtonState.Last, total - 1), onClick));
    }

    private static MaterialIcon GetMaterialIcon(MaterialIconKind materialIconKind, double size = 25)
    {
        return new() { Kind = materialIconKind, Width = size, Height = size };
    }

    private static Button CreateButton(MaterialIcon content, int column, PageButtonInfo info, EventHandler<RoutedEventArgs>? onClick)
    {
        var button = new Button() { Content = content, HorizontalAlignment = HorizontalAlignment.Center, Tag = info };
        button.Classes.AddRange([ButtonClass, PageButtonClass]);
        if (onClick != null) button.Click += onClick;

        Grid.SetColumn(button, column);

        return button;
    }
}
