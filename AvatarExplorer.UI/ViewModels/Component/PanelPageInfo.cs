using Avalonia;
using AvatarExplorer.Core.Localization;
using AvatarExplorer.UI.Localization;
using AvatarExplorer.UI.Utils;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace AvatarExplorer.UI.ViewModels.Component;

public partial class PanelPageInfo : ViewModelBase
{
    [Reactive] public partial int CurrentPage { get; set; }
    [Reactive] public partial int TotalItems { get; set; }
    [Reactive] public partial int PageSize { get; set; } = 30;
    [Reactive] public partial int TotalPages { get; private set; } = 1;
    [Reactive] public partial string PageDisplay { get; private set; } = "1 / 1";
    [Reactive] public partial string ItemRangeDisplay { get; private set; } = "1 - 0 / 0";
    [Reactive] public partial bool CanGoFirst { get; private set; }
    [Reactive] public partial bool CanGoPrev { get; private set; }
    [Reactive] public partial bool CanGoNext { get; private set; }
    [Reactive] public partial bool CanGoLast { get; private set; }
    [Reactive] public partial Vector ScrollOffset { get; set; } = AvaloniaVectorUtils.MinValue;

    private Vector _restoreScrollOffset = AvaloniaVectorUtils.MinValue;
    public Vector RestoreScrollOffset
    {
        get => _restoreScrollOffset;
        set
        {
            _restoreScrollOffset = value;
            this.RaisePropertyChanged(nameof(RestoreScrollOffset));
        }
    }

    public PanelPageInfo()
    {
        Localizer.Instance.LanguageChanged += UpdateDerivedProperties;

        this.WhenAnyValue(x => x.CurrentPage, x => x.TotalItems, x => x.PageSize)
            .Subscribe(_ => UpdateDerivedProperties());
    }

    public void SetPage(int page)
    {
        var clamped = Math.Clamp(page, 0, Math.Max(0, TotalPages - 1));
        if (CurrentPage != clamped)
        {
            CurrentPage = clamped;
            RestoreScrollOffset = AvaloniaVectorUtils.MinValue;
        }
    }

    public void GoFirst() => SetPage(0);
    public void GoPrev() => SetPage(CurrentPage - 1);
    public void GoNext() => SetPage(CurrentPage + 1);
    public void GoLast() => SetPage(TotalPages - 1);

    public void Reset()
    {
        CurrentPage = 0;
        ScrollOffset = AvaloniaVectorUtils.MinValue;
        RestoreScrollOffset = AvaloniaVectorUtils.MinValue;
    }

    public IEnumerable<T> GetPageItems<T>(IReadOnlyList<T> items) =>
        items.Skip(CurrentPage * PageSize).Take(PageSize);

    private void UpdateDerivedProperties()
    {
        TotalPages = Math.Max(1, (int)Math.Ceiling((double)TotalItems / Math.Max(1, PageSize)));
        if (CurrentPage >= TotalPages && TotalPages > 0)
        {
            CurrentPage = TotalPages - 1;
            RestoreScrollOffset = AvaloniaVectorUtils.MinValue;
        }

        PageDisplay = Localizer.Instance.Get(Loc.ItemWindow.CurrentPage, [(CurrentPage + 1).ToString(), TotalPages.ToString()]);
        var start = (CurrentPage * PageSize) + 1;
        var end = Math.Min((CurrentPage + 1) * PageSize, TotalItems);
        ItemRangeDisplay = Localizer.Instance.Get(Loc.ItemWindow.PageItemCount, [start.ToString(), end.ToString(), TotalItems.ToString()]);

        CanGoFirst = CurrentPage > 0;
        CanGoPrev = CurrentPage > 0;
        CanGoNext = CurrentPage < TotalPages - 1;
        CanGoLast = CurrentPage < TotalPages - 1;
    }
}
