using System.Collections.Generic;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainViewModel : ViewModelBase
{
    [Reactive] public string Path { get; set; } = string.Empty;
    [Reactive] public string SearchText { get; set; } = string.Empty;

    [Reactive] public int SelectedCategory { get; set; } = 0;
    [Reactive] public IEnumerable<ItemViewModel> LeftItems { get; set; } = [];
    
    [Reactive] public IEnumerable<ItemViewModel> MainItems { get; set; } = [];

    [Reactive] public bool IsSidePanelVisible { get; set; } = false;
    [Reactive] public double SidePanelMinWidth { get; set; } = 50;

    public IReactiveCommand UndoCommand { get; }
    public IReactiveCommand HomeCommand { get; }
    public IReactiveCommand OpenSettingsCommand { get; }
    public IReactiveCommand AddItemCommand { get; }
    public IReactiveCommand OpenSidePanelCommand { get; }

    public IReactiveCommand SelectLeftItemCommand { get; }
    public IReactiveCommand SelectRightItemCommand { get; }

    public MainViewModel()
    {
        UpdateColumn();
        this.WhenAnyValue(i => i.SelectedCategory); // 左のリスト
    }

    private void UpdateColumn()
    {
        SidePanelMinWidth = IsSidePanelVisible ? 50 : 342;
    }
}
