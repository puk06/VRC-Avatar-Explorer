using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Overlays;

public class ProgressViewModel : ViewModelBase
{
    [Reactive] public bool IsVisible { get; set; }
    [Reactive] public string Title { get; set; } = string.Empty;
    [Reactive] public int Progress { get; set; } = 0;
    [Reactive] public bool IsIndeterminate { get; set; } = false;

    public void Open(string title)
    {
        Title = title;
        IsVisible = true;
    }

    public void Close()
    {
        Title = string.Empty;
        IsVisible = false;
    }

    public void Update(string title, int progress)
    {
        Title = title;
        Update(progress);
    }

    public void Update(int progress)
    {
        Progress = progress;
        IsIndeterminate = progress == 0;
    }
}
