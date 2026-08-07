using System.Diagnostics;
using System.Security.Principal;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Utils;
using Microsoft.Win32;

namespace AvatarExplorer.Core.Services.System;

#pragma warning disable CA1416 // プラットフォームの互換性を検証
public static class SchemeService
{
    public const string ProtocolVRCAE = "vrcae";
    public const string ProtocolBLM = "booth-library-manager";

    public static string? GetRegisteredCommand(string protocol)
    {
        try
        {
            if (!ProcessUtils.IsWindows()) return null;

            using var key = Registry.ClassesRoot.OpenSubKey($@"{protocol}\shell\open\command");
            return key?.GetValue(string.Empty) as string;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to read registered command for URL scheme '{protocol}'.", ex);
            return null;
        }
    }

    public static bool IsOwnSchemeRegistered(string protocol)
    {
        var registered = GetRegisteredCommand(protocol);
        if (string.IsNullOrEmpty(registered)) return false;

        var processPath = ProcessUtils.GetCurrentProcessPath();
        if (string.IsNullOrEmpty(processPath)) return false;

        return registered.Contains(processPath, StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsAnySchemeRegistered(string protocol)
    {
        try
        {
            if (!ProcessUtils.IsWindows()) return false;

            using var key = Registry.ClassesRoot.OpenSubKey($@"{protocol}\shell\open\command");
            return key != null;
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to determine whether URL scheme '{protocol}' is registered.", ex);
            return false;
        }
    }

    public static bool HasBackup(string protocol)
    {
        try
        {
            var backupPath = GetBackupPath(protocol);
            return File.Exists(backupPath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to check backup for URL scheme '{protocol}'.", ex);
            return false;
        }
    }

    public static void RegisterScheme(string protocol)
    {
        try
        {
            if (!IsRunAsAdmin()) return;

            var processPath = ProcessUtils.GetCurrentProcessPath();
            if (string.IsNullOrEmpty(processPath)) return;

            var currentCommand = GetRegisteredCommand(protocol);
            if (!string.IsNullOrEmpty(currentCommand) && !currentCommand.Contains(processPath, StringComparison.OrdinalIgnoreCase))
            {
                SaveBackup(protocol, currentCommand);
            }

            RegisterCustomScheme(protocol, processPath);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to register URL scheme '{protocol}'.", ex);
        }
    }

    public static void UnregisterScheme(string protocol)
    {
        try
        {
            if (!IsRunAsAdmin()) return;

            var backupPath = GetBackupPath(protocol);
            if (File.Exists(backupPath))
            {
                var originalCommand = File.ReadAllText(backupPath).Trim();
                if (!string.IsNullOrEmpty(originalCommand))
                {
                    RestoreCustomScheme(protocol, originalCommand);
                    File.Delete(backupPath);
                    return;
                }
            }

            using var key = Registry.ClassesRoot.OpenSubKey(protocol, true);
            key?.DeleteSubKeyTree("shell", false);
            Registry.ClassesRoot.DeleteSubKeyTree(protocol, false);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to unregister URL scheme '{protocol}'.", ex);
        }
    }

    public static bool IsRunAsAdmin()
    {
        if (!ProcessUtils.IsWindows()) return false;

        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RestartAsAdmin()
    {
        try
        {
            var processPath = ProcessUtils.GetCurrentProcessPath();
            if (string.IsNullOrEmpty(processPath)) return;

            var processStartInfo = new ProcessStartInfo()
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

    private static string GetBackupPath(string protocol) => Path.Join(SystemPath.SchemeBackupFolderPath, $"{protocol}.backup");

    private static void SaveBackup(string protocol, string command)
    {
        try
        {
            Directory.CreateDirectory(SystemPath.SchemeBackupFolderPath);
            File.WriteAllText(GetBackupPath(protocol), command);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to save backup for URL scheme '{protocol}'.", ex);
        }
    }

    private static void RegisterCustomScheme(string protocol, string processPath)
    {
        try
        {
            if (!ProcessUtils.IsWindows()) return;

            using (var key = Registry.ClassesRoot.CreateSubKey(protocol))
            {
                key.SetValue(string.Empty, "URL:" + protocol + " Protocol");
                key.SetValue("URL Protocol", string.Empty);
            }

            var commandKey = $@"{protocol}\shell\open\command";
            using (var key = Registry.ClassesRoot.CreateSubKey(commandKey))
            {
                key.SetValue(string.Empty, $"\"{processPath}\" \"%1\"");
            }
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to register URL scheme '{protocol}' with command '{processPath}'.", ex);
        }
    }

    private static void RestoreCustomScheme(string protocol, string command)
    {
        try
        {
            if (!ProcessUtils.IsWindows()) return;

            var commandKey = $@"{protocol}\shell\open\command";
            using var key = Registry.ClassesRoot.CreateSubKey(commandKey);
            key.SetValue(string.Empty, command);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to restore URL scheme '{protocol}' with command '{command}'.", ex);
        }
    }
}
#pragma warning restore CA1416 // プラットフォームの互換性を検証
