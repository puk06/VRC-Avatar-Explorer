namespace AvatarExplorer.UI.ViewModels.Component;

public class TagViewModel(string tag, bool isCommonAvatar) : ViewModelBase
{
    public string Label { get; } = tag;
    public bool IsCommonAvatar { get; } = isCommonAvatar;
}
