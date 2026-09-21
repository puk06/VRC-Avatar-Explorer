using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class TagViewModel(string valueRaw) : ViewModelBase
{
    [Reactive] public partial string Label { get; private set; } = string.Empty;
    [Reactive] public partial bool IsCommonAvatar { get; set; } = false;
    [Reactive] public partial bool IsBoothId { get; set; } = false;

    public string ValueRaw { get; } = valueRaw;

    public void Update()
    {
        if (IsCommonAvatar) Label = Localizer.Instance.Get(Loc.Button.Tag.CommonAvatar, ValueRaw);
        else if (IsBoothId) Label = Localizer.Instance.Get(Loc.Button.Tag.BoothId, ValueRaw);
        else Label = ValueRaw;
    }
}
