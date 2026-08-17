using System.IO;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ArchivePasswordDialogViewModel : ViewModelBase
{
    public string FileName { get; set; } = string.Empty;
    public int CurrentAttempt { get; set; } = 0;
    public int MaxAttempt { get; set; } = 3;

    [Reactive] public string FileNameText { get; set; } = string.Empty;
    [Reactive] public string AttemptInfoText { get; set; } = string.Empty;
    [Reactive] public string Password { get; set; } = string.Empty;
    private TaskCompletionSource<string?> _tcs = new();

    public IReactiveCommand ConfirmCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ArchivePasswordDialogViewModel()
    {
        ConfirmCommand = ReactiveCommand.Create(Confirm);
        CancelCommand = ReactiveCommand.Create(Cancel);
    }

    public Task<string?> ShowAsync(ArchivePasswordRequest request)
    {
        FileName = Path.GetFileName(request.ArchivePath);
        CurrentAttempt = request.Attempt;
        MaxAttempt = request.MaxAttempts;

        FileNameText = $"{Localizer.Instance[Loc.ArchivePasswordDialog.FileName]}: {FileName}";
        AttemptInfoText = $"{Localizer.Instance[Loc.ArchivePasswordDialog.Attempts]}: {CurrentAttempt}/{MaxAttempt}";

        _tcs = new();
        return _tcs.Task;
    }

    public void Confirm() => _tcs.SetResult(Password);
    public void Cancel() => _tcs.SetResult(null);
}
