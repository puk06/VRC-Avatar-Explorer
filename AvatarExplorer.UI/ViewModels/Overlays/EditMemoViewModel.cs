using System.Threading.Tasks;
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
        ConfirmCommand = ReactiveCommand.Create(() => _tcs.SetResult(Memo));
        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));
    }

    public Task<string?> Show(string content)
    {
        Memo = content;

        _tcs = new();

        return _tcs.Task;
    }
}
