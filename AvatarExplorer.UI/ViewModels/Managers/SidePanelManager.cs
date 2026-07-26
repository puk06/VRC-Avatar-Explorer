using Avalonia.Controls;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class SidePanelManager
{
    private readonly MainViewModel _vm;

    public SidePanelManager(MainViewModel vm)
    {
        _vm = vm;
    }

    public void Open(string index)
    {
        if (!int.TryParse(index, out var selected)) return;

        _vm.SelectedSidePanelTab = selected;
        _vm.IsSidePanelVisible = true;
        UpdateLayout();
    }

    public void OnButtonPressed(int index)
    {
        if (_vm.SelectedSidePanelTab != index) return;

        _vm.IsSidePanelVisible = false;
        UpdateLayout();
    }

    public void UpdateLayout()
    {
        _vm.SidePanelMinWidth = _vm.IsSidePanelVisible ? 342 : 50;
        if (!_vm.IsSidePanelVisible)
        {
            _vm.SidePanelMaxWidth = 50;
            _vm.SidePanelWidth = new GridLength(_vm.SidePanelMinWidth);
            _vm.SidePanelMaxWidth = 550;
        }
    }
}
