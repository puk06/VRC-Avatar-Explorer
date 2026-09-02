using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI.ViewModels.Component;

public class TagViewModel : ViewModelBase
{
    public string Label { get; private set; } = string.Empty;
    public bool IsCommonAvatar { get; set; } = false;
    public bool IsBoothId { get; set; } = false;

    public string ValueRaw { get; set; } = string.Empty;

    public void Update()
    {
        if (IsCommonAvatar) Label = Localizer.Instance.Get(Loc.Button.Tag.CommonAvatar, ValueRaw);
        else if (IsBoothId) Label = Localizer.Instance.Get(Loc.Button.Tag.BoothId, ValueRaw);
        else Label = ValueRaw;
    }
}
