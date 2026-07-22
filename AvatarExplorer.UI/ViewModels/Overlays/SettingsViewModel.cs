using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.Updates;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
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
    public IReactiveCommand ExportDataCommand { get; }
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
        OpenItemsFolderCommand = ReactiveCommand.CreateFromTask(OpenItemsFolder);
        OpenAutoBackupFolderCommand = ReactiveCommand.CreateFromTask(OpenAutoBackupFolder);
        ImportDataCommand = ReactiveCommand.Create(ImportData);
        ExportDataCommand = ReactiveCommand.Create(ExportData);
        FetchAllThumbnailsCommand = ReactiveCommand.Create(FetchAllThumbnails);
        RestoreFromBackupCommand = ReactiveCommand.Create(RestoreFromBackup);
        AutoFixDatabaseCommand = ReactiveCommand.Create(AutoFixDatabase);
        ResetItemDatabaseCommand = ReactiveCommand.CreateFromTask(ResetItemDatabase);
        ResetCommonAvatarDatabaseCommand = ReactiveCommand.CreateFromTask(ResetCommonAvatarDatabase);
        ResetBulkImportPresetDatabaseCommand = ReactiveCommand.CreateFromTask(ResetBulkImportPresetDatabase);
        ShowErrorLogCommand = ReactiveCommand.Create(ShowErrorLog);
        RegisterSchemeCommand = ReactiveCommand.Create(RegisterScheme);
        CheckForUpdateNowCommand = ReactiveCommand.Create(CheckForUpdateNow);
        OpenTwitterCommand = ReactiveCommand.CreateFromTask(OpenTwitter);
        OpenGithubCommand = ReactiveCommand.CreateFromTask(OpenGithub);
        OpenSourceCodeCommand = ReactiveCommand.CreateFromTask(OpenSourceCode);
        ViewLicenseCommand = ReactiveCommand.CreateFromTask(ViewLicense);
        ViewThirdPartyLicensesCommand = ReactiveCommand.CreateFromTask(ViewThirdPartyLicenses);
        CloseCommand = ReactiveCommand.Create(OnClose);
        ApplyCommand = ReactiveCommand.Create(OnApply);
    }

    public void Open()
    {
        var runtimeSettings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;
        var preferences = MainWindowViewModel.Instance.UserPreferences.Settings;

        SelectedLanguage = preferences.Language;
        SelectedSortOrder = (int)runtimeSettings.ItemSortOrder;
        SelectedTheme = (int)preferences.Theme;
        RemoveBrackets = preferences.RemoveBrackets;
        NormalIconSize = preferences.NormalIconSize;
        EnableHoverIconSize = preferences.EnableHoverIconSize;
        HoverIconSize = preferences.HoverIconSize;
        SelectedAntiAliasing = (int)preferences.AntiAliasingMode;
        ItemsPerPage = preferences.ItemsPerPage;
        RemoveOriginal = runtimeSettings.RemoveOriginal;
        LinkToOriginal = runtimeSettings.ShouldLinkToOriginal;
        TreatEmptySupportedAvatarAsNone = runtimeSettings.TreatEmptySupportedAvatarAsNone;
        ThumbnailCompressionMaxSize = preferences.ThumbnailCompressionMaxEdge;
        UseBackgroundImage = preferences.UseBackgroundImage;
        BackgroundImagePath = preferences.BackgroundImage;
        BackgroundImageOpacity = preferences.BackgroundOpacity;
        ItemsFolderPath = runtimeSettings.DataRootDirectory;
        AutoBackupFolderPath = runtimeSettings.AutoBackupRootDirectory;
        AutoBackupInterval = runtimeSettings.AutoBackupInterval;
        MaxDegreeOfParallelism = runtimeSettings.MaxDegreeOfParallelism;
        CheckForUpdate = runtimeSettings.CheckForUpdate;
        SelectedUpdateChannel = (int)runtimeSettings.UpdateChannel;

        IsVisible = true;
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
            MaxDegreeOfParallelism = MaxDegreeOfParallelism,
            CheckForUpdate = CheckForUpdate,
            UpdateChannel = (UpdateChannel)SelectedUpdateChannel
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
            BackgroundOpacity = (int)BackgroundImageOpacity
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
        MainWindowViewModel.Instance.EditCommonAvatarsVM.Open();
    }

    private async Task OpenBackgroundImage()
    {
        var files = await StorageService.OpenFileDialog(TopLevelProvider.Current, "Select Background Image");
        if (files == null || files.Length == 0) return;

        BackgroundImagePath = files[0];
    }

    private async Task OpenItemsFolder()
    {
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, "Select Items Folder");
        if (folders == null || folders.Length == 0) return;

        ItemsFolderPath = folders[0];
    }

    private async Task OpenAutoBackupFolder()
    {
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, "Select Auto Backup Folder");
        if (folders == null || folders.Length == 0) return;

        AutoBackupFolderPath = folders[0];
    }

    private void ImportData()
    {
        MainWindowViewModel.Instance.ImportDataVM.Open();
    }

    private void ExportData()
    {
        MainWindowViewModel.Instance.ExportDataVM.Open();
    }

    private void FetchAllThumbnails()
    {
        MainWindowViewModel.Instance.FetchAllThumbnailsVM.IsVisible = true;
    }

    private void RestoreFromBackup()
    {
    }

    private void AutoFixDatabase()
    {
    }

    private async Task ResetItemDatabase()
    {
        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Settings.ResetItemDatabase.Title],
            Localizer.Instance[Loc.Settings.ResetItemDatabase.Description]
        );
        if (!result) return;

        var dbPath = SystemPath.ItemDatabasePath;
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }

    private async Task ResetCommonAvatarDatabase()
    {
        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Settings.ResetCommonAvatarDatabase.Title],
            Localizer.Instance[Loc.Settings.ResetCommonAvatarDatabase.Description]
        );
        if (!result) return;

        var dbPath = SystemPath.CommonAvatarDatabasePath;
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }

    private async Task ResetBulkImportPresetDatabase()
    {
        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Settings.ResetBulkImportPresetDatabase.Title],
            Localizer.Instance[Loc.Settings.ResetBulkImportPresetDatabase.Description]
        );
        if (!result) return;

        var dbPath = SystemPath.BulkImportPresetDatabasePath;
        if (File.Exists(dbPath)) File.Delete(dbPath);
    }

    private void ShowErrorLog()
    {
        MainWindowViewModel.Instance.ShowErrorLog();
    }

    private void RegisterScheme()
    {
        if (!SchemeService.IsRunAsAdmin())
        {
            SchemeService.RestartAsAdmin();
            return;
        }

        SchemeService.RegisterScheme();
    }

    private async void CheckForUpdateNow()
    {
        // 現在選択されているチャンネルでチェックする
        await UpdateChecker.CheckForUpdate((UpdateChannel)SelectedUpdateChannel);
    }

    private async Task OpenTwitter()
    {
        await LauncherService.OpenUri(TopLevelProvider.Current, DeveloperLink.TwitterURL);
    }

    private async Task OpenGithub()
    {
        await LauncherService.OpenUri(TopLevelProvider.Current, DeveloperLink.GithubURL);
    }

    private async Task OpenSourceCode()
    {
        await LauncherService.OpenUri(TopLevelProvider.Current, SoftwareLink.RepositoryURL);
    }

    private async Task ViewLicense()
    {
        var licensePath = Path.Combine(System.AppContext.BaseDirectory, SystemFileName.Lisence);
        if (File.Exists(licensePath)) await LauncherService.OpenUri(TopLevelProvider.Current, licensePath);
    }

    private async Task ViewThirdPartyLicenses()
    {
        var licensePath = Path.Combine(System.AppContext.BaseDirectory, SystemFileName.ThirdPartyLisences);
        if (File.Exists(licensePath)) await LauncherService.OpenUri(TopLevelProvider.Current, licensePath);
    }
}
