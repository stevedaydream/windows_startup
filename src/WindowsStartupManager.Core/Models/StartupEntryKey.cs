namespace WindowsStartupManager.Core.Models;

/// <summary>
/// Identifies a startup entry within its source: a registry value name for Run keys,
/// or a shortcut file name for Startup folders.
/// </summary>
public sealed record StartupEntryKey(StartupEntrySource Source, string Identifier);