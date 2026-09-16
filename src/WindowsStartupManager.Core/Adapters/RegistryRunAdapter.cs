using System.Diagnostics;
using Microsoft.Win32;
using WindowsStartupManager.Core.Metadata;
using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Services;

namespace WindowsStartupManager.Core.Adapters;

/// <summary>
/// Adapts the HKCU/HKLM ...\CurrentVersion\Run registry keys. Reading never creates
/// the key (a missing key just means no entries); writing creates it on demand.
/// </summary>
public sealed class RegistryRunAdapter : IStartupEntrySourceAdapter
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    public RegistryRunAdapter(StartupEntrySource source)
    {
        if (source is not (StartupEntrySource.CurrentUserRegistryRun or StartupEntrySource.LocalMachineRegistryRun))
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, "Registry adapter only supports Run key sources.");
        }
        Source = source;
    }

    public StartupEntrySource Source { get; }

    public bool RequiresElevation => Source == StartupEntrySource.LocalMachineRegistryRun;

    public Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<StartupEntry>();
        using var key = OpenBaseKey(writable: false, createIfMissing: false);
        if (key is null)
        {
            return Task.FromResult<IReadOnlyList<StartupEntry>>(results);
        }

        foreach (var valueName in key.GetValueNames())
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(valueName))
            {
                continue;
            }

            var raw = key.GetValue(valueName, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
            results.Add(StartupEntryFactory.Create(
                new StartupEntryKey(Source, valueName),
                valueName,
                Source,
                StartupEntryStatus.Enabled,
                raw));
        }

        return Task.FromResult<IReadOnlyList<StartupEntry>>(results);
    }

    public Task AddAsync(string name, string command, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        using var key = OpenBaseKey(writable: true, createIfMissing: true)!;
        key.SetValue(name, command, RegistryValueKind.String);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        using var baseKey = OpenBaseKey(writable: true, createIfMissing: false);
        baseKey?.DeleteValue(key.Identifier, throwOnMissingValue: false);
        return Task.CompletedTask;
    }

    public Task<StartupEntryRawData> CaptureAndRemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        using var baseKey = OpenBaseKey(writable: true, createIfMissing: false)
            ?? throw new InvalidOperationException("The Run key no longer exists.");

        var raw = baseKey.GetValue(key.Identifier, null, RegistryValueOptions.DoNotExpandEnvironmentNames) as string ?? string.Empty;
        baseKey.DeleteValue(key.Identifier, throwOnMissingValue: false);

        return Task.FromResult(new StartupEntryRawData
        {
            Name = key.Identifier,
            RawCommand = raw
        });
    }

    public Task RestoreAsync(StartupEntryRawData data, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        using var baseKey = OpenBaseKey(writable: true, createIfMissing: true)!;
        baseKey.SetValue(data.Name, data.RawCommand, RegistryValueKind.String);
        return Task.CompletedTask;
    }

    public void OpenSourceLocation(StartupEntryKey key)
    {
        var hiveName = Source == StartupEntrySource.CurrentUserRegistryRun ? "HKEY_CURRENT_USER" : "HKEY_LOCAL_MACHINE";
        TrySetRegeditLastKey($@"{hiveName}\{RunKeyPath}");
        Process.Start(new ProcessStartInfo("regedit.exe") { UseShellExecute = true });
    }

    private RegistryKey? OpenBaseKey(bool writable, bool createIfMissing)
    {
        var hive = Source == StartupEntrySource.CurrentUserRegistryRun ? Registry.CurrentUser : Registry.LocalMachine;
        var key = hive.OpenSubKey(RunKeyPath, writable);
        if (key is null && createIfMissing)
        {
            key = hive.CreateSubKey(RunKeyPath, writable);
        }
        return key;
    }

    private static void TrySetRegeditLastKey(string fullKeyPath)
    {
        try
        {
            using var applets = Registry.CurrentUser.CreateSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit", writable: true);
            applets?.SetValue("LastKey", $"Computer\\{fullKeyPath}", RegistryValueKind.String);
        }
        catch
        {
            // Best effort only; regedit still opens even if the last-key hint can't be set.
        }
    }

    private void EnsureElevationIfRequired()
    {
        if (RequiresElevation && !ElevationHelper.IsElevated())
        {
            throw new ElevationRequiredException(Source);
        }
    }
}