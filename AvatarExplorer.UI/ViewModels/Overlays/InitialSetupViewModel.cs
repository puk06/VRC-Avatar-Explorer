using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.System.Repositories;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
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
        if (!AvatarExplorerApp.Instance.Items.GetAll().Any())
            Open();
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
        IsVisible = false;
    }
}
