using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class FatalErrorViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public string Content { get; set; } = string.Empty;

    public void Open(string title, string content)
    {
        Title = title;
        Content = content;
        IsVisible = true;
    }
}
