using System;
using Avalonia;
using AvatarExplorer.Core.Services.Items;
using AvatarExplorer.Core.Services.System;
using AvatarExplorer.UI.Utils;
using AvatarExplorer.UI.ViewModels.Component;

namespace AvatarExplorer.UI.ViewModels.Managers;

public class StateCacheManager
{
    private readonly CacheManager<Guid, int> _pageCache = new(0);
    private readonly CacheManager<Guid, Vector> _scrollValueCache = new(AvaloniaVectorUtils.MaxValue);
    private readonly CacheManager<int, (int, Vector)> _leftStateCache = new((0, AvaloniaVectorUtils.MaxValue));

    private readonly ItemNavigationService _navigationService;

    public StateCacheManager(ItemNavigationService navigationService)
    {
        _navigationService = navigationService;
    }

    public void SaveLeftState(int categoryIndex, PanelPageInfo leftPageInfo)
    {
        _leftStateCache.Add(categoryIndex, (leftPageInfo.CurrentPage, leftPageInfo.ScrollOffset));
    }

    public void RestoreLeftState(int categoryIndex, PanelPageInfo leftPageInfo)
    {
        if (_leftStateCache.TryGetValue(categoryIndex, out var state))
        {
            leftPageInfo.CurrentPage = state.Item1;
            leftPageInfo.ScrollOffset = state.Item2;
            leftPageInfo.RestoreScrollOffset = AvaloniaVectorUtils.MaxValue;
            leftPageInfo.RestoreScrollOffset = state.Item2;
        }
        else
        {
            leftPageInfo.Reset();
        }
    }

    public void SaveRightState(PanelPageInfo rightPageInfo, Guid? customGuid = null)
    {
        var currentGuid = customGuid ?? _navigationService.CurrentStateId ?? Guid.Empty;

        _pageCache.Add(currentGuid, rightPageInfo.CurrentPage);
        _scrollValueCache.Add(currentGuid, rightPageInfo.ScrollOffset);
    }

    /// <summary>
    /// キャッシュに未登録の場合のみ保存する。既に登録されている場合は何もしない。
    /// 検索前の画面状態を保存する際に、テキスト変更で何度も呼ばれても初回だけ保存したい場合に使う。
    /// </summary>
    public bool SaveRightStateIfAbsent(PanelPageInfo rightPageInfo, Guid? customGuid = null)
    {
        var currentGuid = customGuid ?? _navigationService.CurrentStateId ?? Guid.Empty;
        if (_pageCache.ContainsKey(currentGuid)) return false;

        _pageCache.Add(currentGuid, rightPageInfo.CurrentPage);
        _scrollValueCache.Add(currentGuid, rightPageInfo.ScrollOffset);
        return true;
    }

    public void RestoreRightState(PanelPageInfo rightPageInfo, Guid? customGuid = null)
    {
        var currentGuid = customGuid ?? _navigationService.CurrentStateId ?? Guid.Empty;
        if (_pageCache.TryGetValue(currentGuid, out var page))
        {
            rightPageInfo.CurrentPage = page;
            var scroll = _scrollValueCache.Get(currentGuid);
            rightPageInfo.ScrollOffset = scroll;
            rightPageInfo.RestoreScrollOffset = AvaloniaVectorUtils.MaxValue;
            rightPageInfo.RestoreScrollOffset = scroll;
        }
        else
        {
            rightPageInfo.Reset();
        }
    }
}
