using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class UnitypackageViewerViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }

    public string UnitypackagePath { get; set; } = string.Empty;

    [Reactive] public IEnumerable<UnitypackagePathNodeViewModel> Nodes { get; set; } = [];
    [Reactive] public string FileName { get; set; } = string.Empty;
    [Reactive] public string Status { get; set; } = string.Empty;

    [Reactive] public UnitypackagePathNodeViewModel? SelectedNode { get; set; } = null;
    [Reactive] public string SelectedPath { get; set; } = string.Empty;
    [Reactive] public bool IsExportable { get; set; } = false; // ISFILE

    public IReactiveCommand ExportCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public UnitypackageViewerViewModel()
    {
        ExportCommand = ReactiveCommand.CreateFromTask(Export);
        CloseCommand = ReactiveCommand.Create(OnClose);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(i => i.SelectedNode)
            .Subscribe(UpdateNodeStatus);
    }

    public async void Open(string unitypackagePath)
    {
        IsVisible = true;
        await LoadAsync(unitypackagePath);
    }

    private async Task LoadAsync(string unitypackagePath)
    {
        UnitypackagePath = unitypackagePath;

        SelectedNode = null;
        SelectedPath = string.Empty;
        IsExportable = false;

        FileName = Path.GetFileName(unitypackagePath);
        Status = Localizer.Instance[Loc.UnitypackageViewer.Status.Loading];

        var result = await FileSystemService.GetUnitypackagePathnamesAsync(unitypackagePath);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to load unitypackage pathname list.", tag: result.Errors.ToErrorString());
            Status = Localizer.Instance[Loc.UnitypackageViewer.Status.LoadFailed];
            return;
        }

        Nodes = BuildPathTree(result.Value);
        Status = Localizer.Instance.Get(Loc.UnitypackageViewer.Status.Entries, [result.Value.Count.ToString()]);
    }

    private static IEnumerable<UnitypackagePathNodeViewModel> BuildPathTree(IEnumerable<string> pathnames)
    {
        var rootMap = new Dictionary<string, UnitypackagePathNodeViewModel>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawPath in pathnames)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) continue;

            var normalizedPath = rawPath.Trim().Replace('\\', '/');
            var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) continue;

            var rootSegment = segments[0];
            if (!rootMap.TryGetValue(rootSegment, out UnitypackagePathNodeViewModel? currentNode))
            {
                currentNode = new UnitypackagePathNodeViewModel(rootSegment, rootSegment);
                rootMap[rootSegment] = currentNode;
            }

            for (int i = 1; i < segments.Length; i++)
            {
                currentNode = currentNode.GetOrAddChild(segments[i]);
            }

            currentNode.MarkAsFile();
        }

        var rootNodes = rootMap.Values.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase);
        foreach (var node in rootNodes)
        {
            SortChildren(node);
        }

        return rootNodes;
    }
    private static void SortChildren(UnitypackagePathNodeViewModel node)
    {
        node.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

        foreach (var child in node.Children)
        {
            SortChildren(child);
        }
    }

    private void UpdateNodeStatus(UnitypackagePathNodeViewModel? node)
    {
        SelectedPath = node?.FullPath ?? string.Empty;
        IsExportable = node?.IsFile ?? false;
    }

    private async Task Export()
    {
        if (UnitypackagePath == null || SelectedNode == null || !SelectedNode.IsFile)
            return;

        var initialPath = Path.GetDirectoryName(UnitypackagePath);
        var folders = await StorageService.OpenFolderDialog(
            TopLevelProvider.Current,
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            allowMultiple: false,
            initialPath
        );
        if (folders == null || folders.Length == 0) return;

        var selectedFolder = folders[0];

        var result = await FileSystemService.ExtractUnitypackageAssetAsync(
            UnitypackagePath,
            SelectedNode.FullPath,
            selectedFolder
        );

        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to export unitypackage asset.", tag: result.Errors.ToErrorString());
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ExportFailed],
                NotificationType.Error
            );
            return;
        }

        Status = $"{Localizer.Instance[Loc.Success.Export]}: {result.Value}";
    }

    private void OnClose()
    {
        IsVisible = false;
    }
}
