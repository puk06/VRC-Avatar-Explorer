using System.Collections.Generic;
using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.ViewModels.Component;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ResolveTempAvatarViewModel : ViewModelBase
{

    [Reactive] public string SearchText { get; set; } = string.Empty;
    [Reactive] public IEnumerable<ItemViewModel> Avatars { get; set; } = [];
    private TaskCompletionSource<string?> _tcs = new();

    public IReactiveCommand SelectItemCommand { get; }
    public IReactiveCommand CancelCommand { get; }

    private string? SelectedAvatar { get; set; } = null;

    public ResolveTempAvatarViewModel()
    {
        CancelCommand = ReactiveCommand.Create(() => _tcs.SetResult(null));
        SelectItemCommand = ReactiveCommand.CreateFromTask<ItemViewModel>(SelectItem);
    }

    private async Task SelectItem(ItemViewModel item)
    {
        if (SelectedAvatar == null)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[LocalizationKey.Error.Default],
                Localizer.Instance[LocalizationKey.Error.TempAvatarNotFound],
                Avalonia.Controls.Notifications.NotificationType.Error
            );
            return;
        }

        var tempAvatar = AvatarExplorerApp.Instance.TempAvatars.Get(SelectedAvatar);
        if (tempAvatar == null)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[LocalizationKey.Error.Default],
                Localizer.Instance[LocalizationKey.Error.TempAvatarNotFound],
                Avalonia.Controls.Notifications.NotificationType.Error
            );
            return;
        }

        var resolveConfirmationResult = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default],
            Localizer.Instance.Get(LocalizationKey.Dialog.Confirmation.ResolveTempAvatar, [tempAvatar.AvatarName, item.Title])
        );
        if (resolveConfirmationResult is false) return;

        AvatarExplorerApp.Instance.ItemGroupService.ResolveTempAvatar(SelectedAvatar, item.Tag);
    }
}
