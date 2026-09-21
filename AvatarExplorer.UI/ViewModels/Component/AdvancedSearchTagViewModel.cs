using AvatarExplorer.UI.Localization;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class AdvancedSearchTagViewModel(string valueRaw) : ViewModelBase
{
    public const string NegationPrefix = "~";

    [Reactive] public partial string Label { get; private set; } = string.Empty;
    [Reactive] public partial bool IsLocalizable { get; set; } = false;
    [Reactive] public partial bool IsNegation { get; set; } = false;

    public string ValueRaw { get; } = valueRaw;

    public AdvancedSearchTagViewModel Update()
    {
        if (IsLocalizable) Label = Localizer.Instance[ValueRaw];
        else Label = ValueRaw;

        return this;
    }

    public string GetSearchValue()
    {
        var value = ValueRaw;

        if (IsLocalizable) value = Localizer.Instance[value];
        if (IsNegation) value = NegationPrefix + value;

        return value;
    }
}
