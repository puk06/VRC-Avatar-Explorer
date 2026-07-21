using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Settings;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class SettingsViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
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

    public IReactiveCommand OpenBackgroundImageCommand { get; }
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
        Languages = Localizer.Instance.GetLanguageList();
        SelectedLanguage = Localizer.Instance.CurrentLanguageIndex;

        OpenBackgroundImageCommand = ReactiveCommand.CreateFromTask(OpenBackgroundImage);
        OpenCommonAvatarManagerCommand = ReactiveCommand.Create(OpenCommonAvatarManager);
        OpenItemsFolderCommand = ReactiveCommand.Create(OpenItemsFolder);
        OpenAutoBackupFolderCommand = ReactiveCommand.Create(OpenAutoBackupFolder);
        ImportDataCommand = ReactiveCommand.Create(ImportData);
        ExportDataToCsvCommand = ReactiveCommand.Create(ExportDataToCsv);
        ImportThumbnailCommand = ReactiveCommand.Create(ImportThumbnail);
        FetchAllThumbnailsCommand = ReactiveCommand.Create(FetchAllThumbnails);
        RestoreFromBackupCommand = ReactiveCommand.Create(RestoreFromBackup);
        AutoFixDatabaseCommand = ReactiveCommand.Create(AutoFixDatabase);
        ResetItemDatabaseCommand = ReactiveCommand.Create(ResetItemDatabase);
        ResetCommonAvatarDatabaseCommand = ReactiveCommand.Create(ResetCommonAvatarDatabase);
        ResetBulkImportPresetDatabaseCommand = ReactiveCommand.Create(ResetBulkImportPresetDatabase);
        ShowErrorLogCommand = ReactiveCommand.Create(ShowErrorLog);
        RegisterSchemeCommand = ReactiveCommand.Create(RegisterScheme);
        CheckForUpdateNowCommand = ReactiveCommand.Create(CheckForUpdateNow);
        OpenTwitterCommand = ReactiveCommand.Create(OpenTwitter);
        OpenGithubCommand = ReactiveCommand.Create(OpenGithub);
        OpenSourceCodeCommand = ReactiveCommand.Create(OpenSourceCode);
        ViewLicenseCommand = ReactiveCommand.Create(ViewLicense);
        ViewThirdPartyLicensesCommand = ReactiveCommand.Create(ViewThirdPartyLicenses);
        CloseCommand = ReactiveCommand.Create(OnClose);
        ApplyCommand = ReactiveCommand.Create(OnApply);
    }

    public RuntimeSettings CreateRuntimeSettings()
    {
        return new RuntimeSettings
        {
            DataRootDirectory = ItemsFolderPath,
            AutoBackupRootDirectory = AutoBackupFolderPath,
            ItemSortOrder = (ItemSortOrder)SelectedSortOrder,
            RemoveOriginal = RemoveOriginal,
            ShouldLinkToOriginal = LinkToOriginal,
            AutoBackupInterval = AutoBackupInterval,
            TreatEmptySupportedAvatarAsNone = TreatEmptySupportedAvatarAsNone,
            MaxDegreeOfParallelism = MaxDegreeOfParallelism
        };
    }

    public UserPreferences CreateUserPreferences()
    {
        return new UserPreferences
        {
            Language = SelectedLanguage,
            Theme = (Theme)SelectedTheme,
            RemoveBrackets = RemoveBrackets,
            NormalIconSize = (int)NormalIconSize,
            EnableHoverIconSize = EnableHoverIconSize,
            HoverIconSize = (int)HoverIconSize,
            AntiAliasingMode = (BitmapAntiAliasingMode)SelectedAntiAliasing,
            ItemsPerPage = ItemsPerPage,
            ThumbnailCompressionMaxEdge = (int)ThumbnailCompressionMaxSize,
            UseBackgroundImage = UseBackgroundImage,
            BackgroundImage = BackgroundImagePath,
            BackgroundOpacity = (int)BackgroundImageOpacity,
            CheckForUpdate = CheckForUpdate,
            UpdateChannel = (UpdateChannel)SelectedUpdateChannel
        };
    }

    private void OnApply()
    {
    }

    private void OnClose()
    {
        IsVisible = false;
    }

    private void OpenCommonAvatarManager()
    {
    }

    private async Task OpenBackgroundImage()
    {
    }

    private void OpenItemsFolder()
    {
    }

    private void OpenAutoBackupFolder()
    {
    }

    private void ImportData()
    {
    }

    private void ExportDataToCsv()
    {
    }

    private void ImportThumbnail()
    {
    }

    private void FetchAllThumbnails()
    {
    }

    private void RestoreFromBackup()
    {
    }

    private void AutoFixDatabase()
    {
    }

    private void ResetItemDatabase()
    {
    }

    private void ResetCommonAvatarDatabase()
    {
    }

    private void ResetBulkImportPresetDatabase()
    {
    }

    private void ShowErrorLog()
    {
        MainWindowViewModel.Instance.ErrorLogVM.IsVisible = true;
    }

    private void RegisterScheme()
    {
    }

    private void CheckForUpdateNow()
    {
    }

    private void OpenTwitter()
    {
    }

    private void OpenGithub()
    {
    }

    private void OpenSourceCode()
    {
    }

    private void ViewLicense()
    {
    }

    private void ViewThirdPartyLicenses()
    {
    }
}
