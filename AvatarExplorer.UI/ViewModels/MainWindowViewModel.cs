using System;
using System.Diagnostics;
using System.Threading.Tasks;
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

    public AddItemViewModel AddItemVM { get; } = new();
    [Reactive] public bool IsAddItemVisible { get; set; }
    
    public ArchivePasswordDialogViewModel ArchivePasswordDialogVM { get; } = new();
    [Reactive] public bool IsArchivePasswordDialogVisible { get; set; }

    public EditCommonAvatarsViewModel EditCommonAvatarsVM { get; } = new();
    [Reactive] public bool IsEditCommonAvatarsVisible { get; set; }

    public EditImplementedAvatarsViewModel EditImplementedAvatarsVM { get; } = new();
    [Reactive] public bool IsEditImplementedAvatarsVisible { get; set; }

    public EditMemoViewModel EditMemoVM { get; } = new();
    [Reactive] public bool IsEditMemoVisible { get; set; }

    public EditSupportedAvatarsViewModel EditSupportedAvatarsVM { get; } = new();
    [Reactive] public bool IsEditSupportedAvatarsVisible { get; set; }

    public EditTagsViewModel EditTagsVM { get; } = new();
    [Reactive] public bool IsEditTagVisible { get; set; }

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

    public MainWindowViewModel()
    {
        Instance = this;

        UpdateWindowTitle();
        Localizer.Instance.LanguageChanged += UpdateWindowTitle;
        AvatarExplorerApp.ArchivePasswordProvider = GetArchivePassword;

        SetBackgroundImage("D:\\VRChat\\VRChat Pictures\\2026-06\\VRChat_2026-06-23_23-59-53.110_3840x2160_wrld_e7701aa4-377e-4e1e-a1ba-38784032128f.png", 100);
    }

    private void UpdateWindowTitle()
    {
        var title = string.Format("VRC Avatar Explorer v{0}", AvatarExplorerApp.CurrentVersion);

        if (ProcessUtils.IsWindows() && SchemeService.IsRunAsAdmin())
            title += string.Format(" - [{0}]", Localizer.Instance[LocalizationKey.Title.AdministratorMode]);

        WindowTitle = title;
    }

    public void ShowEditCommonAvatars()
    {
        IsEditCommonAvatarsVisible = true;
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
}
