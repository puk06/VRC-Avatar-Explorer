using System.Threading.Tasks;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class TextDialogViewModel : ViewModelBase
{
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Content { get; set; } = string.Empty;
    private TaskCompletionSource<string?> _tcs = new();

    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public TextDialogViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(() => _tcs.SetResult(Content));
        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));
    }

    public Task<string?> WaitForResult()
    {
        _tcs = new TaskCompletionSource<string?>();
        return _tcs.Task;
    }
}
