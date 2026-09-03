using System.Text;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using AvatarExplorer.Core.Extensions;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.Core.Utils;
using AvatarExplorer.UI.Factories;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Models.ContextMenu;
using AvatarExplorer.UI.Models.Items;
using AvatarExplorer.UI.Services.ContextMenu;
using AvatarExplorer.UI.Services.Utilities;
using AvatarExplorer.UI.Services.ViewControl;
using AvatarExplorer.UI.Utils;
using ReactiveUI.Fody.Helpers;

namespace AvatarExplorer.UI.ViewModels.Component;

public class ItemViewModel : ViewModelBase, IDisposable
{
    [Reactive] public bool IsVisible { get; set; } = true;
    [Reactive] public bool IsSelected { get; set; } = false;
    [Reactive] public bool IsImplemented { get; set; } = false;
    [Reactive] public bool IsNotImplemented { get; set; } = false;

    [Reactive] public Bitmap? Thumbnail { get; set; } = null;
    [Reactive] public string Title { get; private set; } = string.Empty;
    [Reactive] public string Description { get; private set; } = string.Empty;
    [Reactive] public TagViewModel[] Tags { get; set; } = [];
    [Reactive] public ContextMenu? ContextMenu { get; set; } = null;
    [Reactive] public string? ToolTip { get; set; } = null;

    [Reactive] public double Width { get; set; } = 0;
    [Reactive] public double Height { get; set; } = 0;

    public ThumbnailSource ThumbnailSource { get; set; } = new();
    public string TitleRaw { get; set; } = string.Empty;
    public bool TitleLocalizable { get; set; } = false;

    public LocalizableField DescriptionRaw { get; set; } = new();

    public string CreatedDate { get; set; } = string.Empty;
    public string UpdatedDate { get; set; } = string.Empty;
    public string ItemMemo { get; set; } = string.Empty;

    public ContextMenuAction[] Actions { get; set; } = [];

    private CancellationTokenSource? _thumbnailLoadCts;
    private Bitmap? _ownedThumbnail;
    private ContextMenuHolder? _contextMenuHolder;
    private bool _disposed;

    public string Identifier { get; set; } = string.Empty;

    // AvatarならItemのIdentifier、CommonAvatarならCommonAvatarのIdentifier、TempAvatarならTempAvatarのIdentifier、FolderならFolderのPath、FileならFileのPath
    public string? ActualValue { get; set; }
    public required ViewModelType ViewModelType { get; set; }

    public ItemViewModel Update(int iconSize = 80, bool removeBrackets = false)
    {
        UpdateThumbnail(iconSize);
        UpdateTexts(removeBrackets);
        UpdateContextMenu();
        UpdateSize(iconSize);
        UpdateTags();
        UpdateToolTip();

        return this;
    }

    private void UpdateThumbnail(int iconSize)
    {
        var cts = ResetThumbnailLoading();

        // UIスレッドではファイルI/Oを行わず、キャッシュ済み (またはシステムアイコン) の画像のみ即時表示する
        ThumbnailSource.Applied = ThumbnailSource.Primary;
        SetThumbnail(ResolveImmediateThumbnail(), owned: false);

        // 実ファイルのサムネイルはバックグラウンドで読み込んで後から差し替える (鮮度チェックも兼ねる)
        if (!string.IsNullOrEmpty(ThumbnailSource.Primary) && !ImageService.IsSystemIcon(ThumbnailSource.Primary))
        {
            _ = ApplyThumbnailAsync(ImageService.GetAsync(ThumbnailSource.Primary), ThumbnailSource.Primary, iconSize, owned: false, cts.Token);
        }

        if (!string.IsNullOrEmpty(ThumbnailSource.FilePath))
        {
            _ = ApplyThumbnailAsync(GetFromFileAsync(ThumbnailSource.FilePath, cts.Token), ThumbnailSource.FilePath, iconSize, owned: true, cts.Token);
        }
    }

    private Bitmap? ResolveImmediateThumbnail()
    {
        var icon = ImageService.Peek(ThumbnailSource.Primary);
        if (icon != null || string.IsNullOrEmpty(ThumbnailSource.Fallback)) return icon;

        ThumbnailSource.Applied = ThumbnailSource.Fallback;
        return ImageService.Peek(ThumbnailSource.Fallback);
    }

    private CancellationTokenSource ResetThumbnailLoading()
    {
        _thumbnailLoadCts?.Cancel();
        _thumbnailLoadCts?.Dispose();
        var cts = new CancellationTokenSource();
        _thumbnailLoadCts = cts;
        return cts;
    }

    private void UpdateTexts(bool removeBrackets)
    {
        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;
        Description = DescriptionRaw.Args == null ? Localizer.Instance[DescriptionRaw.Key] : Localizer.Instance.Get(DescriptionRaw.Key, DescriptionRaw.Args);

        if (removeBrackets && (ViewModelType == ViewModelType.Item || ViewModelType == ViewModelType.Avatar))
        {
            Title = TextBracketsUtils.RemoveBrackets(TitleRaw);
        }

        if (ViewModelType == ViewModelType.CommonAvatar)
        {
            // Prefixを追加する (共通素体: XXX)
            Title = Localizer.Instance.Get(Loc.Button.Title.CommonAvatar, Title);
        }
    }

    private void UpdateContextMenu()
    {
        // 古い ContextMenu のイベントハンドラーを解放してから再生成する
        _contextMenuHolder?.Dispose();
        _contextMenuHolder = ContextMenuFactory.GetContextMenu(Actions, HandleMenuClick);
        ContextMenu = _contextMenuHolder.Menu;
    }

    private void UpdateSize(int iconSize)
    {
        Width = Height = (Thumbnail != null) ? iconSize : 0;
    }

    private void UpdateTags()
    {
        Tags.ForEach(i => i.Update());
    }

    private void UpdateToolTip()
    {
        ToolTip = GenerateToolTipText();
    }

    private string? GenerateToolTipText()
    {
        if (ViewModelType == ViewModelType.Item || ViewModelType == ViewModelType.Avatar)
        {
            return GenerateToolTipFromItem();
        }

        if (ViewModelType == ViewModelType.TempAvatar)
        {
            return TitleRaw;
        }

        if (ViewModelType == ViewModelType.File)
        {
            return Localizer.Instance.Get(Loc.Button.ToolTip.FilePath, ActualValue ?? string.Empty);
        }

        return null;
    }
    private string GenerateToolTipFromItem()
    {
        var blocks = new List<List<string>>();

            var avatarTagLines = new List<string>();
            var commonAvatarTag = Tags.FirstOrDefault(t => t.IsCommonAvatar);
            var otherTags = Tags.Where(t => !t.IsCommonAvatar).ToArray();

            if (commonAvatarTag != null)
                avatarTagLines.Add(Localizer.Instance.Get(Loc.Button.ToolTip.CommonAvatar, commonAvatarTag.ValueRaw));

            if (otherTags.Length > 0)
                avatarTagLines.Add(Localizer.Instance.Get(Loc.Button.ToolTip.Tag, string.Join(", ", otherTags.Select(t => t.ValueRaw))));

            if (avatarTagLines.Count > 0)
                blocks.Add(avatarTagLines);

            var dateLines = new List<string>();

            if (!string.IsNullOrEmpty(UpdatedDate))
                dateLines.Add(Localizer.Instance.Get(Loc.Button.ToolTip.UpdatedDate, DatetimeUtils.GetDateStringFromUnixTime(UpdatedDate)));

            if (!string.IsNullOrEmpty(CreatedDate))
                dateLines.Add(Localizer.Instance.Get(Loc.Button.ToolTip.CreatedDate, DatetimeUtils.GetDateStringFromUnixTime(CreatedDate)));

            if (dateLines.Count > 0)
                blocks.Add(dateLines);

            if (!string.IsNullOrEmpty(ItemMemo))
                blocks.Add([ItemMemo]);

            var sb = new StringBuilder();
            sb.Append(TitleRaw);

            foreach (var block in blocks)
            {
                sb.AppendLine();
                sb.AppendLine();
                sb.AppendJoin(Environment.NewLine, block);
            }

            return sb.ToString();
    }

    private void HandleMenuClick(ContextMenuAction action)
    {
        ContextMenuHandlerService.Handle(action.ActionKey, ActualValue ?? Identifier);
    }

    private static Task<Bitmap?> GetFromFileAsync(string filePath, CancellationToken ct)
    {
        return Task.Run(() => ImageService.GetFromFileSystem(filePath), ct);
    }

    private async Task ApplyThumbnailAsync(Task<Bitmap?> loadTask, string appliedSource, int iconSize, bool owned, CancellationToken ct)
    {
        try
        {
            var bitmap = await loadTask.ConfigureAwait(false);

            if (bitmap == null || ct.IsCancellationRequested)
            {
                if (owned) bitmap?.Dispose();
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ct.IsCancellationRequested)
                {
                    if (owned) bitmap.Dispose();
                    return;
                }
                SetThumbnail(bitmap, owned);
                ThumbnailSource.Applied = appliedSource;
                Width = Height = iconSize;
            }, DispatcherPriority.Normal, ct);
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError($"Failed to load thumbnail from {appliedSource}: {ex.Message}");
        }
    }

    private void SetThumbnail(Bitmap? value, bool owned)
    {
        if (_ownedThumbnail != null && !ReferenceEquals(_ownedThumbnail, value))
        {
            _ownedThumbnail.Dispose();
        }
        _ownedThumbnail = owned ? value : null;
        Thumbnail = value;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        _disposed = true;

        if (disposing)
        {
            _thumbnailLoadCts?.Cancel();
            _thumbnailLoadCts?.Dispose();
            _thumbnailLoadCts = null;
            _ownedThumbnail?.Dispose();
            _ownedThumbnail = null;
            _contextMenuHolder?.Dispose();
            _contextMenuHolder = null;
            ContextMenu = null;
        }
    }
}
