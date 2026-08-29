using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AvatarExplorer.Core.Utils;

/// <summary>
/// 現在の実行プラットフォームの判定や、現在のプロセス情報の取得を行うユーティリティを提供します。
/// </summary>
public static class ProcessUtils
{
    /// <summary>現在の OS が Windows かどうかを判定します。</summary>
    /// <returns>Windows の場合は true。</returns>
    public static bool IsWindows() => CheckPlatForm(OSPlatform.Windows);
    /// <summary>現在の OS が Linux かどうかを判定します。</summary>
    /// <returns>Linux の場合は true。</returns>
    public static bool IsLinux() => CheckPlatForm(OSPlatform.Linux);
    /// <summary>現在の OS が macOS かどうかを判定します。</summary>
    /// <returns>macOS の場合は true。</returns>
    public static bool IsOSX() => CheckPlatForm(OSPlatform.OSX);
    /// <summary>現在の OS が FreeBSD かどうかを判定します。</summary>
    /// <returns>FreeBSD の場合は true。</returns>
    public static bool IsFreeBSD() => CheckPlatForm(OSPlatform.FreeBSD);
    private static bool CheckPlatForm(OSPlatform platform) => RuntimeInformation.IsOSPlatform(platform);

    /// <summary>
    /// 現在のプロセスの実行可能ファイルのフルパスを取得します。
    /// </summary>
    /// <returns>現在のプロセスのファイル名（フルパス）。取得できない場合は null。</returns>
    public static string? GetCurrentProcessPath() => Process.GetCurrentProcess()?.MainModule?.FileName;
}
