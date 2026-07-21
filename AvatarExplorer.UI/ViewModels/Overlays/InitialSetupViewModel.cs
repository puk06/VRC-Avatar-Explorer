using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class InitialSetupViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    public IEnumerable<string> Languages { get; }
    [Reactive] public int SelectedLanguage { get; set; }

    [Reactive] public string ItemsFolder { get; set; }

    public IReactiveCommand CloseCommand { get; }

    private static RuntimeSettingsRepository Settings => AvatarExplorerApp.Instance.RuntimeSettings;

    public InitialSetupViewModel()
    {
        Languages = Localizer.Instance.GetLanguageList();
        SelectedLanguage = Localizer.Instance.CurrentLanguageIndex;
        ItemsFolder = Settings.Settings.DataRootDirectory;
        
        CloseCommand = ReactiveCommand.Create(OnClose);

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
        
        if (!AvatarExplorerApp.Instance.Items.GetAll().Any())
        {
            IsVisible = true;
        }
    }

    private void OnClose()
    {
        IsVisible = false;
    }
}
