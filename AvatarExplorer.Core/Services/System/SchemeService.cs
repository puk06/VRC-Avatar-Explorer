using System.Diagnostics;
using System.Security.Principal;
using AvatarExplorer.Core.Data.Paths;
using AvatarExplorer.Core.Utils;
using Microsoft.Win32;

namespace AvatarExplorer.Core.Services.System;

#pragma warning disable CA1416
public static class SchemeService
{
    public const string ProtocolVRCAE = "vrcae";
    public const string ProtocolBLM = "booth-library-manager";

    private const string AppName = "VRC Avatar Explorer";
    private const string AppNameShort = "vrc-avatar-explorer";

    public static string? GetRegisteredCommand(string protocol)
    {
        try
        {
            if (ProcessUtils.IsWindows())
            {
                using var key = Registry.ClassesRoot.OpenSubKey($@"{protocol}\shell\open\command");
                return key?.GetValue(string.Empty) as string;
            }

            if (ProcessUtils.IsLinux())
            {
                var desktopPath = GetLinuxDesktopFilePath(protocol);
                if (!File.Exists(desktopPath)) return null;

                foreach (var line in File.ReadAllLines(desktopPath))
                {
                    if (line.StartsWith("Exec=", StringComparison.Ordinal))
                        return line[5..].Trim();
                }
                return null;
            }

            return null;
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
            if (ProcessUtils.IsWindows())
            {
                using var key = Registry.ClassesRoot.OpenSubKey($@"{protocol}\shell\open\command");
                return key != null;
            }

            if (ProcessUtils.IsLinux())
            {
                var desktopPath = GetLinuxDesktopFilePath(protocol);
                if (File.Exists(desktopPath)) return true;

                var localDir = GetLinuxApplicationsDirectory();
                if (Directory.Exists(localDir))
                {
                    foreach (var file in Directory.GetFiles(localDir, "*.desktop"))
                    {
                        if (ContainsMimeType(file, protocol)) return true;
                    }
                }

                var systemDirs = new[]
                {
                    "/usr/share/applications",
                    "/usr/local/share/applications"
                };
                foreach (var dir in systemDirs)
                {
                    if (!Directory.Exists(dir)) continue;
                    foreach (var file in Directory.GetFiles(dir, "*.desktop"))
                    {
                        if (ContainsMimeType(file, protocol)) return true;
                    }
                }

                return false;
            }

            return false;
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
            var processPath = ProcessUtils.GetCurrentProcessPath();
            if (string.IsNullOrEmpty(processPath)) return;

            if (ProcessUtils.IsWindows())
            {
                if (!IsRunAsAdmin()) return;

                var currentCommand = GetRegisteredCommand(protocol);
                if (!string.IsNullOrEmpty(currentCommand) && !currentCommand.Contains(processPath, StringComparison.OrdinalIgnoreCase))
                {
                    SaveBackup(protocol, currentCommand);
                }

                RegisterWindowsScheme(protocol, processPath);
            }
            else if (ProcessUtils.IsLinux())
            {
                var desktopPath = GetLinuxDesktopFilePath(protocol);
                if (File.Exists(desktopPath))
                {
                    var existingCommand = GetRegisteredCommand(protocol);
                    if (!string.IsNullOrEmpty(existingCommand) && !existingCommand.Contains(processPath, StringComparison.OrdinalIgnoreCase))
                    {
                        SaveBackup(protocol, existingCommand);
                    }
                }

                RegisterLinuxScheme(protocol, processPath);
            }
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
            if (ProcessUtils.IsWindows())
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
            else if (ProcessUtils.IsLinux())
            {
                var desktopPath = GetLinuxDesktopFilePath(protocol);
                if (File.Exists(desktopPath))
                    File.Delete(desktopPath);

                RunUpdateDesktopDatabase();
            }
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

    private static string GetLinuxApplicationsDirectory()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Join(home, ".local", "share", "applications");
    }

    private static string GetLinuxDesktopFilePath(string protocol)
    {
        return Path.Join(GetLinuxApplicationsDirectory(), $"{AppNameShort}-{protocol}.desktop");
    }

    private static bool ContainsMimeType(string desktopFilePath, string protocol)
    {
        try
        {
            var mimeType = $"x-scheme-handler/{protocol}";
            foreach (var line in File.ReadAllLines(desktopFilePath))
            {
                if (line.StartsWith("MimeType=", StringComparison.Ordinal) &&
                    line.Contains(mimeType, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static void RunUpdateDesktopDatabase()
    {
        try
        {
            var appsDir = GetLinuxApplicationsDirectory();
            var psi = new ProcessStartInfo("update-desktop-database", appsDir)
            {
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var process = Process.Start(psi);
            process?.WaitForExit(5000);
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError("Failed to run update-desktop-database.", ex);
        }
    }

    private static void RegisterWindowsScheme(string protocol, string processPath)
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

    private static void RegisterLinuxScheme(string protocol, string processPath)
    {
        try
        {
            if (!ProcessUtils.IsLinux()) return;

            var appsDir = GetLinuxApplicationsDirectory();
            Directory.CreateDirectory(appsDir);

            var desktopPath = GetLinuxDesktopFilePath(protocol);
            var content = $"""
                [Desktop Entry]
                Type=Application
                Name={AppName} ({protocol})
                Comment={AppName} URI Handler
                Exec={processPath} %u
                Terminal=false
                MimeType=x-scheme-handler/{protocol};
                """;

            File.WriteAllText(desktopPath, content);
            RunUpdateDesktopDatabase();
        }
        catch (Exception ex)
        {
            ErrorManager.Instance.PostInternalError($"Failed to register URL scheme '{protocol}' on Linux.", ex);
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
#pragma warning restore CA1416
