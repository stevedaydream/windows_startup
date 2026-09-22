using System.Diagnostics;
using WindowsStartupManager.Core.Interop;
using WindowsStartupManager.Core.Metadata;
using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Parsing;
using WindowsStartupManager.Core.Services;

namespace WindowsStartupManager.Core.Adapters;

/// <summary>
/// Adapts the current-user/common Startup folders. Disabling moves the shortcut into
/// an app-managed backup directory (rather than deleting it) so enabling can move it
/// back to its exact original location.
/// </summary>
public sealed class StartupFolderAdapter : IStartupEntrySourceAdapter
{
    private readonly string _startupFolderPath;
    private readonly string _disabledBackupDirectory;

    public StartupFolderAdapter(StartupEntrySource source, string? disabledBackupDirectory = null)
    {
        if (source is not (StartupEntrySource.CurrentUserStartupFolder or StartupEntrySource.CommonStartupFolder))
        {
            throw new ArgumentOutOfRangeException(nameof(source), source, "Startup folder adapter only supports folder sources.");
        }

        Source = source;
        _startupFolderPath = Environment.GetFolderPath(
            source == StartupEntrySource.CurrentUserStartupFolder
                ? Environment.SpecialFolder.Startup
                : Environment.SpecialFolder.CommonStartup);
        _disabledBackupDirectory = disabledBackupDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsStartupManager", "DisabledShortcuts");
    }

    public StartupEntrySource Source { get; }

    public bool RequiresElevation => Source == StartupEntrySource.CommonStartupFolder;

    public Task<IReadOnlyList<StartupEntry>> GetEntriesAsync(CancellationToken cancellationToken = default)
    {
        var results = new List<StartupEntry>();
        if (!Directory.Exists(_startupFolderPath))
        {
            return Task.FromResult<IReadOnlyList<StartupEntry>>(results);
        }

        foreach (var file in Directory.EnumerateFiles(_startupFolderPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (string.Equals(Path.GetFileName(file), "desktop.ini", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var name = GetDisplayName(file);
            var key = new StartupEntryKey(Source, Path.GetFileName(file));

            try
            {
                var rawCommand = ReadRawCommand(file);
                results.Add(StartupEntryFactory.Create(key, name, Source, StartupEntryStatus.Enabled, rawCommand));
            }
            catch (Exception ex)
            {
                results.Add(new StartupEntry
                {
                    Key = key,
                    Name = name,
                    Source = Source,
                    Status = StartupEntryStatus.Enabled,
                    RawCommand = string.Empty,
                    Notes = $"Could not read shortcut: {ex.Message}"
                });
            }
        }

        return Task.FromResult<IReadOnlyList<StartupEntry>>(results);
    }

    public Task AddAsync(string name, string command, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        Directory.CreateDirectory(_startupFolderPath);
        var (targetPath, arguments) = SplitCommand(command);
        var shortcutPath = Path.Combine(_startupFolderPath, SanitizeFileName(name) + ".lnk");
        ShellLinkService.CreateShortcut(shortcutPath, targetPath, arguments);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        var path = Path.Combine(_startupFolderPath, key.Identifier);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    public Task<StartupEntryRawData> CaptureAndRemoveAsync(StartupEntryKey key, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        var originalPath = Path.Combine(_startupFolderPath, key.Identifier);
        if (!File.Exists(originalPath))
        {
            throw new FileNotFoundException("Startup shortcut was not found.", originalPath);
        }

        string rawCommand;
        try
        {
            rawCommand = ReadRawCommand(originalPath);
        }
        catch
        {
            rawCommand = string.Empty;
        }

        Directory.CreateDirectory(_disabledBackupDirectory);
        var backupPath = Path.Combine(_disabledBackupDirectory, MakeUniqueBackupName(key.Identifier));
        File.Move(originalPath, backupPath, overwrite: false);

        return Task.FromResult(new StartupEntryRawData
        {
            Name = GetDisplayName(key.Identifier),
            RawCommand = rawCommand,
            OriginalFilePath = originalPath,
            BackupFilePath = backupPath
        });
    }

    public Task RestoreAsync(StartupEntryRawData data, CancellationToken cancellationToken = default)
    {
        EnsureElevationIfRequired();
        if (string.IsNullOrEmpty(data.BackupFilePath) || string.IsNullOrEmpty(data.OriginalFilePath))
        {
            throw new InvalidOperationException("Missing backup information required to restore the startup shortcut.");
        }

        if (!File.Exists(data.BackupFilePath))
        {
            throw new FileNotFoundException("The backed-up shortcut could not be found.", data.BackupFilePath);
        }

        var originalDirectory = Path.GetDirectoryName(data.OriginalFilePath);
        if (!string.IsNullOrEmpty(originalDirectory))
        {
            Directory.CreateDirectory(originalDirectory);
        }

        File.Move(data.BackupFilePath, data.OriginalFilePath, overwrite: false);
        return Task.CompletedTask;
    }

    public void OpenSourceLocation(StartupEntryKey key)
    {
        var path = Path.Combine(_startupFolderPath, key.Identifier);
        var argument = File.Exists(path) ? $"/select,\"{path}\"" : $"\"{_startupFolderPath}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", argument) { UseShellExecute = true });
    }

    private static bool IsShortcut(string path) =>
        string.Equals(Path.GetExtension(path), ".lnk", StringComparison.OrdinalIgnoreCase);

    // Shortcuts show without ".lnk"; other files (e.g. .bat) keep their full name so
    // "x.bat" and "x.bat.bak" stay distinguishable.
    private static string GetDisplayName(string path) =>
        IsShortcut(path) ? Path.GetFileNameWithoutExtension(path) : Path.GetFileName(path);

    // Windows launches every file in a Startup folder, so non-shortcut files (scripts,
    // executables) are reported with the file itself as the command.
    private static string ReadRawCommand(string path)
    {
        if (!IsShortcut(path))
        {
            return path.Contains(' ') ? $"\"{path}\"" : path;
        }

        var (targetPath, arguments) = ShellLinkService.ReadShortcut(path);
        return BuildRawCommand(targetPath, arguments);
    }

    private static string BuildRawCommand(string targetPath, string arguments)
    {
        var quotedTarget = targetPath.Contains(' ') ? $"\"{targetPath}\"" : targetPath;
        return string.IsNullOrWhiteSpace(arguments) ? quotedTarget : $"{quotedTarget} {arguments}";
    }

    private static (string TargetPath, string Arguments) SplitCommand(string command)
    {
        var executable = CommandLineParser.ExtractExecutablePath(command) ?? command;
        var expanded = CommandLineParser.ExpandEnvironmentVariables(command).Trim();
        var quotedExecutable = executable.Contains(' ') ? $"\"{executable}\"" : executable;

        string remainder;
        if (expanded.StartsWith(quotedExecutable, StringComparison.OrdinalIgnoreCase))
        {
            remainder = expanded[quotedExecutable.Length..].TrimStart();
        }
        else if (expanded.StartsWith(executable, StringComparison.OrdinalIgnoreCase))
        {
            remainder = expanded[executable.Length..].TrimStart();
        }
        else
        {
            remainder = string.Empty;
        }

        return (executable, remainder);
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        return new string(chars);
    }

    private static string MakeUniqueBackupName(string identifier)
    {
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
        return $"{stamp}_{identifier}";
    }

    private void EnsureElevationIfRequired()
    {
        if (RequiresElevation && !ElevationHelper.IsElevated())
        {
            throw new ElevationRequiredException(Source);
        }
    }
}