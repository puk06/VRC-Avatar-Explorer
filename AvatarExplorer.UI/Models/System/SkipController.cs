namespace AvatarExplorer.UI.Models.System;

public sealed class SkipController
{
    private int _remaining;

    /// <summary>次の count 個の通知を無視する</summary>
    public void SkipNext(int count = 1) => _remaining = count;

    /// <summary>まだ消費されていない予約を破棄する</summary>
    public void Reset() => _remaining = 0;

    internal bool ShouldPass()
    {
        if (_remaining > 0)
        {
            _remaining--;
            return false;
        }
        return true;
    }
}
