using Avalonia.Controls.Notifications;
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
using AvatarExplorer.UI.Models.Sort;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public partial class SettingsViewModel : ViewModelBase, IInitializable
{
    [Reactive] public partial bool IsVisible { get; set; }
    [Reactive] public partial IEnumerable<string> Languages { get; set; } = [];
    [Reactive] public partial int SelectedLanguage { get; set; }

    [Reactive] public partial int SelectedSortOrder { get; set; }
    [Reactive] public partial SortDirection SelectedSortDirection { get; set; }
    [Reactive] public partial int SelectedImplementedSort { get; set; }
    [Reactive] public partial bool EnableSearchInFolder { get; set; }
    [Reactive] public partial int SelectedTheme { get; set; }
    [Reactive] public partial bool RemoveBrackets { get; set; }
    [Reactive] public partial double NormalIconSize { get; set; }
    [Reactive] public partial bool EnableHoverIconSize { get; set; }
    [Reactive] public partial double HoverIconSize { get; set; }
    [Reactive] public partial int SelectedAntiAliasing { get; set; }
    [Reactive] public partial string ItemsPerPage { get; set; } = string.Empty;
    [Reactive] public partial int SelectedViewMode { get; set; }
    [Reactive] public partial int SelectedGridItemSize { get; set; }
    [Reactive] public partial bool AutoChangeUnitypackagePath { get; set; }
    [Reactive] public partial bool RemoveOriginal { get; set; }
    [Reactive] public partial bool LinkToOriginal { get; set; }
    [Reactive] public partial bool TreatEmptySupportedAvatarAsNone { get; set; }
    [Reactive] public partial bool HideAvatarCategoryWhenAvatarSelected { get; set; }
    [Reactive] public partial double ThumbnailCompressionMaxSize { get; set; }
    [Reactive] public partial bool UseBackgroundImage { get; set; }
    [Reactive] public partial string BackgroundImagePath { get; set; } = string.Empty;
    [Reactive] public partial double BackgroundImageOpacity { get; set; }
    [Reactive] public partial string ItemsFolderPath { get; set; } = string.Empty;
    [Reactive] public partial string AutoBackupFolderPath { get; set; } = string.Empty;
    [Reactive] public partial string AutoBackupInterval { get; set; } = string.Empty;
    [Reactive] public partial string MaxDegreeOfParallelism { get; set; } = string.Empty;
    [Reactive] public partial bool CheckForUpdate { get; set; }
    [Reactive] public partial int SelectedUpdateChannel { get; set; }
    [Reactive] public partial Bitmap? GithubUserImage { get; set; } = null;
    [Reactive] public partial string VRCAESchemeStatusText { get; set; } = string.Empty;
    [Reactive] public partial string BLMSchemeStatusText { get; set; } = string.Empty;

    public IReactiveCommand OpenTagEditorCommand { get; }
    public IReactiveCommand OpenBackgroundImageCommand { get; }
    public IReactiveCommand OpenCommonAvatarManagerCommand { get; }
    public IReactiveCommand OpenItemsFolderCommand { get; }
    public IReactiveCommand OpenAutoBackupFolderCommand { get; }
    public IReactiveCommand ImportDataCommand { get; }
    public IReactiveCommand ExportDataCommand { get; }
    public IReactiveCommand FetchAllThumbnailsCommand { get; }
    public IReactiveCommand FetchAllVariationHashesCommand { get; }
    public IReactiveCommand RestoreFromBackupCommand { get; }
    public IReactiveCommand AutoFixDatabaseCommand { get; }
    public IReactiveCommand ResetDatabaseCommand { get; }
    public IReactiveCommand ShowErrorLogCommand { get; }
    public IReactiveCommand RegisterVRCAESchemeCommand { get; }
    public IReactiveCommand UnregisterVRCAESchemeCommand { get; }
    public IReactiveCommand RegisterBLMSchemeCommand { get; }
    public IReactiveCommand UnregisterBLMSchemeCommand { get; }
    public IReactiveCommand CheckForUpdateNowCommand { get; }
    public IReactiveCommand OpenTwitterCommand { get; }
    public IReactiveCommand OpenGithubCommand { get; }
    public IReactiveCommand OpenSourceCodeCommand { get; }
    public IReactiveCommand OpenIssuesCommand { get; }
    public IReactiveCommand ViewLicenseCommand { get; }
    public IReactiveCommand ViewThirdPartyLicensesCommand { get; }

    public IReactiveCommand CloseCommand { get; }
    public IReactiveCommand ApplyCommand { get; }

    public SettingsViewModel()
    {
        OpenTagEditorCommand = ReactiveCommand.Create(OpenTagEditor);
        OpenBackgroundImageCommand = ReactiveCommand.CreateFromTask(OpenBackgroundImage);
        OpenCommonAvatarManagerCommand = ReactiveCommand.Create(OpenCommonAvatarManager);
        OpenItemsFolderCommand = ReactiveCommand.CreateFromTask(OpenItemsFolder);
        OpenAutoBackupFolderCommand = ReactiveCommand.CreateFromTask(OpenAutoBackupFolder);
        ImportDataCommand = ReactiveCommand.Create(ImportData);
        ExportDataCommand = ReactiveCommand.Create(ExportData);
        FetchAllVariationHashesCommand = ReactiveCommand.Create(FetchAllVariationHashes);
        FetchAllThumbnailsCommand = ReactiveCommand.Create(FetchAllThumbnails);
        RestoreFromBackupCommand = ReactiveCommand.CreateFromTask(RestoreFromBackup);
        AutoFixDatabaseCommand = ReactiveCommand.CreateFromTask(AutoFixDatabase);
        ResetDatabaseCommand = ReactiveCommand.Create(ResetDatabase);
        ShowErrorLogCommand = ReactiveCommand.Create(ShowErrorLog);
        RegisterVRCAESchemeCommand = ReactiveCommand.CreateFromTask(() => RegisterScheme(SchemeService.ProtocolVRCAE));
        UnregisterVRCAESchemeCommand = ReactiveCommand.CreateFromTask(() => UnregisterScheme(SchemeService.ProtocolVRCAE));
        RegisterBLMSchemeCommand = ReactiveCommand.CreateFromTask(() => RegisterScheme(SchemeService.ProtocolBLM));
        UnregisterBLMSchemeCommand = ReactiveCommand.CreateFromTask(() => UnregisterScheme(SchemeService.ProtocolBLM));
        CheckForUpdateNowCommand = ReactiveCommand.Create(CheckForUpdateNow);
        OpenTwitterCommand = ReactiveCommand.CreateFromTask(OpenTwitter);
        OpenGithubCommand = ReactiveCommand.CreateFromTask(OpenGithub);
        OpenSourceCodeCommand = ReactiveCommand.CreateFromTask(OpenSourceCode);
        OpenIssuesCommand = ReactiveCommand.CreateFromTask(OpenIssues);
        ViewLicenseCommand = ReactiveCommand.CreateFromTask(ViewLicense);
        ViewThirdPartyLicensesCommand = ReactiveCommand.CreateFromTask(ViewThirdPartyLicenses);
        CloseCommand = ReactiveCommand.Create(Close);
        ApplyCommand = ReactiveCommand.Create(Apply);

        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        Localizer.Instance.LanguageChanged += UpdateSchemeStatus;
        _ = LoadProfileImage();
    }

    public async Task LoadProfileImage()
    {
        GithubUserImage = await GithubService.GetProfileIconAsync();
    }

    public void Open()
    {
        var runtimeSettings = InstanceRepository.RuntimeSettings;
        var preferences = InstanceRepository.UserPreferences;

        Languages = Localizer.Instance.GetLanguageList();

        SelectedLanguage = -1;
        SelectedLanguage = preferences.Language;

        SelectedTheme = (int)preferences.Theme;
        RemoveBrackets = preferences.RemoveBrackets;
        NormalIconSize = preferences.NormalIconSize;
        EnableHoverIconSize = preferences.EnableHoverIconSize;
        HoverIconSize = preferences.HoverIconSize;
        SelectedAntiAliasing = (int)preferences.AntiAliasingMode;
        ItemsPerPage = preferences.ItemsPerPage.ToString();
        AutoChangeUnitypackagePath = runtimeSettings.AutoChangeUnitypackagePath;
        RemoveOriginal = runtimeSettings.RemoveOriginal;
        LinkToOriginal = runtimeSettings.ShouldLinkToOriginal;
        TreatEmptySupportedAvatarAsNone = runtimeSettings.TreatEmptySupportedAvatarAsNone;
        HideAvatarCategoryWhenAvatarSelected = runtimeSettings.HideAvatarCategoryWhenAvatarSelected;
        ThumbnailCompressionMaxSize = preferences.ThumbnailCompressionMaxEdge;
        UseBackgroundImage = preferences.UseBackgroundImage;
        BackgroundImagePath = preferences.BackgroundImage;
        BackgroundImageOpacity = preferences.BackgroundOpacity;
        ItemsFolderPath = runtimeSettings.DataRootDirectory;
        AutoBackupFolderPath = runtimeSettings.AutoBackupRootDirectory;
        AutoBackupInterval = runtimeSettings.AutoBackupInterval.ToString();
        MaxDegreeOfParallelism = runtimeSettings.MaxDegreeOfParallelism.ToString();
        CheckForUpdate = runtimeSettings.CheckForUpdate;
        SelectedUpdateChannel = (int)runtimeSettings.UpdateChannel;
        SelectedSortOrder = (int)preferences.SortOrder;
        SelectedSortDirection = preferences.SortDirection;
        SelectedImplementedSort = (int)preferences.ImplementedSort;
        EnableSearchInFolder = preferences.EnableSearchInFolder;
        SelectedViewMode = (int)preferences.MainViewMode;
        SelectedGridItemSize = (int)preferences.GridItemSize;

        UpdateSchemeStatus();

        IsVisible = true;
    }

    private void OpenTagEditor()
    {
        InstanceRepository.MainWindow.TagEditorVM.Open();
    }

    private void OpenCommonAvatarManager()
    {
        InstanceRepository.MainWindow.EditCommonAvatarsVM.Open();
    }

    private async Task OpenBackgroundImage()
    {
        var files = await StorageService.OpenFileDialog(Localizer.Instance[Loc.Dialog.SelectFilePath]);
        if (files == null || files.Length == 0) return;

        BackgroundImagePath = files[0];
    }

    private async Task OpenItemsFolder()
    {
        var folders = await StorageService.OpenFolderDialog(Localizer.Instance[Loc.Dialog.SelectFolderPath]);
        if (folders == null || folders.Length == 0) return;

        ItemsFolderPath = folders[0];
    }

    private async Task OpenAutoBackupFolder()
    {
        var folders = await StorageService.OpenFolderDialog(Localizer.Instance[Loc.Dialog.SelectFolderPath]);
        if (folders == null || folders.Length == 0) return;

        AutoBackupFolderPath = folders[0];
    }

    private void ImportData()
    {
        InstanceRepository.MainWindow.ImportDataVM.Open();
    }

    private void ExportData()
    {
        InstanceRepository.MainWindow.ExportDataVM.Open();
    }

    private void FetchAllThumbnails()
    {
        InstanceRepository.MainWindow.FetchAllThumbnailsVM.Open();
    }

    private void FetchAllVariationHashes()
    {
        InstanceRepository.MainWindow.FetchAllVariationHashesVM.Open();
    }

    private async Task RestoreFromBackup()
    {
        var folders = await StorageService.OpenFolderDialog(
            Localizer.Instance[Loc.Dialog.SelectFolderPath],
            initialPath: InstanceRepository.RuntimeSettings.AutoBackupRootDirectory
        );
        if (folders == null || folders.Length == 0) return;

        var selectedBackupPath = folders[0];
        await InstanceRepository.BackupManager.RestoreBackup(selectedBackupPath);
    }

    private async Task AutoFixDatabase()
    {
        var items = InstanceRepository.Items.GetAll();

        var avatarExists = items.Any(i => i.Category.Type == ItemType.Avatar);
        var unknownCategoryExists = items.Any(i => (int)i.Category.Type >= 11);
        if (!avatarExists)
        {
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Dialog.Confirmation.NoAvatarsAndValidateType]
            );

            if (result)
            {
                await InstanceRepository.BackupManager.ExecuteBackup();
                InstanceRepository.Items.ValidateAndAutoFixItemType(true);
            }
        }
        else if (unknownCategoryExists)
        {
            await InstanceRepository.BackupManager.ExecuteBackup();
            InstanceRepository.Items.ValidateAndAutoFixItemType(false);
        }
    }

    private void ResetDatabase()
    {
        InstanceRepository.MainWindow.ResetDatabaseVM.Open();
    }

    private void ShowErrorLog()
    {
        InstanceRepository.MainWindow.ErrorLogVM.Open();
    }

    private void UpdateSchemeStatus()
    {
        if (!ProcessUtils.IsWindows() && !ProcessUtils.IsLinux())
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
            var command = SchemeService.GetRegisteredCommand(protocol) ?? string.Empty;
            var applicationName = command.Split(" ").FirstOrDefault() ?? string.Empty;

            // ユーザー名を*****にする
            var userName = Environment.UserName;
            var maskedUserName = new string('*', userName.Length);
            applicationName = applicationName.Replace(userName, maskedUserName);

            return Localizer.Instance.Get(Loc.Settings.RegisterScheme.Status.Other, applicationName);
        }

        return Localizer.Instance[Loc.Settings.RegisterScheme.Status.None];
    }

    private async Task RegisterScheme(string protocol)
    {
        if (!ProcessUtils.IsWindows() && !ProcessUtils.IsLinux())
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.UnsupportedPlatform],
                NotificationType.Error
            );
            return;
        }

        if (ProcessUtils.IsWindows() && !SchemeService.IsRunAsAdmin())
        {
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Scheme.RestartAsAdmin]
            );
            if (result) SchemeService.RestartAsAdmin();

            return;
        }

        if (SchemeService.IsAnySchemeRegistered(protocol) && !SchemeService.IsOwnSchemeRegistered(protocol))
        {
            var command = SchemeService.GetRegisteredCommand(protocol) ?? "";
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance.Get(Loc.Settings.RegisterScheme.OverwriteConfirm, command)
            );
            if (!result) return;
        }

        SchemeService.RegisterScheme(protocol);
        UpdateSchemeStatus();

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Scheme.RegisterSuccess],
            NotificationType.Success
        );
    }

    private async Task UnregisterScheme(string protocol)
    {
        if (ProcessUtils.IsWindows() && !SchemeService.IsRunAsAdmin())
        {
            var result = await InstanceRepository.MainWindow.ShowYesNoDialog(
                Localizer.Instance[Loc.Dialog.Confirmation.Default],
                Localizer.Instance[Loc.Scheme.RestartAsAdmin]
            );
            if (result) SchemeService.RestartAsAdmin();

            return;
        }

        if (!SchemeService.IsAnySchemeRegistered(protocol)) return;

        var confirm = await InstanceRepository.MainWindow.ShowYesNoDialog(
            Localizer.Instance[Loc.Dialog.Confirmation.Default],
            Localizer.Instance[Loc.Settings.RegisterScheme.UnregisterConfirm]
        );
        if (!confirm) return;

        SchemeService.UnregisterScheme(protocol);
        UpdateSchemeStatus();

        NotificationManager.Show(
            Localizer.Instance[Loc.Success.Default],
            Localizer.Instance[Loc.Settings.RegisterScheme.UnregisterSuccess],
            NotificationType.Success
        );
    }

    private async void CheckForUpdateNow()
    {
        // 現在選択されているチャンネルでチェックする
        var result = await UpdateChecker.CheckForUpdate((UpdateChannel)SelectedUpdateChannel);
        if (!result)
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.UpdateDialog.NoUpdateAvailableTitle],
                Localizer.Instance.Get(Loc.UpdateDialog.NoUpdateAvailable, AvatarExplorerApp.CurrentVersion),
                NotificationType.Information
            );
        }
    }

    private Task OpenTwitter() => LauncherService.OpenUri(DeveloperLink.TwitterURL);
    private Task OpenGithub() => LauncherService.OpenUri(DeveloperLink.GithubURL);
    private Task OpenSourceCode() => LauncherService.OpenUri(SoftwareLink.RepositoryURL);
    private Task OpenIssues() => LauncherService.OpenUri(SoftwareLink.IssuesURL);
    private async Task ViewLicense()
    {
        var licensePath = Path.Combine(AppContext.BaseDirectory, SystemFileName.License);
        if (File.Exists(licensePath))
        {
            await LauncherService.OpenUri(licensePath);
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.LicenseFileNotFound],
                NotificationType.Error
            );
        }
    }
    private async Task ViewThirdPartyLicenses()
    {
        var licensePath = Path.Combine(AppContext.BaseDirectory, SystemFileName.ThirdPartyLicenses);
        if (File.Exists(licensePath))
        {
            await LauncherService.OpenUri(licensePath);
        }
        else
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                Localizer.Instance[Loc.Error.ThirdPartyLicenseFileNotFound],
                NotificationType.Error
            );
        }
    }

    public RuntimeSettings CreateRuntimeSettings()
    {
        return new RuntimeSettings
        {
            DataRootDirectory = ItemsFolderPath,
            AutoBackupRootDirectory = AutoBackupFolderPath,
            RemoveOriginal = RemoveOriginal,
            ShouldLinkToOriginal = LinkToOriginal,
            AutoBackupInterval = ValueParser.Int(AutoBackupInterval, 5),
            TreatEmptySupportedAvatarAsNone = TreatEmptySupportedAvatarAsNone,
            HideAvatarCategoryWhenAvatarSelected = HideAvatarCategoryWhenAvatarSelected,
            MaxDegreeOfParallelism = ValueParser.Int(MaxDegreeOfParallelism, 4),
            AutoChangeUnitypackagePath = AutoChangeUnitypackagePath,
            CheckForUpdate = CheckForUpdate,
            UpdateChannel = (UpdateChannel)SelectedUpdateChannel
        };
    }
    private void Apply()
    {
        InstanceRepository.RuntimeSettingsRepository.Update(CreateRuntimeSettings());
        InstanceRepository.UserPreferencesRepository.Update(InstanceRepository.UserPreferences with
        {
            Language = SelectedLanguage,
            Theme = (Theme)SelectedTheme,
            RemoveBrackets = RemoveBrackets,
            NormalIconSize = (int)NormalIconSize,
            EnableHoverIconSize = EnableHoverIconSize,
            HoverIconSize = (int)HoverIconSize,
            AntiAliasingMode = (BitmapAntiAliasingMode)SelectedAntiAliasing,
            ItemsPerPage = ValueParser.Int(ItemsPerPage, 30),
            ThumbnailCompressionMaxEdge = (int)ThumbnailCompressionMaxSize,
            UseBackgroundImage = UseBackgroundImage,
            BackgroundImage = BackgroundImagePath,
            BackgroundOpacity = (int)BackgroundImageOpacity,
            SortOrder = (ItemSortOrder)SelectedSortOrder,
            SortDirection = SelectedSortDirection,
            ImplementedSort = (ImplementedSort)SelectedImplementedSort,
            MainViewMode = (MainItemViewMode)SelectedViewMode,
            GridItemSize = (GridItemSize)SelectedGridItemSize,
            EnableSearchInFolder = EnableSearchInFolder,
        });
    }
    private void Close()
    {
        IsVisible = false;
    }
}
