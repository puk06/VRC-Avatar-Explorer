using Avalonia.Controls;
using Avalonia.Threading;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.ViewModels;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class SettingsOverlay : UserControl
{
    public SettingsOverlay()
    {
        InitializeComponent();
        DataContext = MainWindowViewModel.Instance.SettingsVM;
        Localizer.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            RefreshComboBox(SortOrderComboBox);
            RefreshComboBox(SortDirectionComboBox);
            RefreshComboBox(ThemeComboBox);
            RefreshComboBox(AntiAliasingComboBox);
            RefreshComboBox(UpdateChannelComboBox);
        });
    }

    private static void RefreshComboBox(ComboBox comboBox)
    {
        var selectedIndex = comboBox.SelectedIndex;
        comboBox.SelectedIndex = -1;
        comboBox.SelectedIndex = selectedIndex;
    }
}

