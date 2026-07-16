using Avalonia.Controls;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        NotificationManager = MainWindowViewModel.Instance.NotificationManager;
    }
}
