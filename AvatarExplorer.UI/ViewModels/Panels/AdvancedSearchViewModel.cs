using System;
using DynamicData.Binding;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Panels;

public class AdvancedSearchViewModel : ViewModelBase
{
    [Reactive] public bool OrSearch { get; set; }
    [Reactive] public bool CategoryOrSearch { get; set; }
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Author { get; set; } = string.Empty;
    [Reactive] public string BoothId { get; set; } = string.Empty;
    [Reactive] public string SupportedAvatar { get; set; } = string.Empty;
    [Reactive] public bool TreatEmptySupportedAvatarAsNone { get; set; }
    [Reactive] public string Category { get; set; } = string.Empty;
    [Reactive] public string Memo { get; set; } = string.Empty;
    [Reactive] public string Folder { get; set; } = string.Empty;
    [Reactive] public string File { get; set; } = string.Empty;
    [Reactive] public string ImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public string NotImplementedAvatar { get; set; } = string.Empty;
    [Reactive] public string Tag { get; set; } = string.Empty;
    [Reactive] public string CommonAvatar { get; set; } = string.Empty;

    public event Action? SearchPropertyChanged;

    public AdvancedSearchViewModel()
    {
        this.WhenAnyPropertyChanged()
            .Subscribe(i => SearchPropertyChanged?.Invoke());
    }
}
