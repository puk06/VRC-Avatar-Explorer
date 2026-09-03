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
        _thumbnailLoadCts?.Cancel();
        _thumbnailLoadCts?.Dispose();
        _thumbnailLoadCts = null;

        ThumbnailSource.Applied = ThumbnailSource.Primary;
        var defaultIcon = ImageService.Get(ThumbnailSource.Primary);
        if (defaultIcon == null && !string.IsNullOrEmpty(ThumbnailSource.Fallback))
        {
            defaultIcon = ImageService.Get(ThumbnailSource.Fallback);
            ThumbnailSource.Applied = ThumbnailSource.Fallback;
        }
        SetThumbnail(defaultIcon, owned: false);

        if (!string.IsNullOrEmpty(ThumbnailSource.FilePath))
        {
            var cts = new CancellationTokenSource();
            _thumbnailLoadCts = cts;
            _ = LoadThumbnailAsync(ThumbnailSource.FilePath, cts.Token);
        }

        Title = TitleLocalizable ? Localizer.Instance[TitleRaw] : TitleRaw;
        Description = DescriptionRaw.Args == null ? Localizer.Instance[DescriptionRaw.Key] : Localizer.Instance.Get(DescriptionRaw.Key, DescriptionRaw.Args);

        // 古い ContextMenu のイベントハンドラーを解放してから再生成する
        _contextMenuHolder?.Dispose();
        var holder = ContextMenuFactory.GetContextMenu(Actions, HandleMenuClick);
        _contextMenuHolder = holder;
        ContextMenu = holder.Menu;

        Width = Height = (Thumbnail != null) ? iconSize : 0;
        Tags.ForEach(i => i.Update());

        if (removeBrackets && (ViewModelType == ViewModelType.Item || ViewModelType == ViewModelType.Avatar))
        {
            Title = TextBracketsUtils.RemoveBrackets(TitleRaw);
        }

        if (ViewModelType == ViewModelType.CommonAvatar)
        {
            // Prefixを追加する (共通素体: XXX)
            Title = Localizer.Instance.Get(Loc.Button.Title.CommonAvatar, Title);
        }

        ToolTip = GenerateToolTipText();

        return this;
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

    private async Task LoadThumbnailAsync(string filePath, CancellationToken ct)
    {
        try
        {
            var bitmap = await Task.Run(() => ImageService.GetFromFileSystem(filePath), ct);

            if (bitmap == null) return;

            if (ct.IsCancellationRequested)
            {
                bitmap.Dispose();
                return;
            }

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (ct.IsCancellationRequested)
                {
                    bitmap.Dispose();
                    return;
                }
                SetThumbnail(bitmap, owned: true);
                ThumbnailSource.Applied = filePath;
            }, DispatcherPriority.Normal, ct);
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合は何もしない
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostError($"Failed to load thumbnail from {filePath}: {ex.Message}");
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
