using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;

namespace AvatarExplorer.UI.ViewModels.Component;

public class TagViewModel : ViewModelBase
{
    public string Label { get; set; } = string.Empty;
    public bool IsCommonAvatar { get; set; } = false;

    public string ValueRaw { get; set; } = string.Empty;

    public void Update()
    {
        if (IsCommonAvatar) Label = Localizer.Instance.Get(LocalizationKey.Button.Tag.CommonAvatar, ValueRaw);
        else Label = ValueRaw;
    }
}
