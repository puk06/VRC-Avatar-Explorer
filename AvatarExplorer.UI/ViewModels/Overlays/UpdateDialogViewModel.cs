using System.Threading.Tasks;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class UpdateDialogViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }

    private string CurrentVersion { get; set; } = string.Empty;
    private string LatestVersion { get; set; } = string.Empty;
    private string ReleaseDate { get; set; } = string.Empty;
    public string ReleaseUrl { get; set; } = string.Empty;

    [Reactive] public string VersionText { get; set; } = string.Empty;
    [Reactive] public string Content { get; set; } = string.Empty;

    public IReactiveCommand LaterCommand { get; }
    public IReactiveCommand UpdateNowCommand { get; }

    public UpdateDialogViewModel()
    {
        LaterCommand = ReactiveCommand.Create(OnLater);
        UpdateNowCommand = ReactiveCommand.CreateFromTask(UpdateNow);
    }

    public void Open(string currentVersion, VersionRelease latestRelease)
    {
        CurrentVersion = currentVersion;
        LatestVersion = latestRelease.Version;
        ReleaseDate = latestRelease.ReleaseDate;
        ReleaseUrl = latestRelease.ReleaseUrl;
        Content = latestRelease.ChangeLogs.ToString();

        UpdateVersionText();
        IsVisible = true;
    }

    private void UpdateVersionText()
    {
        VersionText = Localizer.Instance.Get(Loc.UpdateDialog.VersionText, [$"v{LatestVersion}", $"v{CurrentVersion}", ReleaseDate]);
    }

    private void OnLater()
    {
        IsVisible = false;
    }

    private async Task UpdateNow()
    {
        await LauncherService.OpenUri(ReleaseUrl);
        IsVisible = false;
    }
}
