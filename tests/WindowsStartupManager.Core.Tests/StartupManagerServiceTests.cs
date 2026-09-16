using WindowsStartupManager.Core;
using WindowsStartupManager.Core.Adapters;
using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Services;
using Xunit;

namespace WindowsStartupManager.Core.Tests;

public class StartupManagerServiceTests
{
    private static StartupEntry MakeEntry(StartupEntryKey key) => new()
    {
        Key = key,
        Name = "My App",
        Source = key.Source,
        Status = StartupEntryStatus.Enabled,
        RawCommand = "C:\\App\\app.exe"
    };

    [Fact]
    public async Task DisableAsync_WhenPersistSucceeds_RemovesLiveEntryAndStoresRecord()
    {
        var key = new StartupEntryKey(StartupEntrySource.CurrentUserRegistryRun, "MyApp");
        var adapter = new FakeAdapter(StartupEntrySource.CurrentUserRegistryRun);
        var store = new FakeDisabledEntryStore();
        var service = new StartupManagerService(new[] { adapter }, store);

        await service.DisableAsync(MakeEntry(key));

        Assert.Equal(1, adapter.CaptureAndRemoveCalls);
        Assert.Equal(0, adapter.RestoreCalls);
        Assert.Single(store.Records);
        Assert.Equal("MyApp", store.Records[0].Identifier);
    }

    [Fact]
    public async Task DisableAsync_WhenPersistFails_RestoresLiveEntryAndSurfacesOriginalError()
    {
        var key = new StartupEntryKey(StartupEntrySource.CurrentUserRegistryRun, "MyApp");
        var adapter = new FakeAdapter(StartupEntrySource.CurrentUserRegistryRun);
        var store = new FakeDisabledEntryStore { ThrowOnAdd = new IOException("disk full") };
        var service = new StartupManagerService(new[] { adapter }, store);

        var ex = await Assert.ThrowsAsync<IOException>(() => service.DisableAsync(MakeEntry(key)));

        Assert.Equal("disk full", ex.Message);
        Assert.Equal(1, adapter.CaptureAndRemoveCalls);
        Assert.Equal(1, adapter.RestoreCalls);
        Assert.Empty(store.Records);
    }

    [Fact]
    public async Task DisableAsync_WhenPersistFailsBecauseCallerTokenWasCancelled_RestoresWithNonCancelledTokenAndRethrowsCancellation()
    {
        var key = new StartupEntryKey(StartupEntrySource.CurrentUserRegistryRun, "MyApp");
        var adapter = new FakeAdapter(StartupEntrySource.CurrentUserRegistryRun);
        using var cts = new CancellationTokenSource();
        var store = new FakeDisabledEntryStore { ThrowOnAdd = new OperationCanceledException(cts.Token) };
        var service = new StartupManagerService(new[] { adapter }, store);

        cts.Cancel();

        var ex = await Assert.ThrowsAsync<OperationCanceledException>(() => service.DisableAsync(MakeEntry(key), cts.Token));

        Assert.Equal(cts.Token, ex.CancellationToken);
        Assert.Equal(1, adapter.RestoreCalls);
        Assert.False(adapter.LastRestoreToken.IsCancellationRequested);
        Assert.Empty(store.Records);
    }

    [Fact]
    public async Task DisableAsync_WhenPersistAndRollbackFail_ThrowsWithBothErrors()
    {
        var key = new StartupEntryKey(StartupEntrySource.CurrentUserRegistryRun, "MyApp");
        var adapter = new FakeAdapter(StartupEntrySource.CurrentUserRegistryRun)
        {
            ThrowOnRestore = new InvalidOperationException("registry locked")
        };
        var store = new FakeDisabledEntryStore { ThrowOnAdd = new IOException("disk full") };
        var service = new StartupManagerService(new[] { adapter }, store);

        var ex = await Assert.ThrowsAsync<DisableRollbackFailedException>(() => service.DisableAsync(MakeEntry(key)));

        Assert.Same(store.ThrowOnAdd, ex.PersistException);
        Assert.Same(adapter.ThrowOnRestore, ex.RollbackException);
        Assert.Equal(1, adapter.CaptureAndRemoveCalls);
        Assert.Equal(1, adapter.RestoreCalls);
    }

    private sealed class FakeAdapter : IStartupEntrySourceAdapter
    {
        public FakeAdapter(StartupEntrySource source) => Source = source;

        public StartupEntrySource Source { get; }
        public bool RequiresElevation => false;
        public int CaptureAndRemoveCalls { get; private set; }
        public int RestoreCalls { get; private set; }
        public CancellationToken LastRestoreToken { get; private set; }
        public Exception? ThrowOnRestore { get; set; }

        public Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<StartupEntry>>(Array.Empty<StartupEntry>());

        public Task AddAsync(string name, string command, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public Task<StartupEntryRawData> CaptureAndRemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
        {
            CaptureAndRemoveCalls++;
            return Task.FromResult(new StartupEntryRawData
            {
                Name = "My App",
                RawCommand = "C:\\App\\app.exe"
            });
        }

        public Task RestoreAsync(StartupEntryRawData data, CancellationToken cancellationToken = default)
        {
            RestoreCalls++;
            LastRestoreToken = cancellationToken;
            if (ThrowOnRestore is not null)
            {
                throw ThrowOnRestore;
            }
            return Task.CompletedTask;
        }

        public void OpenSourceLocation(StartupEntryKey key)
        {
        }
    }

    private sealed class FakeDisabledEntryStore : IDisabledEntryStore
    {
        public List<DisabledEntryRecord> Records { get; } = new();
        public Exception? ThrowOnAdd { get; set; }

        public Task<IReadOnlyList<DisabledEntryRecord>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DisabledEntryRecord>>(Records);

        public Task<DisabledEntryRecord?> TryGetAsync(StartupEntryKey key, CancellationToken cancellationToken = default) =>
            Task.FromResult(Records.FirstOrDefault(r => r.Source == key.Source.ToString() && r.Identifier == key.Identifier));

        public Task AddAsync(DisabledEntryRecord record, CancellationToken cancellationToken = default)
        {
            if (ThrowOnAdd is not null)
            {
                throw ThrowOnAdd;
            }
            Records.Add(record);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
        {
            Records.RemoveAll(r => r.Source == key.Source.ToString() && r.Identifier == key.Identifier);
            return Task.CompletedTask;
        }
    }
}
