using System.Diagnostics;
using WindowsStartupManager.Core.Adapters;
using WindowsStartupManager.Core.Metadata;
using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.Core.Services;

/// <summary>
/// Aggregates all source adapters behind a single API for the UI: enumeration merges
/// live entries with disabled ones tracked in the <see cref="DisabledEntryStore"/>,
/// and disable/enable round-trips an entry through capture-and-remove/restore.
/// </summary>
public sealed class StartupManagerService
{
    private readonly IReadOnlyDictionary<StartupEntrySource, IStartupEntrySourceAdapter> _adapters;
    private readonly IDisabledEntryStore _disabledStore;

    public StartupManagerService(IEnumerable<IStartupEntrySourceAdapter> adapters, IDisabledEntryStore disabledStore)
    {
        _adapters = adapters.ToDictionary(a => a.Source);
        _disabledStore = disabledStore;
    }

    public async Task<IReadOnlyList<StartupEntry>> GetAllEntriesAsync(CancellationToken cancellationToken = default)
    {
        var entries = new List<StartupEntry>();

        foreach (var adapter in _adapters.Values)
        {
            try
            {
                entries.AddRange(await adapter.GetEntriesAsync(cancellationToken).ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                entries.Add(new StartupEntry
                {
                    Key = new StartupEntryKey(adapter.Source, string.Empty),
                    Name = $"({adapter.Source} unavailable)",
                    Source = adapter.Source,
                    Status = StartupEntryStatus.Enabled,
                    RawCommand = string.Empty,
                    Notes = $"Could not read entries: {ex.Message}"
                });
            }
        }

        var disabledRecords = await _disabledStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
        foreach (var record in disabledRecords)
        {
            if (!Enum.TryParse<StartupEntrySource>(record.Source, out var source))
            {
                continue;
            }

            entries.Add(StartupEntryFactory.Create(
                new StartupEntryKey(source, record.Identifier),
                record.Name,
                source,
                StartupEntryStatus.Disabled,
                record.RawCommand));
        }

        return entries
            .OrderBy(e => e.Source)
            .ThenBy(e => e.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public Task AddEntryAsync(StartupEntrySource source, string name, string command, CancellationToken cancellationToken = default) =>
        GetAdapter(source).AddAsync(name, command, cancellationToken);

    public Task RemoveEntryAsync(StartupEntry entry, CancellationToken cancellationToken = default) =>
        entry.Status == StartupEntryStatus.Disabled
            ? RemoveDisabledAsync(entry, cancellationToken)
            : GetAdapter(entry.Source).RemoveAsync(entry.Key, cancellationToken);

    public async Task DisableAsync(StartupEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Status == StartupEntryStatus.Disabled)
        {
            return;
        }

        var adapter = GetAdapter(entry.Source);
        var raw = await adapter.CaptureAndRemoveAsync(entry.Key, cancellationToken).ConfigureAwait(false);

        try
        {
            await _disabledStore.AddAsync(new DisabledEntryRecord
            {
                Source = entry.Source.ToString(),
                Identifier = entry.Key.Identifier,
                Name = raw.Name,
                RawCommand = raw.RawCommand,
                OriginalFilePath = raw.OriginalFilePath,
                BackupFilePath = raw.BackupFilePath,
                DisabledAtUtc = DateTimeOffset.UtcNow
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception persistException)
        {
            try
            {
                // Rollback must not inherit the caller's token: if persistence failed because
                // that token was cancelled, restoring with it would abort the compensating
                // write and leave the entry removed with no record of it anywhere.
                await adapter.RestoreAsync(raw, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception rollbackException)
            {
                throw new DisableRollbackFailedException(persistException, rollbackException);
            }

            throw;
        }
    }

    public async Task EnableAsync(StartupEntry entry, CancellationToken cancellationToken = default)
    {
        if (entry.Status == StartupEntryStatus.Enabled)
        {
            return;
        }

        var record = await _disabledStore.TryGetAsync(entry.Key, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException("No disabled record was found for this entry.");

        var adapter = GetAdapter(entry.Source);
        await adapter.RestoreAsync(new StartupEntryRawData
        {
            Name = record.Name,
            RawCommand = record.RawCommand,
            OriginalFilePath = record.OriginalFilePath,
            BackupFilePath = record.BackupFilePath
        }, cancellationToken).ConfigureAwait(false);

        await _disabledStore.RemoveAsync(entry.Key, cancellationToken).ConfigureAwait(false);
    }

    public void OpenSourceLocation(StartupEntry entry) => GetAdapter(entry.Source).OpenSourceLocation(entry.Key);

    public void OpenResolvedLocation(StartupEntry entry)
    {
        if (string.IsNullOrEmpty(entry.ResolvedExecutablePath) || !File.Exists(entry.ResolvedExecutablePath))
        {
            throw new FileNotFoundException("The resolved executable could not be found on disk.");
        }

        Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{entry.ResolvedExecutablePath}\"")
        {
            UseShellExecute = true
        });
    }

    private async Task RemoveDisabledAsync(StartupEntry entry, CancellationToken cancellationToken)
    {
        var record = await _disabledStore.TryGetAsync(entry.Key, cancellationToken).ConfigureAwait(false);
        if (record is not null && !string.IsNullOrEmpty(record.BackupFilePath) && File.Exists(record.BackupFilePath))
        {
            File.Delete(record.BackupFilePath);
        }
        await _disabledStore.RemoveAsync(entry.Key, cancellationToken).ConfigureAwait(false);
    }

    private IStartupEntrySourceAdapter GetAdapter(StartupEntrySource source) =>
        _adapters.TryGetValue(source, out var adapter)
            ? adapter
            : throw new InvalidOperationException($"No adapter registered for source {source}.");
}