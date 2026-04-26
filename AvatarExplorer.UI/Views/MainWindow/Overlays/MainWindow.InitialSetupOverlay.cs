using System.Threading.Tasks;
using Avalonia.Interactivity;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.Utilities;

namespace AvatarExplorer.UI;

public partial class MainWindow
{
    private TaskCompletionSource<bool>? _initialSetupOverlay_tcs;

    private async Task InitialSetupOverlay_ShowAsync()
    {
        _initialSetupOverlay_tcs?.TrySetResult(false);
        _initialSetupOverlay_tcs = new();

        InitialSetupOverlay_LanguageComboBox.SelectedIndex = UserPreferences.Language;
        InitialSetupOverlay_ItemsFolderPathTextBox.Text = RuntimeSettings.DataRootDirectory;

        InitialSetupOverlay.IsVisible = true;

        await _initialSetupOverlay_tcs.Task;
    }

    #region Event Handler
    private void InitialSetupOverlay_LanguageComboBox_SelectionChanged(object? sender, RoutedEventArgs e)
    {
        if (InitialSetupOverlay == null || !InitialSetupOverlay.IsVisible) return;

        if (InitialSetupOverlay_LanguageComboBox != null)
        {
            _userPreferencesManager.Update(UserPreferences with
            {
                Language = InitialSetupOverlay_LanguageComboBox?.SelectedIndex ?? 0
            });

            SettingsOverlay_ApplyLanguage(UserPreferences.Language);
        }
    }

    private async void InitialSetupOverlay_OpenFolder_Click(object? sender, RoutedEventArgs e)
    {
        string[]? folders = await StorageService.OpenFolderDialog(this, Localizer.Instance[LocalizationKey.Dialog.SelectFolderPath], false);
        if (folders == null || folders.Length == 0) return;

        if (InitialSetupOverlay_ItemsFolderPathTextBox != null)
        {
            AvatarExplorer.SetRuntimeSettings(RuntimeSettings with
            {
                DataRootDirectory = folders[0]
            });
            
            InitialSetupOverlay_ItemsFolderPathTextBox.Text = RuntimeSettings.DataRootDirectory;
        }
    }

    private void InitialSetupOverlay_OK_Click(object? sender, RoutedEventArgs e)
    {
        _userPreferencesManager.Save();
        AvatarExplorer.SaveRuntimeSettings();

        InitialSetupOverlay.IsVisible = false;
        _initialSetupOverlay_tcs?.TrySetResult(true);
    }
    #endregion
}
