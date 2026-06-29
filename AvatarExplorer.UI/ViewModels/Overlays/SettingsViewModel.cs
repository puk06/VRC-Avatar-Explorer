using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class SettingsViewModel : ViewModelBase
{
    public IEnumerable<string> Languages { get; }
    [Reactive] public int SelectedLanguage { get; set; }

    [Reactive] public int SelectedSortOrder { get; set; }
    [Reactive] public int SelectedTheme { get; set; }
    [Reactive] public bool RemoveBrackets { get; set; }
    [Reactive] public double NormalIconSize { get; set; }
    [Reactive] public bool EnableHoverIconSize { get; set; }
    [Reactive] public double HoverIconSize { get; set; }
    [Reactive] public int SelectedAntiAliasing { get; set; }
    [Reactive] public int ItemsPerPage { get; set; }
    [Reactive] public bool RemoveOriginal { get; set; }
    [Reactive] public bool LinkToOriginal { get; set; }
    [Reactive] public bool TreatEmptySupportedAvatarAsNone { get; set; }
    [Reactive] public double ThumbnailCompressionMaxSize { get; set; }
    [Reactive] public bool UseBackgroundImage { get; set; }
    [Reactive] public string BackgroundImagePath { get; set; }
    [Reactive] public double BackgroundImageOpacity { get; set; }
    [Reactive] public string ItemsFolderPath { get; set; }
    [Reactive] public string AutoBackupFolderPath { get; set; }
    [Reactive] public int AutoBackupInterval { get; set; }
    [Reactive] public int MaxDegreeOfParallelism { get; set; }
    [Reactive] public bool CheckForUpdate { get; set; }
    [Reactive] public int SelectedUpdateChannel { get; set; }
    [Reactive] public Image GithubUserImage { get; set; }

    public IReactiveCommand OpenBackgroundImageCommand { get; } // TODO: 作る
    public IReactiveCommand OpenCommonAvatarManagerCommand { get; }
    public IReactiveCommand OpenItemsFolderCommand { get; }
    public IReactiveCommand OpenAutoBackupFolderCommand { get; }
    public IReactiveCommand ImportDataCommand { get; }
    public IReactiveCommand ExportDataToCsvCommand { get; }
    public IReactiveCommand ImportThumbnailCommand { get; }
    public IReactiveCommand FetchAllThumbnailsCommand { get; }
    public IReactiveCommand RestoreFromBackupCommand { get; }
    public IReactiveCommand AutoFixDatabaseCommand { get; }
    public IReactiveCommand ResetItemDatabaseCommand { get; }
    public IReactiveCommand ResetCommonAvatarDatabaseCommand { get; }
    public IReactiveCommand ResetBulkImportPresetDatabaseCommand { get; }
    public IReactiveCommand ShowErrorLogCommand { get; }
    public IReactiveCommand RegisterSchemeCommand { get; }
    public IReactiveCommand CheckForUpdateNowCommand { get; }
    public IReactiveCommand OpenTwitterCommand { get; }
    public IReactiveCommand OpenGithubCommand { get; }
    public IReactiveCommand OpenSourceCodeCommand { get; }
    public IReactiveCommand ViewLicenseCommand { get; }
    public IReactiveCommand ViewThirdPartyLicensesCommand { get; }
    
    public IReactiveCommand CloseCommand { get; }
    public IReactiveCommand ApplyCommand { get; }

    public SettingsViewModel()
    {
        OpenBackgroundImageCommand = ReactiveCommand.CreateFromTask(OpenBackgroundImage);
        OpenCommonAvatarManagerCommand = ReactiveCommand.Create(OpenCommonAvatarManager);
        CloseCommand = ReactiveCommand.Create(OnClose);
    }

    private void OnClose()
    {
        // ボタンが押されたときの処理
    }

    private void OpenCommonAvatarManager()
    {
        // ボタンが押されたときの処理
    }

    private async Task OpenBackgroundImage()
    {
        // ボタンが押されたときの処理
    }
}
