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
        CloseCommand = ReactiveCommand.Create(OnClose);
        OpenFolderCommand = ReactiveCommand.CreateFromTask(OpenFolder);
    }

    private void OnClose()
    {
        IsVisible = false;
    }

    private async Task OpenFolder()
    {
        await LauncherService.OpenFolder(TopLevelProvider.Current, SystemPath.LogsFolderPath);
    }
}
