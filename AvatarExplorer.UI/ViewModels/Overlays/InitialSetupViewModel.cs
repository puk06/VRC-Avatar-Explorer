using System.Reactive.Linq;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public partial class InitialSetupViewModel : ViewModelBase, IInitializable, IPostInitializable
{
    [Reactive] public partial bool IsVisible { get; set; }

    [Reactive] public partial IEnumerable<string> Languages { get; set; } = [];
    [Reactive] public partial int SelectedLanguage { get; set; }
    [Reactive] public partial string ItemsFolder { get; set; } = string.Empty;

    public IReactiveCommand CloseCommand { get; }

    public InitialSetupViewModel()
    {
        CloseCommand = ReactiveCommand.CreateFromTask(Close);

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
                InstanceRepository.RuntimeSettingsRepository.Update(InstanceRepository.RuntimeSettings with { DataRootDirectory = path });
            });
    }

    public async Task OnInitialized()
    {
        if (InstanceRepository.UserPreferences.InitialSetupCompleted) return;

        if (InstanceRepository.Items.GetAll().Any())
        {
            MarkInitialSetupCompleted();
            return;
        }

        Open();
    }

    public void Open()
    {
        Languages = Localizer.Instance.GetLanguageList();
        SelectedLanguage = -1;
        SelectedLanguage = Localizer.Instance.CurrentLanguageIndex;
        ItemsFolder = InstanceRepository.RuntimeSettings.DataRootDirectory;

        IsVisible = true;
    }

    private async Task Close()
    {
        IsVisible = false;

        // 他のツールからデータをインポートするかどうか
        var importFromOtherTools = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.ImportFromOtherTools]
        );
        if (importFromOtherTools) InstanceRepository.MainWindow.ImportDataVM.Open();

        MarkInitialSetupCompleted();
    }

    private static void MarkInitialSetupCompleted() =>
        InstanceRepository.UserPreferencesRepository.Update(InstanceRepository.UserPreferences with { InitialSetupCompleted = true });
}
