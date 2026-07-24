using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
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
        RegisterPathScrollEvent();
        RegisterScrollTracking();
    }

    private void RegisterPathScrollEvent()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.PropertyChanged += OnPathSegmentsChanged;
        }

        PathScrollViewer.ScrollChanged += OnPathScrollViewerScrollChanged;
        PathScrollViewer.SizeChanged += OnPathScrollViewerSizeChanged;
    }

    private void OnPathSegmentsChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.PathSegments))
        {
            Dispatcher.UIThread.Post(UpdatePathOverflowAndScroll, DispatcherPriority.Render);
        }
    }

    private void OnPathScrollViewerScrollChanged(object? sender, ScrollChangedEventArgs e)
    {
        if (e.ExtentDelta != default || e.ViewportDelta != default)
        {
            UpdatePathOverflow();
        }
    }

    private void OnPathScrollViewerSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdatePathOverflowAndScroll();
    }

    private void UpdatePathOverflow()
    {
        if (DataContext is MainViewModel vm)
        {
            vm.HasOverflow = PathScrollViewer.Extent.Width > PathScrollViewer.Viewport.Width;
        }
    }

    private void UpdatePathOverflowAndScroll()
    {
        UpdatePathOverflow();
        ScrollPathToEnd();
    }

    private void ScrollPathToEnd()
    {
        var maxOffset = Math.Max(0, PathScrollViewer.Extent.Width - PathScrollViewer.Viewport.Width);
        PathScrollViewer.Offset = new Vector(maxOffset, 0);
    }

    private void RegisterScrollTracking()
    {
        Main_LeftPanelScrollViewer.ScrollChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.LeftPageInfo.ScrollOffset = Main_LeftPanelScrollViewer.Offset;
        };

        Main_RightPanelScrollViewer.ScrollChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
                vm.RightPageInfo.ScrollOffset = Main_RightPanelScrollViewer.Offset;
        };

        if (DataContext is MainViewModel mainVm)
        {
            mainVm.LeftPageInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.LeftPageInfo.RestoreScrollOffset))
                    Main_LeftPanelScrollViewer.Offset = mainVm.LeftPageInfo.RestoreScrollOffset;
            };

            mainVm.RightPageInfo.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainViewModel.RightPageInfo.RestoreScrollOffset))
                    Main_RightPanelScrollViewer.Offset = mainVm.RightPageInfo.RestoreScrollOffset;
            };
        }
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
