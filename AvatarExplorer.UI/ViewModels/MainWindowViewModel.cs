using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.Updates;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.ViewModels.Overlays;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public string WindowTitle { get; set; } = string.Empty;
    [Reactive] public ImageBrush? BackgroundImage { get; set; } = null;
    public static AvatarExplorerApp AvatarExplorerApp => AvatarExplorerApp.Instance;

    public static MainWindowViewModel Instance { get; private set; } = null!;

    public UserPreferencesRepository UserPreferences { get; } = new();

    public MainViewModel MainVM { get; } = new();

    public AddItemViewModel AddItemVM { get; } = new();
    [Reactive] public bool IsAddItemVisible { get; set; }

    public ArchivePasswordDialogViewModel ArchivePasswordDialogVM { get; } = new();
    [Reactive] public bool IsArchivePasswordDialogVisible { get; set; }

    public EditCommonAvatarsViewModel EditCommonAvatarsVM { get; } = new();

    public SelectAvatarsViewModel SelectAvatarsVM { get; } = new();
    [Reactive] public bool SelectAvatarsVisible { get; set; }

    public EditMemoViewModel EditMemoVM { get; } = new();
    [Reactive] public bool IsEditMemoVisible { get; set; }

    public EditTagsViewModel EditTagsVM { get; } = new();
    [Reactive] public bool IsEditTagsVisible { get; set; }

    public ErrorLogViewModel ErrorLogVM { get; } = new();

    public FatalErrorViewModel FatalErrorVM { get; } = new();

    public FetchAllThumbnailsViewModel FetchAllThumbnailsVM { get; } = new();

    public ImportDataViewModel ImportDataVM { get; } = new();

    public ImportThumbnailViewModel ImportThumbnailVM { get; } = new();

    public InitialSetupViewModel InitialSetupVM { get; } = new();

    public MergeCategoryViewModel MergeCategoryVM { get; } = new();

    public PdfViewerViewModel PdfViewerVM { get; } = new();

    public ProgressViewModel ProgressVM { get; } = new();

    public ResolveTempAvatarViewModel ResolveTempAvatarVM { get; } = new();
    [Reactive] public bool IsResolveTempAvatarVisible { get; set; }

    public SettingsViewModel SettingsVM { get; } = new();

    public TextDialogViewModel TextDialogVM { get; } = new();
    [Reactive] public bool IsTextDialogVisible { get; set; }

    public UnitypackageViewerViewModel UnitypackageViewerVM { get; } = new();

    public UpdateDialogViewModel UpdateDialogVM { get; } = new();
    [Reactive] public bool IsUpdateDialogVisible { get; set; }

    public YesNoDialogViewModel YesNoDialogVM { get; } = new();
    [Reactive] public bool IsYesNoDialogVisible { get; set; }

    public WindowNotificationManager NotificationManager { get; } = new();

    public MainWindowViewModel()
    {
        Instance = this;

        UserPreferences.OnSettingsChanged += OnPreferenceSettingsUpdated;
        UserPreferences.Load();

        UpdateWindowTitle();
        Localizer.Instance.LanguageChanged += UpdateWindowTitle;
        AvatarExplorerApp.ArchivePasswordProvider = GetArchivePassword;
        UpdateChecker.UpdateAvailable += OnUpdateAvailable;

        _ = CheckForUpdateOnStartup();
    }

    private void OnPreferenceSettingsUpdated(UserPreferences settings)
    {
        SetBackgroundImage(settings.BackgroundImage, settings.BackgroundOpacity);
        SetTheme(settings.Theme);
    }

    private void OnUpdateAvailable(VersionRelease release)
    {
        UpdateDialogVM.Open(AvatarExplorerApp.CurrentVersion, release);
        IsUpdateDialogVisible = true;
    }

    private async Task CheckForUpdateOnStartup()
    {
        var settings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;
        await UpdateChecker.CheckForUpdate(settings);
    }

    private void UpdateWindowTitle()
    {
        var title = string.Format("VRC Avatar Explorer v{0}", AvatarExplorerApp.CurrentVersion);

        if (ProcessUtils.IsWindows() && SchemeService.IsRunAsAdmin())
            title += string.Format(" - [{0}]", Localizer.Instance[LocalizationKey.Title.AdministratorMode]);

        WindowTitle = title;
    }

    public void ShowAddItem(string? itemId = null)
    {
        AddItemVM.Open(itemId);
        IsAddItemVisible = true;
    }

    public async Task<string?> ShowEditMemoDialog(string memo)
    {
        EditMemoVM.Memo = memo;
        
        IsEditMemoVisible = true;
        var result = await EditMemoVM.WaitForResult();
        IsEditMemoVisible = false;

        return result;
    }

    public async Task<string[]?> ShowSelectAvatars(string title, string[]? avatars = null, bool includeCommonAvatar = false, bool includeTempAvatar = true, bool allowCreateTempAvatar = false)
    {
        SelectAvatarsVM.Open(title, avatars, includeCommonAvatar, includeTempAvatar, allowCreateTempAvatar);

        SelectAvatarsVisible = true;
        var result = await SelectAvatarsVM.WaitForResult();
        SelectAvatarsVisible = false;

        return result;
    }

    public async Task<string[]?> ShowEditTagsDialog(string[]? tags = null)
    {
        EditTagsVM.Open(tags);
        
        IsEditTagsVisible = true;
        var result = await EditTagsVM.WaitForResult();
        IsEditTagsVisible = false;

        return result;
    }

    public async Task<bool> ShowYesNoDialog(string title, string content)
    {
        YesNoDialogVM.Title = title;
        YesNoDialogVM.Content = content;
        
        IsYesNoDialogVisible = true;
        var result = await YesNoDialogVM.WaitForResult();
        IsYesNoDialogVisible = false;

        return result;
    }

    public async Task<string?> ShowTextDialog(string title, string content = "")
    {
        TextDialogVM.Title = title;
        TextDialogVM.Content = content;
        
        IsTextDialogVisible = true;
        var result = await TextDialogVM.WaitForResult();
        IsTextDialogVisible = false;

        return result;
    }

    public async ValueTask<string?> GetArchivePassword(ArchivePasswordRequest request)
    {
        IsArchivePasswordDialogVisible = true;
        var password = await ArchivePasswordDialogVM.WaitForResult(request);
        IsArchivePasswordDialogVisible = false;

        return password;
    }

    private void SetBackgroundImage(string path, int opacity)
    {
        try
        {
            var image = new ImageBrush()
            {
                Source = new Bitmap(path),
                Opacity = Math.Clamp(opacity / 100.0, 0, 1),
                Stretch = Stretch.UniformToFill
            };

            BackgroundImage = image;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
    private static void SetTheme(Theme theme)
    {
        var application = Application.Current;
        if (application == null) return;

        application.RequestedThemeVariant = theme.GetThemeVariant();
    }

    public void ShowNotification(string title, string content, NotificationType type)
    {
        NotificationManager.Show(new Notification()
        {
            Title = title,
            Message = content,
            Type = type,
            Expiration = TimeSpan.FromSeconds(5)
        });
    }

    public static async Task CheckForUpdate()
    {
        var settings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;
        await UpdateChecker.CheckForUpdate(settings);
    }
}
