using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Models.External;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.ViewModels.Overlays;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    [Reactive] public string WindowTitle { get; set; } = string.Empty;
    [Reactive] public ImageBrush? BackgroundImage { get; set; } = null;
    public static AvatarExplorerApp AvatarExplorerApp => AvatarExplorerApp.Instance;

    public static MainWindowViewModel Instance { get; private set; } = null!;

    public MainViewModel MainVM { get; } = new();

    public AddItemViewModel AddItemVM { get; } = new();
    [Reactive] public bool IsAddItemVisible { get; set; }
    
    public ArchivePasswordDialogViewModel ArchivePasswordDialogVM { get; } = new();
    [Reactive] public bool IsArchivePasswordDialogVisible { get; set; }

    public EditCommonAvatarsViewModel EditCommonAvatarsVM { get; } = new();
    [Reactive] public bool IsEditCommonAvatarsVisible { get; set; }

    public SelectAvatarsViewModel SelectAvatarsVM { get; } = new();
    [Reactive] public bool SelectAvatarsVisible { get; set; }

    public EditMemoViewModel EditMemoVM { get; } = new();
    [Reactive] public bool IsEditMemoVisible { get; set; }

    public EditTagsViewModel EditTagsVM { get; } = new();
    [Reactive] public bool IsEditTagsVisible { get; set; }

    public ErrorLogViewModel ErrorLogVM { get; } = new();
    [Reactive] public bool IsErrorLogVisible { get; set; }

    public FatalErrorViewModel FatalErrorVM { get; } = new();
    [Reactive] public bool IsFatalErrorVisible { get; set; }

    public FetchAllThumbnailsViewModel FetchAllThumbnailsVM { get; } = new();
    [Reactive] public bool IsFetchAllThumbnailsVisible { get; set; }

    public ImportDataViewModel ImportDataVM { get; } = new();
    [Reactive] public bool IsImportDataVisible { get; set; }
    
    public ImportThumbnailViewModel ImportThumbnailVM { get; } = new();
    [Reactive] public bool IsImportThumbnailVisible { get; set; }

    public InitialSetupViewModel InitialSetupVM { get; } = new();
    [Reactive] public bool IsInitialSetupVisible { get; set; }

    public MergeCategoryViewModel MergeCategoryVM { get; } = new();
    [Reactive] public bool IsMergeCategoryVisible { get; set; }

    public PdfViewerViewModel PdfViewerVM { get; } = new();
    [Reactive] public bool IsPdfViewerVisible { get; set; }

    public ProgressViewModel ProgressVM { get; } = new();
    [Reactive] public bool IsProgressVisible { get; set; }

    public ResolveTempAvatarViewModel ResolveTempAvatarVM { get; } = new();
    [Reactive] public bool IsResolveTempAvatarVisible { get; set; }

    public SettingsViewModel SettingsVM { get; } = new();
    [Reactive] public bool IsSettingsVisible { get; set; }

    public TextDialogViewModel TextDialogVM { get; } = new();
    [Reactive] public bool IsTextDialogVisible { get; set; }

    public UnitypackageViewerViewModel UnitypackageViewerVM { get; } = new();
    [Reactive] public bool IsUnitypackageViewerVisible { get; set; }

    public UpdateDialogViewModel UpdateDialogVM { get; } = new();
    [Reactive] public bool IsUpdateDialogVisible { get; set; }

    public YesNoDialogViewModel YesNoDialogVM { get; } = new();
    [Reactive] public bool IsYesNoDialogVisible { get; set; }

    public WindowNotificationManager NotificationManager { get; } = new();

    public MainWindowViewModel()
    {
        Instance = this;

        UpdateWindowTitle();
        Localizer.Instance.LanguageChanged += UpdateWindowTitle;
        AvatarExplorerApp.ArchivePasswordProvider = GetArchivePassword;

        SetBackgroundImage("D:\\VRChat\\VRChat Pictures\\2026-07\\VRChat_2026-07-13_01-23-26.027_3840x2160_wrld_b4eef105-5db1-4c1f-8800-a6e6de4a20e7.png", 100);
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

    public void ShowEditCommonAvatars()
    {
        IsEditCommonAvatarsVisible = true;
    }
    
    public void ShowEditImplementedAvatars()
    {
        SelectAvatarsVisible = true;
    }

    public async Task<string?> ShowEditMemoDialog(string memo)
    {
        EditMemoVM.Memo = memo;
        
        IsEditMemoVisible = true;
        var result = await EditMemoVM.WaitForResult();
        IsEditMemoVisible = false;

        return result;
    }

    public async Task<string[]?> ShowSelectAvatars(string[]? avatars = null, bool includeCommonAvatar = false, bool includeTempAvatar = true, bool allowCreateTempAvatar = false)
    {
        SelectAvatarsVM.Open(avatars, includeCommonAvatar, includeTempAvatar, allowCreateTempAvatar);

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

    public void SetBackgroundImage(string path, int opacity)
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
}
