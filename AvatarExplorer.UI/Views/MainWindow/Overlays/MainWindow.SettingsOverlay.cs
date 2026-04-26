using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using AvatarExplorer.Core.Data.Links;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Items;
using AvatarExplorer.Core.Models.System;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.Utilities;
using ErrorOr;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private void SettingsOverlay_Open()
    {
        SettingsOverlay_SetUiValueFromCurrentSettings();
        SettingsOverlay.IsVisible = true;
    }
    private void SettingsOverlay_Close() => SettingsOverlay.IsVisible = false;

    private void SettingsOverlay_SetUiValueFromCurrentSettings()
    {
        // 基本
        SettingsOverlay_ItemsFolderPathTextBox?.Text = RuntimeSettings.DataRootDirectory ?? string.Empty;
        
        if (SettingsOverlay_LanguageComboBox != null)
        {
            SettingsOverlay_LanguageComboBox.SelectedIndex = -1;
            SettingsOverlay_LanguageComboBox.SelectedIndex = UserPreferences.Language;
        }

        if (SettingsOverlay_SortOrderComboBox != null)
        {
            SettingsOverlay_SortOrderComboBox.SelectedIndex = -1;
            SettingsOverlay_SortOrderComboBox.SelectedIndex = (int)RuntimeSettings.ItemSortOrder;
        }

        if (SettingsOverlay_ThemeComboBox != null)
        {
            SettingsOverlay_ThemeComboBox.SelectedIndex = -1;
            SettingsOverlay_ThemeComboBox.SelectedIndex = (int)UserPreferences.Theme;
        }

        // 表示
        SettingsOverlay_RemoveBracketsCheckBox?.IsChecked = RuntimeSettings.RemoveBrackets;
        SettingsOverlay_NormalIconSizeSlider?.Value = UserPreferences.NormalIconSize;
        SettingsOverlay_EnableHoverIconSizeCheckBox?.IsChecked = UserPreferences.EnableHoverIconSize;
        SettingsOverlay_HoverIconSizeSlider?.Value = UserPreferences.HoverIconSize;

        if (SettingsOverlay_AntiAliasingModeComboBox != null)
        {
            SettingsOverlay_AntiAliasingModeComboBox.SelectedIndex = -1;
            SettingsOverlay_AntiAliasingModeComboBox.SelectedIndex = (int)UserPreferences.AntiAliasingMode;
        }

        SettingsOverlay_ItemsPerPageTextBox?.Text = UserPreferences.ItemsPerPage.ToString();

        // アイテム
        SettingsOverlay_RemoveOriginalCheckBox?.IsChecked = RuntimeSettings.RemoveOriginal;
        SettingsOverlay_LinkToOriginalCheckBox?.IsChecked = RuntimeSettings.ShouldLinkToOriginal;
        SettingsOverlay_TreatEmptySupportedAvatarAsNoneCheckBox?.IsChecked = RuntimeSettings.TreatEmptySupportedAvatarAsNone;
        SettingsOverlay_ThumbnailCompressionMaxSizeSlider?.Value = UserPreferences.ThumbnailCompressionMaxEdge;

        // 背景
        SettingsOverlay_UseBackgroundImageCheckBox?.IsChecked = UserPreferences.UseBackgroundImage;
        SettingsOverlay_BackgroundImagePathTextBox?.Text = UserPreferences.BackgroundImage ?? string.Empty;
        SettingsOverlay_BackgroundImageOpacitySlider?.Value = UserPreferences.BackgroundOpacity;

        // データ
        SettingsOverlay_AutoBackupPathTextBox?.Text = RuntimeSettings.AutoBackupRootDirectory ?? string.Empty;
        SettingsOverlay_AutoBackupIntervalTextBox?.Text = RuntimeSettings.AutoBackupInterval.ToString();

        // システム
        SettingsOverlay_MaxDegreeOfParallelismTextBox?.Text = RuntimeSettings.MaxDegreeOfParallelism.ToString();
        SettingsOverlay_CheckForUpdateCheckBox?.IsChecked = UserPreferences.CheckForUpdate;
        if (SettingsOverlay_UpdateChannelComboBox != null)
        {
            SettingsOverlay_UpdateChannelComboBox.SelectedIndex = -1;
            SettingsOverlay_UpdateChannelComboBox.SelectedIndex = (int)UserPreferences.UpdateChannel;
        }
    }
    private async Task SettingsOverlay_ApplySettingsValues(bool checkDataCopy = true, bool reloadWindow = true)
    {
        string previousDataRootDirectoryPath = RuntimeSettings.DataRootDirectory;

        RuntimeSettings runtimeSettings = new()
        {
            DataRootDirectory = SettingsOverlay_ItemsFolderPathTextBox?.Text ?? string.Empty,
            ItemSortOrder = (ItemSortOrder)(SettingsOverlay_SortOrderComboBox?.SelectedIndex ?? 0),
            RemoveBrackets = SettingsOverlay_RemoveBracketsCheckBox?.IsChecked ?? false,
            RemoveOriginal = SettingsOverlay_RemoveOriginalCheckBox.IsChecked ?? false,
            ShouldLinkToOriginal = SettingsOverlay_LinkToOriginalCheckBox?.IsChecked ?? false,
            TreatEmptySupportedAvatarAsNone = SettingsOverlay_TreatEmptySupportedAvatarAsNoneCheckBox?.IsChecked ?? false,
            AutoBackupRootDirectory = SettingsOverlay_AutoBackupPathTextBox?.Text ?? string.Empty,
            AutoBackupInterval = ValueParser.Int(SettingsOverlay_AutoBackupIntervalTextBox?.Text, 5),
            MaxDegreeOfParallelism = ValueParser.Int(SettingsOverlay_MaxDegreeOfParallelismTextBox?.Text, 4)
        };

        UserPreferences userPreferences = new()
        {
            Language = SettingsOverlay_LanguageComboBox?.SelectedIndex ?? 0,
            Theme = (Theme)(SettingsOverlay_ThemeComboBox?.SelectedIndex ?? 0),
            NormalIconSize = (int)(SettingsOverlay_NormalIconSizeSlider?.Value ?? 70),
            HoverIconSize = (int)(SettingsOverlay_HoverIconSizeSlider?.Value ?? 200),
            EnableHoverIconSize = SettingsOverlay_EnableHoverIconSizeCheckBox?.IsChecked ?? true,
            AntiAliasingMode = (BitmapAntiAliasingMode)(SettingsOverlay_AntiAliasingModeComboBox?.SelectedIndex ?? 0),
            ItemsPerPage = ValueParser.Int(SettingsOverlay_ItemsPerPageTextBox?.Text, 30),
            ThumbnailCompressionMaxEdge = Math.Clamp((int)(SettingsOverlay_ThumbnailCompressionMaxSizeSlider?.Value ?? 256), 64, 2048),
            UseBackgroundImage = SettingsOverlay_UseBackgroundImageCheckBox?.IsChecked ?? false,
            BackgroundImage = SettingsOverlay_BackgroundImagePathTextBox?.Text ?? string.Empty,
            BackgroundOpacity = Math.Clamp((int)(SettingsOverlay_BackgroundImageOpacitySlider?.Value ?? 20), 0, 100),
            CheckForUpdate = SettingsOverlay_CheckForUpdateCheckBox.IsChecked ?? true,
            UpdateChannel = (UpdateChannel)(SettingsOverlay_UpdateChannelComboBox?.SelectedIndex ?? 0)
        };

        ImageService.SetThumbnailCompressionMaxEdge(userPreferences.ThumbnailCompressionMaxEdge);

        _userPreferencesManager.Update(userPreferences);
        AvatarExplorer.SetRuntimeSettings(runtimeSettings);

        SettingsOverlay_ApplyPreferenceSettingsToUi(reloadWindow);
        SettingsOverlay_SetUiValueFromCurrentSettings();

        if (checkDataCopy && RuntimeSettings.DataRootDirectory != previousDataRootDirectoryPath)
        {
            await SettingsOverlay_CheckDataCopy(previousDataRootDirectoryPath, RuntimeSettings.DataRootDirectory);
        }
    }

    private async Task SettingsOverlay_CheckDataCopy(string previousPath, string currentPath)
    {
        // データをコピーするか
        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.StoragePathChange.CopyData]);
        if (result == null) return;

        if (result == YesNoResult.No)
        {
            YesNoResult? convertPathResult = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.StoragePathChange.ConvertRelativePath]);
            if (convertPathResult != null && convertPathResult == YesNoResult.Yes) AvatarExplorer.ConvertDatabaseRelativePathsToFullPaths(previousPath);

            return;
        }
        
        async Task progressAction((string localizationKey, int progress) tuple)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressOverlay_Show(Localizer.Instance.Get(tuple.localizationKey, tuple.progress.ToString()));
                ProgressOverlay_Update(tuple.progress);
            });
        }

        ErrorOr<CopyResult> result1 = await FileSystemService.CopyDirectoryAsync(previousPath, currentPath, RuntimeSettings.MaxDegreeOfParallelism, progressAction);
        ProgressOverlay_Hide();

        if (result1.IsError)
        {
            ErrorManager.Instance.PostInternalError("Failed to copy directory.", tag: result1.Errors.ToErrorString());
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.CopyDirectoryFailed]);
        }
        else if (result1.Value.Failures.Count > 0)
        {
            result1.Value.Failures.ForEach(i => ErrorManager.Instance.PostInternalError(string.Format("Failed to copy file: '{0}' to '{1}'", i.SourcePath, i.DestinationPath), tag: i.ErrorMessage));
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.FoundProcessingFailedPath]);
        }
        else
        {
            DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.CopyDirectory]);
        }
    }

    private void SettingsOverlay_ApplyPreferenceSettingsToUi(bool reloadWindow = true)
    {
        SettingsOverlay_SetApplicationTheme(Application.Current, UserPreferences.Theme);
        SettingsOverlay_SetBackground(UserPreferences.Theme);
        SettingsOverlay_ApplyBackgroundImage(UserPreferences);
        SettingsOverlay_ApplyLanguage(UserPreferences.Language, reloadWindow);
    }
    private void SettingsOverlay_SetApplicationTheme(Application? application, Theme theme)
    {
        if (application == null) return;

        application.RequestedThemeVariant = theme switch
        {
            Models.Common.Theme.Dark => ThemeVariant.Dark,
            Models.Common.Theme.Light => ThemeVariant.Light,
            Models.Common.Theme.Sakura => AppThemeVariants.Sakura,
            Models.Common.Theme.Mint => AppThemeVariants.Mint,
            Models.Common.Theme.Lavender => AppThemeVariants.Lavender,
            Models.Common.Theme.Ocean => AppThemeVariants.Ocean,
            Models.Common.Theme.Sunset => AppThemeVariants.Sunset,
            Models.Common.Theme.Forest => AppThemeVariants.Forest,
            Models.Common.Theme.Mocha => AppThemeVariants.Mocha,
            Models.Common.Theme.Slate => AppThemeVariants.Slate,
            _ => ThemeVariant.Dark
        };
    }
    private void SettingsOverlay_SetBackground(Theme theme)
    {
        Background = theme switch
        {
            Models.Common.Theme.Dark => new SolidColorBrush(new Color(255, 32, 32, 32)),
            Models.Common.Theme.Light => new SolidColorBrush(new Color(255, 235, 235, 235)),
            Models.Common.Theme.Sakura => new SolidColorBrush(new Color(255, 233, 224, 228)),
            Models.Common.Theme.Mint => new SolidColorBrush(new Color(255, 219, 231, 225)),
            Models.Common.Theme.Lavender => new SolidColorBrush(new Color(255, 223, 216, 236)),
            Models.Common.Theme.Ocean => new SolidColorBrush(new Color(255, 64, 88, 107)),
            Models.Common.Theme.Sunset => new SolidColorBrush(new Color(255, 240, 221, 208)),
            Models.Common.Theme.Forest => new SolidColorBrush(new Color(255, 214, 226, 213)),
            Models.Common.Theme.Mocha => new SolidColorBrush(new Color(255, 110, 91, 85)),
            Models.Common.Theme.Slate => new SolidColorBrush(new Color(255, 87, 94, 110)),
            _ => new SolidColorBrush(new Color(255, 235, 235, 235))
        };
    }
    private void SettingsOverlay_ApplyBackgroundImage(UserPreferences userPreferences)
    {
        if (userPreferences.UseBackgroundImage && !string.IsNullOrEmpty(userPreferences.BackgroundImage) && File.Exists(userPreferences.BackgroundImage))
        {
            WindowPanel.Background = new ImageBrush()
            {
                Source = new Bitmap(userPreferences.BackgroundImage),
                Opacity = Math.Clamp(userPreferences.BackgroundOpacity / 100.0, 0, 1),
                Stretch = Stretch.UniformToFill
            };
        }
        else
        {
            WindowPanel.Background = null;
        }
    }
    private void SettingsOverlay_ApplyLanguage(int language, bool reloadWindow = true)
    {
        bool isLanguageChanged = Localizer.Instance.CurrentLanguageIndex != language;
        if (isLanguageChanged) Localizer.Instance.SetLanguage(language);

        if (reloadWindow && isLanguageChanged) Main_ReloadCurrentWindow();
    }

    #region Event Handler
    private async void SettingsOverlay_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false);
        if (folders == null || folders.Length == 0) return;

        SettingsOverlay_ItemsFolderPathTextBox?.Text = folders[0];
    }
    private async void SettingsOverlay_OpenBackgroundFile_Click(object? sender, RoutedEventArgs e)
    {
        string[]? files = await StorageService.OpenFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFilePath], false);
        if (files == null || files.Length == 0) return;

        SettingsOverlay_BackgroundImagePathTextBox?.Text = files[0];
    }
    private async void SettingsOverlay_OpenAutoBackupRootFolder_Click(object? sender, RoutedEventArgs e)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false, RuntimeSettings.AutoBackupRootDirectory);
        if (folders == null || folders.Length == 0) return;

        SettingsOverlay_AutoBackupPathTextBox?.Text = folders[0];
    }
    private async void SettingsOverlay_RegisterScheme_Click(object? sender, RoutedEventArgs e) => await Main_RegisterSchemeAsync();

    private void SettingsOverlay_Close_Click(object? sender, RoutedEventArgs e) => SettingsOverlay_Close();
    private async void SettingsOverlay_Apply_Click(object? sender, RoutedEventArgs e)
    {
        await SettingsOverlay_ApplySettingsValues();

        // 適用時は自動で保存する
        AvatarExplorer.SaveRuntimeSettings();
        _userPreferencesManager.Save();

        Main_ReloadCurrentWindow();
    }
    private async void SettingsOverlay_UpdateCheckNow_Click(object? sender, RoutedEventArgs e) => await UpdateDialogOverlay_CheckAsync((UpdateChannel)SettingsOverlay_UpdateChannelComboBox.SelectedIndex, false);

    private void SettingsOverlay_ImportData_Click(object? sender, RoutedEventArgs e) => SelectImportTypeOverlay_Show();
    private void SettingsOverlay_ImportThumbnail_Click(object? sender, RoutedEventArgs e) => SelectThumbnailImportTypeOverlay_Show();

    private async void SettingsOverlay_ExportDataToCsv_Click(object? sender, RoutedEventArgs e)
    {
        string? filePath = await StorageService.SaveFileDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectSaveFilePath], "csv");
        if (filePath == null) return;

        Dictionary<ItemType, string> localizedItemTypesMapping = Enum.GetValues<ItemType>().ToDictionary(i => i, i => Localizer.Instance[i.GetLocalizationKey() ?? i.ToString()]);

        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.ExportToCsv.IncludeImplementedToSupported]);
        if (result == null) return;

        ErrorOr<Success> exportResult = await AvatarExplorer.Export(DataExportType.Csv, filePath, localizedItemTypesMapping, includeCommonToSupported: result == YesNoResult.Yes);
        
        if (!exportResult.IsError) DialogOverlay_Show(Localizer.Instance[LocalizationKey.Success.Default], Localizer.Instance[LocalizationKey.Success.Export]);
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ExportFailed]);
    }
    private async void SettingsOverlay_EditCommonAvatars_Click(object? sender, RoutedEventArgs e) => EditCommonAvatarsOverlay_Open();
    private async void SettingsOverlay_ResetItemDatabase_Click(object? sender, RoutedEventArgs e)
    {
        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.ResetItemDatabase]);
        if (result == null || result != YesNoResult.Yes) return;

        AvatarExplorer.ResetItemDatabase();
        AvatarExplorer.ResetTempAvatarDatabase();

        Main_ReloadCurrentWindow();
    }

    private async void SettingsOverlay_ResetCommonAvatarDatabase_Click(object? sender, RoutedEventArgs e)
    {
        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.ResetCommonAvatarDatabase]);
        if (result == null || result != YesNoResult.Yes) return;

        AvatarExplorer.ResetCommonAvatarDatabase();
        Main_ReloadCurrentWindow();
    }

    private async void SettingsOverlay_ResetBulkImportPresetDatabase_Click(object? sender, RoutedEventArgs e)
    {
        YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.ResetBulkImportPresetDatabase]);
        if (result == null || result != YesNoResult.Yes) return;

        AvatarExplorer.ResetBulkImportPresetDatabase();
        Main_ReloadCurrentWindow();
    }
    
    private async void SettingsOverlay_OpenGithub_Click(object? sender, RoutedEventArgs e) => await LauncherService.OpenUri(this, DeveloperLink.GithubURL);

    private async void SettingsOverlay_ViewLicenses_Click(object? sender, RoutedEventArgs e)
    {
        string licenseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "LICENSE.txt");

        if (File.Exists(licenseFile)) await LauncherService.OpenUri(this, licenseFile);
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.LicenseFileNotFound]);
    }

    private async void SettingsOverlay_ThirdPartyLicenses_Click(object? sender, RoutedEventArgs e)
    {
        string licenseFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "THIRD_PARTY_LICENSES.txt");

        if (File.Exists(licenseFile)) await LauncherService.OpenUri(this, licenseFile);
        else DialogOverlay_Show(Localizer.Instance[LocalizationKey.Error.Default], Localizer.Instance[LocalizationKey.Error.ThirdPartyLicenseFileNotFound]);
    }

    private async void SettingsOverlay_OpenTwitter_Click(object? sender, RoutedEventArgs e) => await LauncherService.OpenUri(this, DeveloperLink.TwitterURL);
    private async void SettingsOverlay_OpenSourceCode_Click(object? sender, RoutedEventArgs e) => await LauncherService.OpenUri(this, SoftwareLink.RepositoryURL);

    private async void SettingsOverlay_RestoreDataFromBackup_Click(object? sender, RoutedEventArgs e)
    {
        string[]? folderPaths = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false, RuntimeSettings.AutoBackupRootDirectory);
        if (folderPaths == null || folderPaths.Length == 0) return;

        // バックアップを復元する前に、今の状態をバックアップしておく
        ErrorOr<Success> backupResult = await AvatarExplorer.ExecuteBackup(RuntimeSettings.AutoBackupRootDirectory);

        if (backupResult.IsError)
        {
            YesNoResult? result = await YesNoDialogOverlay_ShowSafeAsync(Localizer.Instance[LocalizationKey.Dialog.Confirmation.Default], Localizer.Instance[LocalizationKey.Dialog.Confirmation.ContinueRestoreFromBackup]);
            if (result == null || result != YesNoResult.Yes) return;
        }

        string backupRootPath = folderPaths[0];

        string itemDatabasePath = Path.Join(backupRootPath, SystemFileName.Database.Items);
        string commonAvatarDatabasePath = Path.Join(backupRootPath, SystemFileName.Database.CommonAvatars);
        string bulkImportPresetDatabasePath = Path.Join(backupRootPath, SystemFileName.Database.BulkImportPresets);
        string tempAvatarsDatabasePath = Path.Join(backupRootPath, SystemFileName.Database.TempAvatars);
        string runtimeSettingsFilePath = Path.Join(backupRootPath, SystemFileName.Settings.Runtime);
        string userPreferencesFilePath = Path.Join(backupRootPath, SystemFileName.Settings.Preferences);

        if (File.Exists(itemDatabasePath))
        {
            AvatarExplorer.LoadItemDatabase(itemDatabasePath);
            AvatarExplorer.SaveItemDatabase();
        }

        if (File.Exists(commonAvatarDatabasePath))
        {
            AvatarExplorer.LoadCommonAvatarDatabase(commonAvatarDatabasePath);
            AvatarExplorer.SaveCommonAvatarDatabase();
        }

        if (File.Exists(bulkImportPresetDatabasePath))
        {
            AvatarExplorer.LoadBulkImportPresetDatabase(bulkImportPresetDatabasePath);
            AvatarExplorer.SaveBulkImportPresetDatabase();
        }

        if (File.Exists(tempAvatarsDatabasePath))
        {
            AvatarExplorer.LoadTempAvatarsDatabase(tempAvatarsDatabasePath);
            AvatarExplorer.SaveTempAvatarsDatabase();
        }

        if (File.Exists(runtimeSettingsFilePath))
        {
            AvatarExplorer.LoadRuntimeSettings(runtimeSettingsFilePath);
            AvatarExplorer.SaveRuntimeSettings();
        }

        if (File.Exists(userPreferencesFilePath))
        {
            _userPreferencesManager.Load(userPreferencesFilePath);
            _userPreferencesManager.Save();
        }

        SettingsOverlay_SetUiValueFromCurrentSettings();
        await SettingsOverlay_ApplySettingsValues();

        Main_ReloadCurrentWindow();
    }
    private async void SettingsOverlay_ShowErrorLog_Click(object? sender, RoutedEventArgs e) => ErrorLogOverlay_Open();
    #endregion
}
