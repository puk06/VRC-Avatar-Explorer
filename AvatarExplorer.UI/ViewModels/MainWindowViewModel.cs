namespace AvatarExplorer.UI.ViewModels;

public class MainWindowViewModel
{
    public static MainWindowViewModel Instance { get; private set; } = null!;

    public MainWindowViewModel()
    {
        Instance = this;
    }
}
