using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class StartupLoadingViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; } = true;
    [Reactive] public string StatusText { get; set; } = string.Empty;
    [Reactive] public int Progress { get; set; } = 0;
}
