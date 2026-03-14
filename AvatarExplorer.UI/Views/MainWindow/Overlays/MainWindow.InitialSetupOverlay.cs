using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<bool>? _initialSetupTcs;

    private async Task InitialSetupOverlay_ShowAsync()
    {
        _initialSetupTcs?.TrySetResult(false);
        _initialSetupTcs = new();

        InitialSetupOverlay_LanguageComboBox.SelectedIndex = _userPreferences.Language;
        InitialSetupOverlay_ItemsFolderPathTextBox.Text = RuntimeSettings.DataRootDirectory;

        InitialSetupOverlay.IsVisible = true;

        await _initialSetupTcs.Task;
    }

    private void InitialSetupOverlay_LanguageComboBox_SelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (InitialSetupOverlay == null || !InitialSetupOverlay.IsVisible) return;

        if (InitialSetupOverlay_LanguageComboBox != null)
        {
            _userPreferences = _userPreferences with
            {
                Language = InitialSetupOverlay_LanguageComboBox?.SelectedIndex ?? 0
            };

            SettingsOverlay_ApplyLanguage(_userPreferences.Language);
        }
    }

    private async void InitialSetupOverlay_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false);
        if (folders == null || folders.Length == 0) return;

        if (InitialSetupOverlay_ItemsFolderPathTextBox != null)
        {
            _avatarExplorerApp.SetRuntimeSettings(RuntimeSettings with
            {
                DataRootDirectory = folders[0]
            });
            
            InitialSetupOverlay_ItemsFolderPathTextBox.Text = RuntimeSettings.DataRootDirectory;
        }
    }

    private void InitialSetupOverlay_OK_Click(object? sender, RoutedEventArgs e)
    {
        UserPreferencesService.Save(_userPreferences);
        _avatarExplorerApp.SaveRuntimeSettings();

        InitialSetupOverlay.IsVisible = false;
        _initialSetupTcs?.TrySetResult(true);
    }
}
