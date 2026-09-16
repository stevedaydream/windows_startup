using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Services;
using Xunit;

namespace WindowsStartupManager.Core.Tests;

public class DisabledEntryStoreTests : IDisposable
{
    private readonly string _tempFile;
    private readonly DisabledEntryStore _store;

    public DisabledEntryStoreTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"wsm-tests-{Guid.NewGuid():N}.json");
        _store = new DisabledEntryStore(_tempFile);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
        {
            File.Delete(_tempFile);
        }
    }

    [Fact]
    public async Task AddAsync_ThenTryGetAsync_RoundTripsRecord()
    {
        var key = new StartupEntryKey(StartupEntrySource.CurrentUserRegistryRun, "MyApp");
        var record = new DisabledEntryRecord
        {
            Source = key.Source.ToString(),
            Identifier = key.Identifier,
            Name = "My App",
            RawCommand = "\"C:\\App\\app.exe\" --silent",
            DisabledAtUtc = DateTimeOffset.UtcNow
        };

        await _store.AddAsync(record);
        var fetched = await _store.TryGetAsync(key);

        Assert.NotNull(fetched);
        Assert.Equal(record.Name, fetched!.Name);
        Assert.Equal(record.RawCommand, fetched.RawCommand);
    }

    [Fact]
    public async Task RemoveAsync_ReversesDisable()
    {
        var key = new StartupEntryKey(StartupEntrySource.CurrentUserStartupFolder, "MyApp.lnk");
        await _store.AddAsync(new DisabledEntryRecord
        {
            Source = key.Source.ToString(),
            Identifier = key.Identifier,
            Name = "My App",
            RawCommand = "C:\\App\\app.exe",
            DisabledAtUtc = DateTimeOffset.UtcNow
        });

        await _store.RemoveAsync(key);
        var fetched = await _store.TryGetAsync(key);

        Assert.Null(fetched);
    }

    [Fact]
    public async Task AddAsync_ReplacesExistingRecordForSameKey()
    {
        var key = new StartupEntryKey(StartupEntrySource.LocalMachineRegistryRun, "Shared");
        await _store.AddAsync(new DisabledEntryRecord
        {
            Source = key.Source.ToString(),
            Identifier = key.Identifier,
            Name = "First",
            RawCommand = "first.exe",
            DisabledAtUtc = DateTimeOffset.UtcNow
        });
        await _store.AddAsync(new DisabledEntryRecord
        {
            Source = key.Source.ToString(),
            Identifier = key.Identifier,
            Name = "Second",
            RawCommand = "second.exe",
            DisabledAtUtc = DateTimeOffset.UtcNow
        });

        var all = await _store.GetAllAsync();

        Assert.Single(all, r => r.Source == key.Source.ToString() && r.Identifier == key.Identifier);
        var fetched = await _store.TryGetAsync(key);
        Assert.Equal("Second", fetched!.Name);
    }

    [Fact]
    public async Task GetAllAsync_WhenFileDoesNotExist_ReturnsEmpty()
    {
        var all = await _store.GetAllAsync();

        Assert.Empty(all);
    }
}