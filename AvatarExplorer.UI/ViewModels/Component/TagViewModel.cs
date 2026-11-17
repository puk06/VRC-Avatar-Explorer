using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class TagViewModel(string valueRaw) : ViewModelBase
{
    [Reactive] public partial string Label { get; private set; } = string.Empty;
    [Reactive] public partial bool IsCommonAvatar { get; set; } = false;
    [Reactive] public partial bool IsSupportedAvatarAll { get; set; } = false;
    [Reactive] public partial bool IsSupportedAvatarNone { get; set; } = false;
    [Reactive] public partial bool IsBoothId { get; set; } = false;

    public string ValueRaw { get; } = valueRaw;

    /// <summary>共通素体でも対応アバターの状態タグでもない、通常のタグであるかどうかを示します。</summary>
    public bool IsNormalTag => !IsCommonAvatar && !IsSupportedAvatarAll && !IsSupportedAvatarNone;

    public void Update()
    {
        if (IsCommonAvatar) Label = Localizer.Instance.Get(Loc.Button.Tag.CommonAvatar, ValueRaw);
        else if (IsSupportedAvatarAll) Label = Localizer.Instance.Get(Loc.Button.Tag.SupportedAvatar.All);
        else if (IsSupportedAvatarNone) Label = Localizer.Instance.Get(Loc.Button.Tag.SupportedAvatar.None);
        else if (IsBoothId) Label = Localizer.Instance.Get(Loc.Button.Tag.BoothId, ValueRaw);
        else Label = ValueRaw;
    }
}
