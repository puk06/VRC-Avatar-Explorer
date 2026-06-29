using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class FatalErrorViewModel : ViewModelBase
{
    [Reactive] public string Title { get; set; } = "えらー！！";
    [Reactive] public string Content { get; set; } = "うお（笑）";
}
