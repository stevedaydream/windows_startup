using System.Text.Json;
using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.Core.Services;

public sealed class DisabledEntryRecord
{
    public required string Source { get; init; }
    public required string Identifier { get; init; }
    public required string Name { get; init; }
    public required string RawCommand { get; init; }
    public string? OriginalFilePath { get; init; }
    public string? BackupFilePath { get; init; }
    public DateTimeOffset DisabledAtUtc { get; init; }
}

/// <summary>
/// Persists disabled startup entries as JSON so disable/enable is reversible without
/// relying on any undocumented Windows state. Not thread-safe across processes, but
/// safe for concurrent calls within this app via an internal lock.
/// </summary>
public sealed class DisabledEntryStore : IDisabledEntryStore
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public DisabledEntryStore(string filePath)
    {
        _filePath = filePath;
    }

    public static string GetDefaultFilePath()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "WindowsStartupManager", "disabled-entries.json");
    }

    public async Task<IReadOnlyList<DisabledEntryRecord>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<DisabledEntryRecord?> TryGetAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken).ConfigureAwait(false);
        return all.FirstOrDefault(r => r.Source == key.Source.ToString() && r.Identifier == key.Identifier);
    }

    public async Task AddAsync(DisabledEntryRecord record, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var all = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            all.RemoveAll(r => r.Source == record.Source && r.Identifier == record.Identifier);
            all.Add(record);
            await SaveUnlockedAsync(all, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var all = (await LoadUnlockedAsync(cancellationToken).ConfigureAwait(false)).ToList();
            all.RemoveAll(r => r.Source == key.Source.ToString() && r.Identifier == key.Identifier);
            await SaveUnlockedAsync(all, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    private async Task<IReadOnlyList<DisabledEntryRecord>> LoadUnlockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_filePath))
        {
            return Array.Empty<DisabledEntryRecord>();
        }

        await using var stream = File.OpenRead(_filePath);
        var records = await JsonSerializer.DeserializeAsync<List<DisabledEntryRecord>>(stream, cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return records ?? new List<DisabledEntryRecord>();
    }

    private async Task SaveUnlockedAsync(IReadOnlyList<DisabledEntryRecord> records, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(_filePath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var options = new JsonSerializerOptions { WriteIndented = true };
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, records, options, cancellationToken).ConfigureAwait(false);
    }
}