namespace AvatarExplorer.Core.Utils;

/// <summary>
/// 時間単位の変換を行うユーティリティを提供します。
/// </summary>
public static class TimeUtils
{
    /// <summary>
    /// 分をミリ秒に変換します。
    /// </summary>
    /// <param name="minutes">変換する分数。</param>
    /// <returns>対応するミリ秒数。</returns>
    public static int MinToMs(int minutes) => minutes * 60 * 1000;
}
