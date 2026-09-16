namespace WindowsStartupManager.Core.Models;

public sealed class StartupEntry
{
    public required StartupEntryKey Key { get; init; }
    public required string Name { get; init; }
    public required StartupEntrySource Source { get; init; }
    public required StartupEntryStatus Status { get; init; }
    public required string RawCommand { get; init; }
    public string? ResolvedExecutablePath { get; init; }
    public string? ProductName { get; init; }
    public string? ApplicationName { get; init; }
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public string? Notes { get; init; }

    public string SourceDisplayName => Source switch
    {
        StartupEntrySource.CurrentUserRegistryRun => "Registry (Current User)",
        StartupEntrySource.LocalMachineRegistryRun => "Registry (All Users)",
        StartupEntrySource.CurrentUserStartupFolder => "Startup Folder (Current User)",
        StartupEntrySource.CommonStartupFolder => "Startup Folder (All Users)",
        _ => Source.ToString()
    };

    public bool RequiresElevation =>
        Source is StartupEntrySource.LocalMachineRegistryRun or StartupEntrySource.CommonStartupFolder;
}