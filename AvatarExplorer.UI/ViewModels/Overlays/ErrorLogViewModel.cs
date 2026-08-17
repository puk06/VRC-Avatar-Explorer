using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ErrorLogViewModel : ViewModelBase
{
    public ObservableCollection<ErrorContext> ErrorContexts { get; } = ErrorManager.Instance.ErrorContexts;
    [Reactive] public bool IsVisible { get; set; }
    public IReactiveCommand CloseCommand { get; }
    public IReactiveCommand OpenFolderCommand { get; }

    public ErrorLogViewModel()
    {
        CloseCommand = ReactiveCommand.Create(Close);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenLogFolder);
    }

    public void Open() => IsVisible = true;
    public void Close() => IsVisible = false;
    public static async Task OpenLogFolder() => await LauncherService.OpenFolder(SystemPath.LogsFolderPath);
}
