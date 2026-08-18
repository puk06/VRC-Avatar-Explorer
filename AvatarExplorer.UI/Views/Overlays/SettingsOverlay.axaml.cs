using Avalonia.Controls;
using Avalonia.Threading;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Services;

namespace AvatarExplorer.UI.Views.Overlays;

public partial class SettingsOverlay : UserControl
{
    public SettingsOverlay()
    {
        InitializeComponent();
        DataContext = InstanceRepository.MainWindow.SettingsVM;
        Localizer.Instance.LanguageChanged += OnLanguageChanged;
    }

    private void OnLanguageChanged()
    {
        Dispatcher.UIThread.Post(() =>
        {
            RefreshComboBox(SortOrderComboBox);
            RefreshComboBox(ImplementedSortComboBox);
            RefreshComboBox(SortDirectionComboBox);
            RefreshComboBox(ThemeComboBox);
            RefreshComboBox(AntiAliasingComboBox);
            RefreshComboBox(ViewModeComboBox);
            RefreshComboBox(GridItemSizeComboBox);
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

