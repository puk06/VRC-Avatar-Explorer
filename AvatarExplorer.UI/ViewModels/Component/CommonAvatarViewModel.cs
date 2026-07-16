using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class CommonAvatarViewModel : ViewModelBase
{
    [Reactive] public string DisplayName { get; set; } = string.Empty;
    [Reactive] public string Identifier { get; set; } = string.Empty;
}
