using AvatarExplorer.UI.Interfaces;
using DynamicData.Binding;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Panels;

public partial class AdvancedSearchViewModel : ViewModelBase, IInitializable
{
    [Reactive] public partial string Title { get; set; } = string.Empty;
    [Reactive] public partial string Author { get; set; } = string.Empty;
    [Reactive] public partial string BoothId { get; set; } = string.Empty;
    [Reactive] public partial string SupportedAvatar { get; set; } = string.Empty;
    [Reactive] public partial string Category { get; set; } = string.Empty;
    [Reactive] public partial string Memo { get; set; } = string.Empty;
    [Reactive] public partial string ImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public partial string NotImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public partial string Tag { get; set; } = string.Empty;
    [Reactive] public partial string CommonAvatar { get; set; } = string.Empty;
    [Reactive] public partial bool IsOr { get; set; }
    [Reactive] public partial bool IncludeHidden { get; set; }

    public event Action? SearchPropertyChanged;

    public AdvancedSearchViewModel()
    {
        IInitializableRegistry.Register(0, this);
    }

    public async Task Initialize()
    {
        this.WhenAnyPropertyChanged()
            .Subscribe(_ => SearchPropertyChanged?.Invoke());
    }
}
