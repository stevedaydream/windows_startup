using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.Core.Adapters;

/// <summary>
/// Owns enumeration and mutation for one Windows startup source (a registry Run key
/// or a Startup folder). Disable/enable is modeled as capture-and-remove followed by
/// restore, so the application-managed disabled store can hold everything needed to
/// reverse the change.
/// </summary>
public interface IStartupEntrySourceAdapter
{
    StartupEntrySource Source { get; }

    bool RequiresElevation { get; }

    Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken = default);

    Task AddAsync(string name, string command, CancellationToken cancellationToken = default);

    Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default);

    Task<StartupEntryRawData> CaptureAndRemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default);

    Task RestoreAsync(StartupEntryRawData data, CancellationToken cancellationToken = default);

    void OpenSourceLocation(StartupEntryKey key);
}