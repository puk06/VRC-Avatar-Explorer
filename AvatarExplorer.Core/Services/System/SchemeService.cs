using System.Diagnostics;
using System.Security.Principal;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Services.IO;
using AvatarExplorer.Core.Utils;
using Microsoft.Win32;

namespace AvatarExplorer.Core.Services.System;

#pragma warning disable CA1416 // プラットフォームの互換性を検証
public static class SchemeService
{
    private static readonly string REG_PROTCOL = "VRCAE";
    private static readonly string SKIPPED_TEXT = "<sys>SKIPPED";

    public static string? GetInternalSchemePath()
    {
        try
        {
            if (ProcessUtils.GetCurrentProcessPath() == null) return null;

            if (!File.Exists(SystemPath.SchemeFilePath)) return null;
            else return File.ReadAllText(SystemPath.SchemeFilePath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to read scheme file at '{SystemPath.SchemeFilePath}'.", ex);
            return null;
        }
    }

    public static bool IsSkipped(string text) => text == SKIPPED_TEXT;
    public static bool IsSchemeRegistered() => IsSchemeRegistered(REG_PROTCOL);

    public static void RegisterScheme()
    {
        try
        {
            if (!IsRunAsAdmin()) return;

            string? processPath = ProcessUtils.GetCurrentProcessPath();
            if (string.IsNullOrEmpty(processPath)) return;

            RegisterCustomScheme(REG_PROTCOL, processPath);

            FileSystemService.PrepareFileDirectory(SystemPath.SchemeFilePath);
            File.WriteAllText(SystemPath.SchemeFilePath, processPath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to register URL scheme '{REG_PROTCOL}' or write scheme file at '{SystemPath.SchemeFilePath}'.", ex);
        }
    }
    public static void MarkSchemeSkipped()
    {
        try
        {
            FileSystemService.PrepareFileDirectory(SystemPath.SchemeFilePath);
            File.WriteAllText(SystemPath.SchemeFilePath, SKIPPED_TEXT);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to write scheme file at '{SystemPath.SchemeFilePath}' when marking scheme as skipped.", ex);
        }
    }

    public static bool IsRunAsAdmin()
    {
        if (!ProcessUtils.IsWindows()) return false;

        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RestartAsAdmin()
    {
        try
        {
            string? processPath = ProcessUtils.GetCurrentProcessPath();
            if (string.IsNullOrEmpty(processPath)) return;

            ProcessStartInfo processStartInfo = new()
            {
                FileName = processPath,
                UseShellExecute = true,
                Verb = "runas"
            };

            Process.Start(processStartInfo);
            Environment.Exit(0);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to restart application as administrator.", ex);
        }
    }

    private static void RegisterCustomScheme(string protocol, string processPath)
    {
        try
        {
            if (!ProcessUtils.IsWindows()) return;

            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(protocol))
            {
                key.SetValue(string.Empty, "URL:" + protocol + " Protocol");
                key.SetValue("URL Protocol", string.Empty);
            }

            string commandKey = $@"{protocol}\shell\open\command";
            using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(commandKey))
            {
                key.SetValue(string.Empty, $"\"{processPath}\" \"%1\"");
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to register URL scheme '{protocol}' with command '{processPath}'.", ex);
        }
    }
    private static bool IsSchemeRegistered(string protocol)
    {
        try
        {
            if (!ProcessUtils.IsWindows()) return false;

            using RegistryKey? key = Registry.ClassesRoot.OpenSubKey(protocol);
            return key != null;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to determine whether URL scheme '{protocol}' is registered.", ex);
            return false;
        }
    }
}
#pragma warning restore CA1416 // プラットフォームの互換性を検証
