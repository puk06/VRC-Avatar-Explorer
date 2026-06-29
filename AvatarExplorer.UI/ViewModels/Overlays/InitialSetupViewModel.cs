using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class InitialSetupViewModel : ViewModelBase
{
    public IEnumerable<string> Languages { get; }
    [Reactive] public int SelectedLanguage { get; set; }

    [Reactive] public string ItemsFolder { get; set; }

    public IReactiveCommand CloseCommand { get; }

    public InitialSetupViewModel()
    {
        Languages = Localizer.Instance.GetLanguageList();
        SelectedLanguage = Localizer.Instance.CurrentLanguageIndex;
        ItemsFolder = AvatarExplorerApp.Instance.GetRuntimeSettings().DataRootDirectory;
        
        CloseCommand = ReactiveCommand.Create(OnClose);

        this.WhenAnyValue(x => x.SelectedLanguage)
            .Subscribe(Localizer.Instance.SetLanguage);

        this.WhenAnyValue(x => x.ItemsFolder)
            .Subscribe(path => AvatarExplorerApp.Instance.SetRuntimeSettings(AvatarExplorerApp.Instance.GetRuntimeSettings() with { DataRootDirectory = path }));
    }

    private void OnClose()
    {
        // ボタンが押されたときの処理
    }
}
