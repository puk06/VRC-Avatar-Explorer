using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Utils;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class UnitypackageViewModel : ViewModelBase
{
    [Reactive] public partial string Name { get; set; } = string.Empty;
    [Reactive] public partial string ToolTipText { get; set; }

    public string ParentDirectory { get; set; } = string.Empty;

    public UnitypackageViewModel(string path)
    {
        Name = Path.GetFileName(path) ?? path;
        ParentDirectory = Directory.GetParent(path)?.Name ?? string.Empty;

        ToolTipText = ParentDirectory + " > " + Name;
    }
}

public partial class BulkImportItemViewModel : ViewModelBase
{
    [Reactive] public partial Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public partial string Title { get; private set; } = string.Empty;
    [Reactive] public partial string Description { get; private set; } = string.Empty;

    [Reactive] public partial double Width { get; set; } = 0;
    [Reactive] public partial double Height { get; set; } = 0;

    public ThumbnailSource ThumbnailSource { get; set; } = new();
    public string TitleRaw { get; set; } = string.Empty;
    public bool TitleLocalizable { get; } = false;

    public LocalizableField DescriptionRaw { get; set; } = new();

    [Reactive] public partial IEnumerable<UnitypackageViewModel> UnitypackageViewModels { get; private set; } = [];
    [Reactive] public partial int SelectedUnitypackage { get; set; } = 0;
    public string SelectedUnitypackagePath => UnitypackageFullPaths.IsValidIndex(SelectedUnitypackage) ? UnitypackageFullPaths[SelectedUnitypackage] : string.Empty;

    public string[] UnitypackageFullPaths { get; set; } = [];

    public string ItemId { get; set; } = string.Empty;

    private CancellationTokenSource? _thumbnailLoadCts;

    public BulkImportItemViewModel Update(int iconSize = 80, bool removeBrackets = false)
    {
        _thumbnailLoadCts?.Cancel();
        _thumbnailLoadCts?.Dispose();
        var cts = new CancellationTokenSource();
        _thumbnailLoadCts = cts;

        // UIスレッドではファイルI/Oを行わず、キャッシュ済みの画像のみ即時表示する
        Thumbnail = ImageService.Peek(ThumbnailSource.Primary);

        if (!string.IsNullOrEmpty(ThumbnailSource.Primary) && !ImageService.IsSystemIcon(ThumbnailSource.Primary))
        {
            _ = ApplyThumbnailAsync(ImageService.GetAsync(ThumbnailSource.Primary), iconSize, cts.Token);
        }

        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;

        Description = DescriptionRaw.Args == null ? Localizer.Instance[DescriptionRaw.Key] : Localizer.Instance.Get(DescriptionRaw.Key, DescriptionRaw.Args);

        Width = Height = (Thumbnail != null) ? iconSize : 0;

        if (removeBrackets)
        {
            Title = TextBracketsUtils.RemoveBrackets(TitleRaw);
        }

        var previousSelectedPackage = SelectedUnitypackage;
        UnitypackageViewModels = UnitypackageFullPaths.Select(path => new UnitypackageViewModel(path));

        if (!UnitypackageViewModels.Any())
            SelectedUnitypackage = -1;
        else if (previousSelectedPackage < 0 || previousSelectedPackage >= UnitypackageViewModels.Count())
            SelectedUnitypackage = 0;
        else
            SelectedUnitypackage = previousSelectedPackage;

        return this;
    }

    private async Task ApplyThumbnailAsync(Task<Bitmap?> loadTask, int iconSize, CancellationToken ct)
    {
        try
        {
            var bitmap = await loadTask.ConfigureAwait(false);
            if (bitmap == null || ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ct.IsCancellationRequested) return;
                Thumbnail = bitmap;
                Width = Height = iconSize;
            }, DispatcherPriority.Normal, ct);
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
    }

    public BulkImportItemViewModel Copy()
    {
        return new()
        {
            ThumbnailSource = ThumbnailSource,
            TitleRaw = TitleRaw,
            DescriptionRaw = DescriptionRaw,
            UnitypackageFullPaths = UnitypackageFullPaths,
            SelectedUnitypackage = SelectedUnitypackage,
            ItemId = ItemId
        };
    }
}
