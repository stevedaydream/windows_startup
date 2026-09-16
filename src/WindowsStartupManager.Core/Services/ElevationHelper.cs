using System.Diagnostics;
using System.Security.Principal;

namespace WindowsStartupManager.Core.Services;

public static class ElevationHelper
{
    public static bool IsElevated()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public static void RelaunchElevated(string? arguments = null)
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName
            ?? throw new InvalidOperationException("Could not determine the current executable path.");

        var startInfo = new ProcessStartInfo(exePath)
        {
            UseShellExecute = true,
            Verb = "runas"
        };

        if (!string.IsNullOrEmpty(arguments))
        {
            startInfo.Arguments = arguments;
        }

        Process.Start(startInfo);
    }
}