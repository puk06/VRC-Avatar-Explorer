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

    public void SaveRightState(PanelPageInfo rightPageInfo)
    {
        var currentGuid = _navigationService.CurrentStateId;
        if (currentGuid != null)
        {
            _pageCache.Add(currentGuid.Value, rightPageInfo.CurrentPage);
            _scrollValueCache.Add(currentGuid.Value, rightPageInfo.ScrollOffset);
        }
    }

    public void RestoreRightState(PanelPageInfo rightPageInfo)
    {
        var currentGuid = _navigationService.CurrentStateId;
        if (currentGuid != null && _pageCache.TryGetValue(currentGuid.Value, out var page))
        {
            rightPageInfo.CurrentPage = page;
            var scroll = _scrollValueCache.Get(currentGuid.Value);
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
