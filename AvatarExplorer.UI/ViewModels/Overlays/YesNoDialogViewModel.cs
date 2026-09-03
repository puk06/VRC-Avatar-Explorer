using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class YesNoDialogViewModel : ViewModelBase
{
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Content { get; set; } = string.Empty;
    private TaskCompletionSource<bool> _tcs = new();

    public IReactiveCommand YesCommand { get; }
    public IReactiveCommand NoCommand { get; }

    public YesNoDialogViewModel()
    {
        YesCommand = ReactiveCommand.Create(Yes);
        NoCommand = ReactiveCommand.Create(No);
    }

    public Task<bool> ShowAsync(string title, string content)
    {
        Title = title;
        Content = content;

        _tcs = new();
        return _tcs.Task;
    }

    private void Yes() => _tcs.SetResult(true);
    private void No() => _tcs.SetResult(false);
}
