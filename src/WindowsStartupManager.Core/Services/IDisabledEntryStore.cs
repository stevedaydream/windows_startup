using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.Core.Services;

/// <summary>
/// Persistence seam for disabled-entry records, so <see cref="StartupManagerService"/>
/// can be tested against a fake store without touching disk.
/// </summary>
public interface IDisabledEntryStore
{
    Task<IReadOnlyList<DisabledEntryRecord>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<DisabledEntryRecord?> TryGetAsync(StartupEntryKey key, CancellationToken cancellationToken = default);

    Task AddAsync(DisabledEntryRecord record, CancellationToken cancellationToken = default);

    Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default);
}
