using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using AvatarExplorer.UI.ViewModels;
using Avalonia.Platform.Storage;
using AvatarExplorer.Core.Services.System;
using Avalonia.Threading;

namespace AvatarExplorer.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        MainWindowViewModel.Instance.NotificationManager = NotificationManager;
        SingleInstanceService.OnPipeMessageReceived += OnPipeMessageReceived;
    }

    private void OnPipeMessageReceived(string[] args)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            if (WindowState == WindowState.Minimized) WindowState = WindowState.Normal;
            Topmost = true;
            Activate();
            Topmost = false;
        });
    }

    private void OnDragDropOver(object? sender, DragEventArgs e)
    {
        // ファイルのD&D: File | アイテムボタンのD&D: Text
        if (e.DataTransfer.Contains(DataFormat.File) || e.DataTransfer.Contains(DataFormat.Text)) e.DragEffects = DragDropEffects.Copy;
    }
    private void OnDragDrop(object? sender, DragEventArgs e)
    {
        // ファイルのみ受け付ける
        if (!e.DataTransfer.Contains(DataFormat.File)) return;

        var storageItems = e.DataTransfer.GetItems(DataFormat.File).Select(i => i.TryGetFile());
        if (storageItems == null) return;

        var storageItemPaths = storageItems
            .Select(i => i?.TryGetLocalPath())
            .Where(i => !string.IsNullOrEmpty(i) && (Directory.Exists(i) || File.Exists(i)))
            .Cast<string>()
            .ToArray();

        if (DataContext is MainWindowViewModel vm)
        {
            vm.OnFilesDrop(storageItemPaths);
        }
    }

    public void SendApplicationArgs(string[]? args)
    {
        if (args == null) return;

        if (DataContext is MainWindowViewModel vm)
            vm.SendApplicationArgs(args);
    }

    private void OnWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        MainWindowViewModel.Instance.OnWindowClosing();
    }
}
