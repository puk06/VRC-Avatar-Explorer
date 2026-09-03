using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public partial class TextDialogViewModel : ViewModelBase
{
    [Reactive] public partial string Title { get; set; } = string.Empty;
    [Reactive] public partial string Content { get; set; } = string.Empty;
    private TaskCompletionSource<string?> _tcs = new();

    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public TextDialogViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    public Task<string?> ShowAsync(string title, string content = "")
    {
        Title = title;
        Content = content;

        _tcs = new();
        return _tcs.Task;
    }

    private void Confirm() => _tcs.SetResult(Content);
    private void Cancel() => _tcs.SetResult(null);
}
