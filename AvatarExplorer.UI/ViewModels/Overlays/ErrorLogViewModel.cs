using System.Collections.ObjectModel;
using System.Threading.Tasks;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Services.System;
using ReactiveUI;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ErrorLogViewModel : ViewModelBase
{
    public ObservableCollection<ErrorContext> ErrorContexts { get; } = ErrorManager.Instance.ErrorContexts;
    private TaskCompletionSource<string[]?> _tcs = new();
    public IReactiveCommand CloseCommand { get; }
    public IReactiveCommand OpenFolderCommand { get; }

    public ErrorLogViewModel()
    {
        CloseCommand = ReactiveCommand.Create(OnClose);
        OpenFolderCommand = ReactiveCommand.Create(OpenFolder);
    }

    private void OnClose()
    {
        // ボタンが押されたときの処理
    }

    private void OpenFolder()
    {
        // ボタンが押されたときの処理
    }
}
