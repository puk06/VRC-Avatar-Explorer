using Avalonia.Controls;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class SidePanelManager
{
    private const double CollapsedWidth = 50;
    private const double DefaultExpandedWidth = 342;
    private const double MaxExpandedWidth = 550;
    private const double ExpandThreshold = 200;

    private readonly MainViewModel _vm;
    private SidePanelState _state = SidePanelState.Collapsed;
    private double _expandedWidth;

    public SidePanelManager(MainViewModel vm)
    {
        _vm = vm;
        _expandedWidth = DefaultExpandedWidth;
        ApplyState();
    }

    public void Open(int index)
    {
        _vm.SelectedSidePanelTab = index;
        _state = SidePanelState.Expanded;
        _expandedWidth = DefaultExpandedWidth;
        ApplyState();
    }

    public void OnButtonPressed(int index)
    {
        if (_vm.SelectedSidePanelTab != index) return;

        _state = SidePanelState.Collapsed;
        ApplyState();
    }

    public void OnWidthChanged(double newWidth)
    {
        if (_state == SidePanelState.Collapsed && newWidth > ExpandThreshold)
        {
            _state = SidePanelState.Expanded;
            _expandedWidth = newWidth;
            ApplyState();
        }
        else if (_state == SidePanelState.Expanded)
        {
            _expandedWidth = newWidth;
        }
    }

    private void ApplyState()
    {
        if (_state == SidePanelState.Expanded)
        {
            _vm.IsSidePanelVisible = true;
            _vm.SidePanelMinWidth = DefaultExpandedWidth;
            _vm.SidePanelMaxWidth = MaxExpandedWidth;
            _vm.SidePanelWidth = new GridLength(_expandedWidth);
        }
        else
        {
            _vm.IsSidePanelVisible = false;
            _vm.SidePanelMinWidth = CollapsedWidth;
            _vm.SidePanelMaxWidth = CollapsedWidth;
            _vm.SidePanelWidth = new GridLength(CollapsedWidth);
        }
    }
}
