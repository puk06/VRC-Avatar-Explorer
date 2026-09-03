using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class CommonAvatarViewModel : ViewModelBase
{
    [Reactive] public partial string DisplayName { get; set; } = string.Empty;
    [Reactive] public partial string Identifier { get; set; } = string.Empty;
}
