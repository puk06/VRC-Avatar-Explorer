using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Overlay;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private string? _unitypackageViewerOverlay_unitypackagePath;
    private UnitypackagePathNode? _unitypackageViewerOverlay_selectedNode;

    private async Task UnitypackageViewerOverlay_OpenAsync(string unitypackagePath)
    {
        if (string.IsNullOrWhiteSpace(unitypackagePath) || !File.Exists(unitypackagePath))
        {
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.OpenFileFailed], isError: true);
            return;
        }

        _unitypackageViewerOverlay_unitypackagePath = unitypackagePath;
        _unitypackageViewerOverlay_selectedNode = null;

        // UnitypackageViewerOverlay_FileName.Text = Path.GetFileName(unitypackagePath);
        // UnitypackageViewerOverlay_StatusText.Text = "Loading...";
        // UnitypackageViewerOverlay_SelectedPathText.Text =  string.Empty;
        // UnitypackageViewerOverlay_ExportButton.IsEnabled = false;
        // UnitypackageViewerOverlay_TreeView.ItemsSource = null;
        // UnitypackageViewerOverlay.IsVisible = true;

        var result = await FileSystemService.GetUnitypackagePathnamesAsync(unitypackagePath);
        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to load unitypackage pathname list.", tag: result.Errors.ToErrorString());
            // UnitypackageViewerOverlay_StatusText.Text = "Failed to load pathname entries.";
            return;
        }

        var rootNodes = UnitypackageViewerOverlay_BuildPathTree(result.Value);
        // UnitypackageViewerOverlay_TreeView.ItemsSource = rootNodes;
        // UnitypackageViewerOverlay_StatusText.Text = $"{result.Value.Count} entries";
    }

    private void UnitypackageViewerOverlay_Close()
    {
        // UnitypackageViewerOverlay.IsVisible = false;
        // UnitypackageViewerOverlay_TreeView.ItemsSource = null;
        // UnitypackageViewerOverlay_FileName.Text = string.Empty;
        // UnitypackageViewerOverlay_StatusText.Text = string.Empty;
        // UnitypackageViewerOverlay_SelectedPathText.Text = string.Empty;
        // UnitypackageViewerOverlay_ExportButton.IsEnabled = false;
        _unitypackageViewerOverlay_unitypackagePath = null;
        _unitypackageViewerOverlay_selectedNode = null;
    }

    private static IEnumerable<UnitypackagePathNode> UnitypackageViewerOverlay_BuildPathTree(IEnumerable<string> pathnames)
    {
        var rootMap = new Dictionary<string, UnitypackagePathNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawPath in pathnames)
        {
            if (string.IsNullOrWhiteSpace(rawPath)) continue;

            var normalizedPath = rawPath.Trim().Replace('\\', '/');
            var segments = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (segments.Length == 0) continue;

            var rootSegment = segments[0];
            if (!rootMap.TryGetValue(rootSegment, out UnitypackagePathNode? currentNode))
            {
                currentNode = new UnitypackagePathNode(rootSegment, rootSegment);
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
            UnitypackageViewerOverlay_SortChildren(node);
        }

        return rootNodes;
    }

    private static void UnitypackageViewerOverlay_SortChildren(UnitypackagePathNode node)
    {
        node.Children.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase));

        foreach (var child in node.Children)
        {
            UnitypackageViewerOverlay_SortChildren(child);
        }
    }

    #region Event Handler
    private void UnitypackageViewerOverlay_TreeView_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        // if (UnitypackageViewerOverlay_TreeView.SelectedItem is UnitypackagePathNode selectedNode)
        // {
        //     _unitypackageViewerOverlay_selectedNode = selectedNode;
        //     UnitypackageViewerOverlay_SelectedPathText.Text = selectedNode.FullPath;
        //     UnitypackageViewerOverlay_ExportButton.IsEnabled = selectedNode.IsFile;
        //     return;
        // }

        // _unitypackageViewerOverlay_selectedNode = null;
        // UnitypackageViewerOverlay_SelectedPathText.Text = string.Empty;
        // UnitypackageViewerOverlay_ExportButton.IsEnabled = false;
    }

    private async void UnitypackageViewerOverlay_Export_Click(object? sender, RoutedEventArgs e)
    {
        if (_unitypackageViewerOverlay_unitypackagePath == null || _unitypackageViewerOverlay_selectedNode == null || !_unitypackageViewerOverlay_selectedNode.IsFile)
            return;

        var initialPath = Path.GetDirectoryName(_unitypackageViewerOverlay_unitypackagePath);
        var folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false, initialPath);
        if (folders == null || folders.Length == 0) return;

        var selectedFolder = folders[0];

        var result = await FileSystemService.ExtractUnitypackageAssetAsync(
            _unitypackageViewerOverlay_unitypackagePath,
            _unitypackageViewerOverlay_selectedNode.FullPath,
            selectedFolder
        );

        if (result.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to export unitypackage asset.", tag: result.Errors.ToErrorString());
            Main_ShowNotification(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ExportFailed], isError: true);
            return;
        }

        // UnitypackageViewerOverlay_StatusText.Text = $"{Localizer.Instance[LocalizationKey.Success.Export]}: {result.Value}";
    }

    private void UnitypackageViewerOverlay_Close_Click(object? sender, RoutedEventArgs e) => UnitypackageViewerOverlay_Close();
    # endregion
}
