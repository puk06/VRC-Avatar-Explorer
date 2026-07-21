using System.Collections.ObjectModel;
using AvatarExplorer.Core.Models.Common;
using AvatarExplorer.Core.Services.System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

// TODO: 未完成

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
        OpenFolderCommand = ReactiveCommand.Create(OpenFolder);
    }

    private void OnClose()
    {
        IsVisible = false;
    }

    private void OpenFolder()
    {
        // ボタンが押されたときの処理
    }
}
