using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Media.Imaging;
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
using AvatarExplorer.UI.Models.Common;
using AvatarExplorer.UI.Models.Settings;
using AvatarExplorer.UI.Services;
using AvatarExplorer.UI.Services.System;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.ViewModels.Overlays;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase, IInitializable, IPostInitializable
{
    [Reactive] public string WindowTitle { get; set; } = string.Empty;
    [Reactive] public ImageBrush? BackgroundImage { get; set; } = null;

    public static AvatarExplorerApp AvatarExplorerApp => AvatarExplorerApp.Instance;
    public static MainWindowViewModel Instance { get; private set; } = null!;

    public string? LastDragDropPath { get; set; } = null;

    public MainViewModel MainVM { get; } = new();

    public ItemEditorViewModel ItemEditorVM { get; } = new();

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
    public ExportDataViewModel ExportDataVM { get; } = new();

    public InitialSetupViewModel InitialSetupVM { get; } = new();

    public MergeCategoryViewModel MergeCategoryVM { get; } = new();

    public PdfViewerViewModel PdfViewerVM { get; } = new();

    public ProgressViewModel ProgressVM { get; } = new();

    public ResolveTempAvatarViewModel ResolveTempAvatarVM { get; } = new();

    public SettingsViewModel SettingsVM { get; } = new();

    public TextDialogViewModel TextDialogVM { get; } = new();
    [Reactive] public bool IsTextDialogVisible { get; set; }

    public UnitypackageViewerViewModel UnitypackageViewerVM { get; } = new();

    public UpdateDialogViewModel UpdateDialogVM { get; } = new();
    [Reactive] public bool IsUpdateDialogVisible { get; set; }

    public YesNoDialogViewModel YesNoDialogVM { get; } = new();
    [Reactive] public bool IsYesNoDialogVisible { get; set; }

    public WindowNotificationManager? NotificationManager { get; set; }

    public event Action? WindowClosing;

    public MainWindowViewModel()
    {
        Instance = this;

        IInitializableRegistry.Register(-1, (IInitializable)this);
        IInitializableRegistry.Register(9999, (IPostInitializable)this);
    }

    private bool _thumbnailWarmupStatus;
    private void OnThumbnailWarmupChanged(bool status)
    {
        Dispatcher.UIThread.Post(() =>
        {
            _thumbnailWarmupStatus = status;
            UpdateWindowTitle();
        });
    }

    private void OnBackupRestored()
    {
        AppInitializer.InitializeUserPreferences();
    }

    public async Task Initialize()
    {
        AppInitializer.InitializeApp();
        AppInitializer.InitializeLocalization();
        AppInitializer.InitializeContextMenu();
        AppInitializer.InitializeUserPreferences();
        AppInitializer.RegisterBackupFiles();
        AppInitializer.StartThumbnailCacheWarmup();
        AppInitializer.StartSingleInstanceService();

        UserPreferencesService.Instance.Repository.OnSettingsChanged += ApplyPreferenceSettings;
        Localizer.Instance.LanguageChanged += UpdateWindowTitle;
        AvatarExplorerApp.ArchivePasswordProvider = GetArchivePassword;
        UpdateChecker.UpdateAvailable += OnUpdateAvailable;
        SingleInstanceService.OnPipeMessageReceived += OnPipeMessageReceived;
        ImageService.ThumbnailCacheWarmupStateChanged += OnThumbnailWarmupChanged;
        AvatarExplorerApp.BackupManager.OnBackupRestored += OnBackupRestored;

        ApplyPreferenceSettings(UserPreferencesService.Instance.Repository.Settings);
        UpdateWindowTitle();
    }
    public async Task OnInitialized()
    {
        CheckForUpdateOnStartup();
    }

    private void OnPipeMessageReceived(string[] args) => Dispatcher.UIThread.Post(() => SendApplicationArgs(args));
    
    public event Action<string[]>? OnApplicationArgsReceived;
    public void SendApplicationArgs(string[] args)
    {
        OnArgsReceived(args);
        OnApplicationArgsReceived?.Invoke(args);
    }

    public void OnArgsReceived(string[] args)
    {
        if (args == null || args.Length == 0 || string.IsNullOrEmpty(args[0])) return;

        var uri = args[0];
        if (uri.StartsWith(SchemeService.ProtocolBLM + "://"))
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

    private void ApplyPreferenceSettings(UserPreferences settings)
    {
        Localizer.Instance.SetLanguage(settings.Language);

        if (settings.UseBackgroundImage) SetBackgroundImage(settings.BackgroundImage, settings.BackgroundOpacity);
        else BackgroundImage = null;

        SetTheme(settings.Theme);
    }

    private static async void CheckForUpdateOnStartup()
    {
        var settings = AvatarExplorerApp.Instance.RuntimeSettings.Settings;
        if (!settings.CheckForUpdate) return;

        await UpdateChecker.CheckForUpdate(settings.UpdateChannel);
    }
    private void OnUpdateAvailable(VersionRelease release)
    {
        UpdateDialogVM.Open(AvatarExplorerApp.CurrentVersion, release);
        IsUpdateDialogVisible = true;
    }

    private void UpdateWindowTitle()
    {
        var title = string.Format("VRC Avatar Explorer v{0}", AvatarExplorerApp.CurrentVersion);

        if (ProcessUtils.IsWindows() && SchemeService.IsRunAsAdmin())
            title += string.Format(" - [{0}]", Localizer.Instance[Loc.Title.AdministratorMode]);

        if (_thumbnailWarmupStatus)
            title += string.Format(" - {0}", Localizer.Instance[Loc.Title.CacheGeneration]);

        WindowTitle = title;
    }

    public void ShowItemEditor(string? itemId = null) => ItemEditorVM.Open(itemId);
    public void OnFilesDrop(string[] filePaths)
    {
        // TODO: URLのD&Dに対応してもいいかも

        // ソフト内からD&Dしたアイテムはスキップするように
        if (filePaths.Length == 1 && filePaths[0] == LastDragDropPath) return;

        ItemEditorVM.AddPaths(filePaths);
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

    public void ShowTempAvatarResolver(string tempAvatar)
    {
        ResolveTempAvatarVM.Open(tempAvatar);
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

    public void ShowUnitypackageViewer(string filePath) => UnitypackageViewerVM.Open(filePath);
    public void ShowPdfViewer(string filePath) => PdfViewerVM.Open(filePath);
    public void ShowErrorLog() => ErrorLogVM.IsVisible = true;

    private void SetBackgroundImage(string path, int opacity)
    {
        try
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

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
    private static void SetTheme(Theme theme)
    {
        var application = Application.Current;
        if (application == null) return;

        application.RequestedThemeVariant = theme.GetThemeVariant();
    }

    public void ShowNotification(string title, string content, NotificationType type)
    {
        NotificationManager?.Show(new Notification()
        {
            Title = title,
            Message = content,
            Type = type,
            Expiration = TimeSpan.FromSeconds(5)
        });
    }

    public void OnWindowClosing()
    {
        WindowClosing?.Invoke();
    }
}
