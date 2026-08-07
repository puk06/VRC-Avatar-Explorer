using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.Updates;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Models.Sort;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class SettingsViewModel : ViewModelBase, IInitializable
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public IEnumerable<string> Languages { get; set; } = [];
    [Reactive] public int SelectedLanguage { get; set; }

    [Reactive] public int SelectedSortOrder { get; set; }
    [Reactive] public SortDirection SelectedSortDirection { get; set; }
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
    [Reactive] public string BackgroundImagePath { get; set; } = string.Empty;
    [Reactive] public double BackgroundImageOpacity { get; set; }
    [Reactive] public string ItemsFolderPath { get; set; } = string.Empty;
    [Reactive] public string AutoBackupFolderPath { get; set; } = string.Empty;
    [Reactive] public int AutoBackupInterval { get; set; }
    [Reactive] public int MaxDegreeOfParallelism { get; set; }
    [Reactive] public bool CheckForUpdate { get; set; }
    [Reactive] public int SelectedUpdateChannel { get; set; }
    [Reactive] public Bitmap? GithubUserImage { get; set; } = null;
    [Reactive] public string VRCAESchemeStatusText { get; set; } = string.Empty;
    [Reactive] public string BLMSchemeStatusText { get; set; } = string.Empty;

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
    public IReactiveCommand RegisterVRCAESchemeCommand { get; }
    public IReactiveCommand UnregisterVRCAESchemeCommand { get; }
    public IReactiveCommand RegisterBLMSchemeCommand { get; }
    public IReactiveCommand UnregisterBLMSchemeCommand { get; }
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
        OpenItemsFolderCommand = ReactiveCommand.CreateFromTask(OpenItemsFolder);
        OpenAutoBackupFolderCommand = ReactiveCommand.CreateFromTask(OpenAutoBackupFolder);
        ImportDataCommand = ReactiveCommand.Create(ImportData);
        ExportDataCommand = ReactiveCommand.Create(ExportData);
        FetchAllThumbnailsCommand = ReactiveCommand.Create(FetchAllThumbnails);
        RestoreFromBackupCommand = ReactiveCommand.CreateFromTask(RestoreFromBackup);
        AutoFixDatabaseCommand = ReactiveCommand.CreateFromTask(AutoFixDatabase);
        ResetItemDatabaseCommand = ReactiveCommand.CreateFromTask(ResetItemDatabase);
        ResetCommonAvatarDatabaseCommand = ReactiveCommand.CreateFromTask(ResetCommonAvatarDatabase);
        ResetBulkImportPresetDatabaseCommand = ReactiveCommand.CreateFromTask(ResetBulkImportPresetDatabase);
        ShowErrorLogCommand = ReactiveCommand.Create(ShowErrorLog);
        RegisterVRCAESchemeCommand = ReactiveCommand.CreateFromTask(() => RegisterScheme(SchemeService.ProtocolVRCAE));
        UnregisterVRCAESchemeCommand = ReactiveCommand.CreateFromTask(() => UnregisterScheme(SchemeService.ProtocolVRCAE));
        RegisterBLMSchemeCommand = ReactiveCommand.CreateFromTask(() => RegisterScheme(SchemeService.ProtocolBLM));
        UnregisterBLMSchemeCommand = ReactiveCommand.CreateFromTask(() => UnregisterScheme(SchemeService.ProtocolBLM));
        CheckForUpdateNowCommand = ReactiveCommand.Create(CheckForUpdateNow);
        OpenTwitterCommand = ReactiveCommand.CreateFromTask(OpenTwitter);
        OpenGithubCommand = ReactiveCommand.CreateFromTask(OpenGithub);
        OpenSourceCodeCommand = ReactiveCommand.CreateFromTask(OpenSourceCode);
        ViewLicenseCommand = ReactiveCommand.CreateFromTask(ViewLicense);
        ViewThirdPartyLicensesCommand = ReactiveCommand.CreateFromTask(ViewThirdPartyLicenses);
        CloseCommand = ReactiveCommand.Create(OnClose);
        ApplyCommand = ReactiveCommand.Create(OnApply);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        LoadProfileImage();
    }

    public async void LoadProfileImage()
    {
        GithubUserImage = await GithubService.GetProfileIconAsync();
    }

    public void Open()
    {
        var runtimeSettings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;
        var preferences = UserPreferencesService.Instance.Repository.Settings;

        Languages = Localizer.Instance.GetLanguageList();

        SelectedLanguage = -1;
        SelectedLanguage = preferences.Language;

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
        SelectedSortOrder = (int)preferences.SortOrder;
        SelectedSortDirection = preferences.SortDirection;

        UpdateSchemeStatus();

        IsVisible = true;
    }

    public RuntimeSettings CreateRuntimeSettings()
    {
        return new RuntimeSettings
        {
            DataRootDirectory = ItemsFolderPath,
            AutoBackupRootDirectory = AutoBackupFolderPath,
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
            BackgroundOpacity = (int)BackgroundImageOpacity,
            SortOrder = (ItemSortOrder)SelectedSortOrder,
            SortDirection = SelectedSortDirection
        };
    }

    private void OnApply()
    {
        AvatarExplorerApp.Instance.RuntimeSettings.Update(CreateRuntimeSettings());
        UserPreferencesService.Instance.Repository.Update(CreateUserPreferences());
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
        var files = await StorageService.OpenFileDialog(TopLevelProvider.Current, Localizer.Instance[Loc.Dialog.SelectFilePath]);
        if (files == null || files.Length == 0) return;

        BackgroundImagePath = files[0];
    }

    private async Task OpenItemsFolder()
    {
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, Localizer.Instance[Loc.Dialog.SelectFolderPath]);
        if (folders == null || folders.Length == 0) return;

        ItemsFolderPath = folders[0];
    }

    private async Task OpenAutoBackupFolder()
    {
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, Localizer.Instance[Loc.Dialog.SelectFolderPath]);
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

    private async Task RestoreFromBackup()
    {
        var folders = await StorageService.OpenFolderDialog(TopLevelProvider.Current, "Select Items Folder");
        if (folders == null || folders.Length == 0) return;

        var selectedBackupPath = folders[0];
        await AvatarExplorerApp.Instance.BackupManager.RestoreBackup(selectedBackupPath);
    }

    private async Task AutoFixDatabase()
    {
        var items = AvatarExplorerApp.Instance.Items.GetAll();
        var backupFolder = AvatarExplorerApp.Instance.RuntimeSettings.Settings.AutoBackupRootDirectory;

        var avatarExists = items.Any(i => i.Category.Type == ItemType.Avatar);
        var unknownCategoryExists = items.Any(i => (int)i.Category.Type >= 11);
        if (!avatarExists)
        {
            var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Dialog.Confirmation.NoAvatarsAndValidateType]
            );

            if (result)
            {
                await AvatarExplorerApp.Instance.BackupManager.ExecuteBackup(backupFolder);
                AvatarExplorerApp.Instance.Items.ValidateAndAutoFixItemType(true);
            }
        }
        else if (unknownCategoryExists)
        {
            await AvatarExplorerApp.Instance.BackupManager.ExecuteBackup(backupFolder);
            AvatarExplorerApp.Instance.Items.ValidateAndAutoFixItemType(false);
        }
    }

    private async Task ResetItemDatabase()
    {
        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.ResetItemDatabase]
        );
        if (!result) return;

        AvatarExplorerApp.Instance.Items.Clear();
        AvatarExplorerApp.Instance.TempAvatars.Clear();
    }

    private async Task ResetCommonAvatarDatabase()
    {
        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.ResetCommonAvatarDatabase]
        );
        if (!result) return;

        AvatarExplorerApp.Instance.CommonAvatars.Clear();
    }

    private async Task ResetBulkImportPresetDatabase()
    {
        var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Dialog.Confirmation.ResetBulkImportPresetDatabase]
        );
        if (!result) return;

        AvatarExplorerApp.Instance.BulkImportPresets.Clear();
    }

    private void ShowErrorLog()
    {
        MainWindowViewModel.Instance.ShowErrorLog();
    }

    private void UpdateSchemeStatus()
    {
        if (!ProcessUtils.IsWindows())
        {
            VRCAESchemeStatusText = string.Empty;
            BLMSchemeStatusText = string.Empty;
            return;
        }

        VRCAESchemeStatusText = GetSchemeStatusText(SchemeService.ProtocolVRCAE);
        BLMSchemeStatusText = GetSchemeStatusText(SchemeService.ProtocolBLM);
    }

    private static string GetSchemeStatusText(string protocol)
    {
        if (SchemeService.IsOwnSchemeRegistered(protocol))
            return Localizer.Instance[Loc.Settings.RegisterScheme.Status.Own];

        if (SchemeService.IsAnySchemeRegistered(protocol))
        {
            var command = SchemeService.GetRegisteredCommand(protocol) ?? "";
            return Localizer.Instance.Get(Loc.Settings.RegisterScheme.Status.Other, command);
        }

        return Localizer.Instance[Loc.Settings.RegisterScheme.Status.None];
    }

    private async Task RegisterScheme(string protocol)
    {
        if (!SchemeService.IsRunAsAdmin())
        {
            var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Scheme.RestartAsAdmin]
            );
            if (result) SchemeService.RestartAsAdmin();

            return;
        }

        if (SchemeService.IsAnySchemeRegistered(protocol) && !SchemeService.IsOwnSchemeRegistered(protocol))
        {
            var command = SchemeService.GetRegisteredCommand(protocol) ?? "";
            var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Settings.RegisterScheme.OverwriteConfirm, command)
            );
            if (!result) return;
        }

        SchemeService.RegisterScheme(protocol);
        UpdateSchemeStatus();

        MainWindowViewModel.Instance.ShowNotification(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Scheme.RegisterSuccess],
            Avalonia.Controls.Notifications.NotificationType.Success
        );
    }

    private async Task UnregisterScheme(string protocol)
    {
        if (!SchemeService.IsRunAsAdmin())
        {
            var result = await MainWindowViewModel.Instance.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Scheme.RestartAsAdmin]
            );
            if (result) SchemeService.RestartAsAdmin();

            return;
        }

        if (!SchemeService.IsAnySchemeRegistered(protocol)) return;

        var confirm = await MainWindowViewModel.Instance.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Settings.RegisterScheme.UnregisterConfirm]
        );
        if (!confirm) return;

        SchemeService.UnregisterScheme(protocol);
        UpdateSchemeStatus();

        MainWindowViewModel.Instance.ShowNotification(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Settings.RegisterScheme.UnregisterSuccess],
            Avalonia.Controls.Notifications.NotificationType.Success
        );
    }

    private async void CheckForUpdateNow()
    {
        // 現在選択されているチャンネルでチェックする
        var result = await UpdateChecker.CheckForUpdate((UpdateChannel)SelectedUpdateChannel);
        if (!result)
        {
            MainWindowViewModel.Instance.ShowNotification(
                Localizer.Instance[Loc.UpdateDialog.NoUpdateAvailableTitle],
                Localizer.Instance.Get(Loc.UpdateDialog.NoUpdateAvailable, AvatarExplorerApp.CurrentVersion),
                Avalonia.Controls.Notifications.NotificationType.Information
            );
        }
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
