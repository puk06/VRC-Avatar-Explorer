using AvatarExplorer.UI.Interfaces;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class AdvancedSearchViewModel : ViewModelBase, IInitializable
{
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Author { get; set; } = string.Empty;
    [Reactive] public string BoothId { get; set; } = string.Empty;
    [Reactive] public string SupportedAvatar { get; set; } = string.Empty;
    [Reactive] public string Category { get; set; } = string.Empty;
    [Reactive] public string Memo { get; set; } = string.Empty;
    [Reactive] public string ImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public string NotImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public string Tag { get; set; } = string.Empty;
    [Reactive] public string CommonAvatar { get; set; } = string.Empty;
    [Reactive] public bool IsOr { get; set; }
    [Reactive] public bool IncludeHidden { get; set; }

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
