using System.Collections.ObjectModel;
using Avalonia.Threading;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ErrorLogViewModel : ViewModelBase
{
    public ObservableCollection<ErrorContext> ErrorContexts { get; } = [];
    [Reactive] public bool IsVisible { get; set; }
    public IReactiveCommand CloseCommand { get; }
    public IReactiveCommand OpenFolderCommand { get; }

    public ErrorLogViewModel()
    {
        foreach (var error in ErrorManager.Instance.GetErrors())
            ErrorContexts.Add(error);

        ErrorManager.Instance.OnErrorAdded += OnErrorAdded;

        CloseCommand = ReactiveCommand.Create(Close);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenLogFolder);
    }

    private void OnErrorAdded(ErrorContext context) => Dispatcher.UIThread.Post(() => ErrorContexts.Add(context));

    public void Open() => IsVisible = true;
    public void Close() => IsVisible = false;
    public static Task OpenLogFolder() => LauncherService.OpenFolder(SystemPath.LogsFolderPath);
}
