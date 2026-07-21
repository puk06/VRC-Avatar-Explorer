using System;
using System.Collections.Generic;
using AvatarExplorer.UI.Models.Overlay;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class UnitypackageViewerViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public IEnumerable<UnitypackagePathNode> Nodes { get; set; } = [];
    [Reactive] public string FileName { get; set; } = string.Empty;
    [Reactive] public string Status { get; set; } = string.Empty;

    [Reactive] public UnitypackagePathNode? SelectedNode { get; set; } = null;
    [Reactive] public string SelectedPath { get; set; } = string.Empty;
    [Reactive] public bool IsExportable { get; set; } = false; // ISFILE

    public IReactiveCommand SelectionChangedCommand { get; }
    public IReactiveCommand ExportCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public UnitypackageViewerViewModel()
    {
        this.WhenAnyValue(i => i.SelectedNode)
            .Subscribe(UpdateNodeStatus);
            
        CloseCommand = ReactiveCommand.Create(OnClose);
    }

    private void UpdateNodeStatus(UnitypackagePathNode? node)
    {
        SelectedPath = node?.FullPath ?? string.Empty;
        IsExportable = node?.IsFile ?? false;
    }
    

    private void OnClose()
    {
        IsVisible = false;
    }
}
