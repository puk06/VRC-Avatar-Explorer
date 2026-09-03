using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class EditMemoViewModel : ViewModelBase
{
    [Reactive] public string Memo { get; set; } = string.Empty;
    private TaskCompletionSource<string?> _tcs = new();

    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public EditMemoViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    public Task<string?> ShowAsync(string content)
    {
        Memo = content;
        _tcs = new();
        return _tcs.Task;
    }

    public void Confirm() => _tcs.SetResult(Memo);
    public void Cancel() => _tcs.SetResult(null);
}
