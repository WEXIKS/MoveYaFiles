using System;
using Microsoft.Win32;

namespace MoveYaFiles.Services;

public static class StartupManager
{
    private const string AppName = "MoveYaFiles";

    public static void SetAutoStart(bool enable)
    {
        if (!OperatingSystem.IsWindows()) return;

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);
            if (key != null)
            {
                string? appPath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (enable && !string.IsNullOrEmpty(appPath))
                {
                    key.SetValue(AppName, $"\"{appPath}\"");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                }
            }
        }
        catch
        {
            // Ignorowanie błędów uprawnień
        }
    }
}