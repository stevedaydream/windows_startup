namespace WindowsStartupManager.Core.Models;

/// <summary>
/// Captures everything needed to recreate a startup entry that has been disabled,
/// including where a moved shortcut file currently lives.
/// </summary>
public sealed class StartupEntryRawData
{
    public required string Name { get; init; }
    public required string RawCommand { get; init; }
    public string? OriginalFilePath { get; init; }
    public string? BackupFilePath { get; init; }
}