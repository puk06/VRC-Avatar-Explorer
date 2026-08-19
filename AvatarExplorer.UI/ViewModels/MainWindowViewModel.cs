using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Models.Updates;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Services.Updates;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Extensions;
using AvatarExplorer.UI.Interfaces;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Utils;
using AvatarExplorer.UI.ViewModels.Overlays;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase, IInitializable, IPostInitializable
{
    [Reactive] public string WindowTitle { get; set; } = string.Empty;
    [Reactive] public ImageBrush? BackgroundImage { get; set; } = null;
    [Reactive] public IBrush? Background { get; set; } = null;
    [Reactive] public FontFamily FontFamily { get; set; } = FontUtils.GetFontFamily(null);

    public static MainWindowViewModel Instance { get; private set; } = null!;
    public string? LastDragDropPath { get; set; } = null;

    public MainViewModel MainVM { get; } = new();
    public ItemEditorViewModel ItemEditorVM { get; } = new();
    public EditCommonAvatarsViewModel EditCommonAvatarsVM { get; } = new();
    public ErrorLogViewModel ErrorLogVM { get; } = new();
    public FetchAllThumbnailsViewModel FetchAllThumbnailsVM { get; } = new();
    public FetchAllVariationHashesViewModel FetchAllVariationHashesVM { get; } = new();
    public ImportDataViewModel ImportDataVM { get; } = new();
    public ExportDataViewModel ExportDataVM { get; } = new();
    public ResetDatabaseViewModel ResetDatabaseVM { get; } = new();
    public InitialSetupViewModel InitialSetupVM { get; } = new();
    public MergeCategoryViewModel MergeCategoryVM { get; } = new();
    public TagEditorViewModel TagEditorVM { get; } = new();
    public PdfViewerViewModel PdfViewerVM { get; } = new();
    public ProgressViewModel ProgressVM { get; } = new();
    public ResolveTempAvatarViewModel ResolveTempAvatarVM { get; } = new();
    public SettingsViewModel SettingsVM { get; } = new();
    public UnitypackageViewerViewModel UnitypackageViewerVM { get; } = new();

    public TextDialogViewModel TextDialogVM { get; } = new();
    [Reactive] public bool IsTextDialogVisible { get; set; }

    public SelectAvatarsViewModel SelectAvatarsVM { get; } = new();
    [Reactive] public bool SelectAvatarsVisible { get; set; }

    public EditMemoViewModel EditMemoVM { get; } = new();
    [Reactive] public bool IsEditMemoVisible { get; set; }

    public EditTagsViewModel EditTagsVM { get; } = new();
    [Reactive] public bool IsEditTagsVisible { get; set; }

    public UpdateDialogViewModel UpdateDialogVM { get; } = new();
    [Reactive] public bool IsUpdateDialogVisible { get; set; }

    public YesNoDialogViewModel YesNoDialogVM { get; } = new();
    [Reactive] public bool IsYesNoDialogVisible { get; set; }

    public ArchivePasswordDialogViewModel ArchivePasswordDialogVM { get; } = new();
    [Reactive] public bool IsArchivePasswordDialogVisible { get; set; }

    public event Action? WindowClosing;

    public string[] ApplicationArgs { get; private set; } = [];

    public MainWindowViewModel()
    {
        Instance = this;

        IInitializableRegistry.Register(-1, (IInitializable)this);
        IInitializableRegistry.Register(9999, (IPostInitializable)this);
    }

    public async Task Initialize()
    {
        AppInitializer.InitializeApp();
        AppInitializer.InitializeLocalization(Path.Combine(AppContext.BaseDirectory, "locales"));
        AppInitializer.InitializeContextMenu();
        AppInitializer.InitializeUserPreferences();
        AppInitializer.RegisterBackupFiles();
        AppInitializer.StartThumbnailCacheWarmup();
        AppInitializer.StartSingleInstanceService();

        InstanceRepository.UserPreferencesRepository.OnSettingsChanged += ApplyPreferenceSettings;
        Localizer.Instance.LanguageChanged += OnLanguageUpdated;
        InstanceRepository.App.ArchivePasswordProvider = GetArchivePassword;
        UpdateChecker.UpdateAvailable += OnUpdateAvailable;
        SingleInstanceService.OnPipeMessageReceived += OnPipeMessageReceived;
        InstanceRepository.App.BackupManager.OnBackupRestored += OnBackupRestored;
        ErrorManager.Instance.OnErrorOccured += OnErrorOccured;

        ApplyPreferenceSettings(InstanceRepository.UserPreferences);
        UpdateFontFamily();
        UpdateWindowTitle();
    }
    public async Task OnInitialized()
    {
        _ = CheckForUpdateOnStartup();
        CheckIfRunningAsAdmin();
    }

    public void OnLanguageUpdated()
    {
        UpdateFontFamily();
        UpdateWindowTitle();
    }
    private void UpdateFontFamily()
    {
        FontFamily = FontUtils.GetFontFamily(Localizer.Instance[Loc.FontFamily]);
    }
    private void OnErrorOccured(string message, Exception? exception, string tag)
    {
        Dispatcher.UIThread.Post(() =>
            NotificationManager.Show(
                Localizer.Instance[Loc.Error.Default],
                message,
                NotificationType.Error
            )
        );
    }

    private void OnBackupRestored()
    {
        AppInitializer.InitializeUserPreferences();
    }

    private static void CheckIfRunningAsAdmin()
    {
        if (!ProcessUtils.IsWindows()) return;

        if (SchemeService.IsRunAsAdmin())
        {
            NotificationManager.Show(
                Localizer.Instance[Loc.Warning.Default],
                Localizer.Instance[Loc.Warning.RunningInAdministratorMode],
                NotificationType.Warning
            );
        }
    }
    private void UpdateWindowTitle()
    {
        var title = string.Format("VRC Avatar Explorer v{0}", AvatarExplorerApp.CurrentVersion);

        if (ProcessUtils.IsWindows() && SchemeService.IsRunAsAdmin())
            title += string.Format(" - [{0}]", Localizer.Instance[Loc.Title.AdministratorMode]);

        WindowTitle = title;
    }

    private static async Task CheckForUpdateOnStartup()
    {
        var settings = InstanceRepository.RuntimeSettings;
        if (!InstanceRepository.RuntimeSettings.CheckForUpdate) return;

        await UpdateChecker.CheckForUpdate(settings.UpdateChannel);
    }
    private void OnUpdateAvailable(VersionRelease release)
    {
        UpdateDialogVM.Open(AvatarExplorerApp.CurrentVersion, release);
        IsUpdateDialogVisible = true;
    }

    private void OnPipeMessageReceived(string[] args) => Dispatcher.UIThread.Post(() => OnArgsReceived(args));
    public void SetApplicationArgs(string[] args)
    {
        ApplicationArgs = args;
        OnArgsReceived(args);
    }
    public void OnArgsReceived(string[] args)
    {
        if (args == null || args.Length == 0 || string.IsNullOrEmpty(args[0])) return;

        var uri = args[0];
        if (uri.StartsWith(SchemeService.ProtocolBLM + "://") ||
            uri.StartsWith(SchemeService.ProtocolVRCAE + "://item-import")) // VRCAE://item-importはBLMと互換 (AssetConnect)
        {
            var blmImportItemInfo = BLMImportItemService.GetBLMImportItemInfo(uri);
            if (blmImportItemInfo != null) ItemEditorVM.Open(blmImportItemInfo);
        }
        else if (uri.StartsWith(SchemeService.ProtocolVRCAE + "://"))
        {
            var launchInfo = LaunchInfoService.GetLaunchInfo(uri);
            if (launchInfo != null) ItemEditorVM.Open(launchInfo);
        }
    }

    public void OnFilesDrop(string[] filePaths)
    {
        // ソフト内からD&Dしたアイテムはスキップするように
        if (filePaths.Length == 1 && filePaths[0] == LastDragDropPath) return;

        ItemEditorVM.AddPaths(filePaths);
    }

    public async Task<string?> ShowEditMemoDialog(string memo)
    {
        IsEditMemoVisible = true;
        var result = await EditMemoVM.ShowAsync(memo);
        IsEditMemoVisible = false;

        return result;
    }
    public async Task<string[]?> ShowSelectAvatars(string title, string[]? avatars = null, bool includeCommonAvatar = false, bool includeTempAvatar = true, bool allowCreateTempAvatar = false)
    {
        SelectAvatarsVisible = true;
        var result = await SelectAvatarsVM.ShowAsync(title, avatars, includeCommonAvatar, includeTempAvatar, allowCreateTempAvatar);
        SelectAvatarsVisible = false;

        return result;
    }
    public async Task<string[]?> ShowEditTagsDialog(string[]? tags = null)
    {
        IsEditTagsVisible = true;
        var result = await EditTagsVM.ShowAsync(tags);
        IsEditTagsVisible = false;

        return result;
    }
    public async Task<bool> ShowYesNoDialog(string title, string content)
    {
        IsYesNoDialogVisible = true;
        var result = await YesNoDialogVM.ShowAsync(title, content);
        IsYesNoDialogVisible = false;

        return result;
    }
    public async Task<string?> ShowTextDialog(string title, string content = "")
    {
        IsTextDialogVisible = true;
        var result = await TextDialogVM.ShowAsync(title, content);
        IsTextDialogVisible = false;

        return result;
    }
    private async ValueTask<string?> GetArchivePassword(ArchivePasswordRequest request)
    {
        IsArchivePasswordDialogVisible = true;
        var password = await ArchivePasswordDialogVM.ShowAsync(request);
        IsArchivePasswordDialogVisible = false;

        return password;
    }

    private void ApplyPreferenceSettings(UserPreferences settings)
    {
        Localizer.Instance.SetLanguage(settings.Language);

        if (settings.UseBackgroundImage) SetBackgroundImage(settings.BackgroundImage, settings.BackgroundOpacity);
        else BackgroundImage = null;

        var (themeVariant, backgroundColor) = settings.Theme.GetThemeVariant();
        SetBackgroundColor(backgroundColor);
        SetTheme(themeVariant);
    }
    private void SetBackgroundImage(string path, int opacity)
    {
        try
        {
            if (string.IsNullOrEmpty(path)) return;

            if (!File.Exists(path))
            {
                ErrorManager.Instance.PostError($"Background image file not found: '{path}'.");
                return;
            }

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
            ErrorManager.Instance.PostError("Failed to set background image.", ex);
        }
    }
    private void SetBackgroundColor(Color color)
    {
        Background = new SolidColorBrush(color);
    }
    private static void SetTheme(ThemeVariant theme)
    {
        var application = Application.Current;
        if (application == null) return;

        application.RequestedThemeVariant = theme;
    }

    public void OnWindowClosing()
    {
        WindowClosing?.Invoke();
        AvatarExplorerApp.ClearTemp();
    }
}
