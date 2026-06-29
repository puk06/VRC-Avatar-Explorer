using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.UI.Localization;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class UpdateDialogViewModel : ViewModelBase
{
    private string CurrentVersion { get; set; } = string.Empty;
    private string LatestVersion { get; set; } = string.Empty;
    private string ReleaseDate { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;

    [Reactive] public string VersionText { get; set; } = string.Empty;
    [Reactive] public string Content { get; set; } = string.Empty;

    public UpdateDialogViewModel()
    {
    }

    public void Open(string currentVersion, VersionRelease latestRelease)
    {
        CurrentVersion = currentVersion;
        LatestVersion = latestRelease.Version;
        ReleaseDate = latestRelease.ReleaseDate;
        ReleaseUrl = latestRelease.ReleaseUrl;
        Content = latestRelease.ChangeLogs.ToString();

        UpdateVersionText();
        Localizer.Instance.LanguageChanged += UpdateVersionText;
    }

    private void UpdateVersionText()
    {
        VersionText = Localizer.Instance.Get(LocalizationKey.UpdateDialog.VersionText, [$"v{LatestVersion}", $"v{CurrentVersion}", ReleaseDate]);
    }
}
