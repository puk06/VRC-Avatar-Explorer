using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class InitialSetupViewModel : ViewModelBase, IInitializable, IPostInitializable
{
    [Reactive] public bool IsVisible { get; set; }

    [Reactive] public IEnumerable<string> Languages { get; set; } = [];
    [Reactive] public int SelectedLanguage { get; set; }
    [Reactive] public string ItemsFolder { get; set; } = string.Empty;

    public IReactiveCommand CloseCommand { get; }

    private static RuntimeSettingsRepository Settings => AvatarExplorerApp.Instance.RuntimeSettings;
    private static UserPreferencesRepository UserPreferences => UserPreferencesService.Instance.Repository;

    public InitialSetupViewModel()
    {
        CloseCommand = ReactiveCommand.Create(Close);

        IInitializableRegistry.Register(0, (IInitializable)this);
        IInitializableRegistry.Register(int.MaxValue, (IPostInitializable)this);
    }

    public async Task Initialize()
    {
        this.WhenAnyValue(x => x.SelectedLanguage)
            .Subscribe(i =>
            {
                if (!IsVisible) return;
                Localizer.Instance.SetLanguage(i);
            });

        this.WhenAnyValue(x => x.ItemsFolder)
            .Subscribe(path =>
            {
                if (!IsVisible) return;
                Settings.Update(Settings.Settings with { DataRootDirectory = path });
            });
    }

    public async Task OnInitialized()
    {
        if (UserPreferences.Settings.InitialSetupCompleted) return;

        if (AvatarExplorerApp.Instance.Items.GetAll().Any())
        {
            MarkInitialSetupCompleted();
            return;
        }

        Open();
        ShowSchemeRegistrationDialog();
    }

    public void Open()
    {
        Languages = Localizer.Instance.GetLanguageList();
        SelectedLanguage = -1;
        SelectedLanguage = Localizer.Instance.CurrentLanguageIndex;
        ItemsFolder = Settings.Settings.DataRootDirectory;

        IsVisible = true;
    }

    private void Close()
    {
        MarkInitialSetupCompleted();
        IsVisible = false;
    }

    private void MarkInitialSetupCompleted() =>
        UserPreferences.Update(UserPreferences.Settings with { InitialSetupCompleted = true });

    private static async void ShowSchemeRegistrationDialog()
    {
        if (SchemeService.IsOwnSchemeRegistered(SchemeService.ProtocolVRCAE)) return;

        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Scheme.Register]
        );

        if (result)
        {
            if (ProcessUtils.IsWindows() && !SchemeService.IsRunAsAdmin())
            {
                var restartAsAdmin = await MainWindowViewModel.Instance.ShowYesNoDialog(
                    Localizer.Instance[Loc.Dialog.Confirmation.Default],
                    Localizer.Instance[Loc.Scheme.RestartAsAdmin]
                );
                if (restartAsAdmin) SchemeService.RestartAsAdmin();

                return;
            }

            SchemeService.RegisterScheme(SchemeService.ProtocolVRCAE);
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Success.Default],
                Localizer.Instance[Loc.Scheme.RegisterSuccess],
                NotificationType.Success
            );
        }
        else
        {
            MainWindowViewModel.ShowNotification(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Scheme.RegisterSkipped],
                NotificationType.Information
            );
        }
    }
}
