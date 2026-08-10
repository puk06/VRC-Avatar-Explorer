using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
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

    public IReactiveCommand ResetCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    public ResetDatabaseViewModel()
    {
        ResetCommand = ReactiveCommand.CreateFromTask(Reset);
        CancelCommand = ReactiveCommand.Create(() => IsVisible = false);
    }

    public void Open()
    {
        ResetItems = true;
        ResetTempAvatars = true;
        ResetCommonAvatars = true;
        ResetBulkImportPresets = true;
        IsVisible = true;
    }

    private async Task Reset()
    {
        if (!ResetItems && !ResetTempAvatars && !ResetCommonAvatars && !ResetBulkImportPresets) return;

        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.ResetDatabase]
        );
        if (!result) return;

        await AvatarExplorerApp.Instance.BackupManager.ExecuteBackup();

        if (ResetItems)
        {
            AvatarExplorerApp.Instance.Items.Clear();
        }

        if (ResetTempAvatars)
        {
            AvatarExplorerApp.Instance.TempAvatars.Clear();
        }

        if (ResetCommonAvatars)
        {
            AvatarExplorerApp.Instance.CommonAvatars.Clear();
        }

        if (ResetBulkImportPresets)
        {
            AvatarExplorerApp.Instance.BulkImportPresets.Clear();
        }

        IsVisible = false;
    }
}
