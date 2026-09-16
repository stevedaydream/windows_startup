using System.Reflection;
using System.Runtime.InteropServices;

namespace WindowsStartupManager.Core.Interop;

/// <summary>
/// Reads and writes .lnk shortcut files via the WScript.Shell COM automation object,
/// invoked through late-bound reflection so no COM interop assembly reference or
/// additional NuGet package is required.
/// </summary>
public static class ShellLinkService
{
    public static (string TargetPath, string Arguments) ReadShortcut(string shortcutPath)
    {
        var shellType = GetShellType();
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType)!;
            shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath })!;
            var shortcutType = shortcut.GetType();
            var targetPath = shortcutType.InvokeMember("TargetPath", BindingFlags.GetProperty, null, shortcut, null) as string ?? string.Empty;
            var arguments = shortcutType.InvokeMember("Arguments", BindingFlags.GetProperty, null, shortcut, null) as string ?? string.Empty;
            return (targetPath, arguments);
        }
        finally
        {
            ReleaseIfComObject(shortcut);
            ReleaseIfComObject(shell);
        }
    }

    public static void CreateShortcut(string shortcutPath, string targetPath, string arguments)
    {
        var shellType = GetShellType();
        object? shell = null;
        object? shortcut = null;
        try
        {
            shell = Activator.CreateInstance(shellType)!;
            shortcut = shellType.InvokeMember("CreateShortcut", BindingFlags.InvokeMethod, null, shell, new object[] { shortcutPath })!;
            var shortcutType = shortcut.GetType();
            shortcutType.InvokeMember("TargetPath", BindingFlags.SetProperty, null, shortcut, new object[] { targetPath });
            shortcutType.InvokeMember("Arguments", BindingFlags.SetProperty, null, shortcut, new object[] { arguments });
            shortcutType.InvokeMember("Save", BindingFlags.InvokeMethod, null, shortcut, null);
        }
        finally
        {
            ReleaseIfComObject(shortcut);
            ReleaseIfComObject(shell);
        }
    }

    private static Type GetShellType() =>
        Type.GetTypeFromProgID("WScript.Shell")
        ?? throw new InvalidOperationException("The WScript.Shell COM component is not available on this system.");

    private static void ReleaseIfComObject(object? instance)
    {
        if (instance is not null && Marshal.IsComObject(instance))
        {
            Marshal.ReleaseComObject(instance);
        }
    }
}