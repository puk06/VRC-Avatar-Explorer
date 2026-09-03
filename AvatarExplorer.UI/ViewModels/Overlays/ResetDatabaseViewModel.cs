using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ResetDatabaseViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }

    [Reactive] public bool ResetItems { get; set; } = true;
    [Reactive] public bool ResetTempAvatars { get; set; } = true;
    [Reactive] public bool ResetCommonAvatars { get; set; } = true;
    [Reactive] public bool ResetBulkImportPresets { get; set; } = true;
    [Reactive] public bool ResetVariationHashes { get; set; } = true;

    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ResetDatabaseViewModel()
    {
        ResetCommand = ReactiveCommand.CreateFromTask(Reset);
        CancelCommand = ReactiveCommand.Create(Close);
    }

    public void Open()
    {
        ResetItems = true;
        ResetTempAvatars = true;
        ResetCommonAvatars = true;
        ResetBulkImportPresets = true;
        ResetVariationHashes = true;
        IsVisible = true;
    }

    private async Task Reset()
    {
        if (!ResetItems && !ResetTempAvatars && !ResetCommonAvatars && !ResetBulkImportPresets && !ResetVariationHashes) return;

        var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.ResetDatabase]
        );
        if (!result) return;

        await InstanceRepository.BackupManager.ExecuteBackup();

        if (ResetItems)
            InstanceRepository.Items.Clear();

        if (ResetTempAvatars)
            InstanceRepository.TempAvatars.Clear();

        if (ResetCommonAvatars)
            InstanceRepository.CommonAvatars.Clear();

        if (ResetBulkImportPresets)
            InstanceRepository.BulkImportPresets.Clear();

        if (ResetVariationHashes)
            InstanceRepository.VariationHashes.Clear();

        Close();
    }

    private void Close() => IsVisible = false;
}
