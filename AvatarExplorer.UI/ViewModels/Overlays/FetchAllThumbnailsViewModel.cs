using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class FetchAllThumbnailsViewModel : ViewModelBase
{
    [Reactive] public string Status { get; set; } = string.Empty;
    [Reactive] public string Count { get; set; } = string.Empty;
    [Reactive] public string Eta { get; set; } = string.Empty;
    [Reactive] public string CurrentItem { get; set; } = string.Empty;
    [Reactive] public int Progress { get; set; } = 0;
    [Reactive] public bool IsCancelable { get; set; } = false;

    public IReactiveCommand StartCommand { get; }
    public IReactiveCommand CancelCommand { get; }
    public IReactiveCommand CloseCommand { get; }

    public FetchAllThumbnailsViewModel()
    {
    }

    public void Open()
    {
        Status = Localizer.Instance[LocalizationKey.FetchAllThumbnailsOverlay.Status.Ready];
    }
}
