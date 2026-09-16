using System.Diagnostics;
using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Parsing;

namespace WindowsStartupManager.Core.Metadata;

/// <summary>
/// Builds a <see cref="StartupEntry"/> from a raw command, resolving the executable
/// path and reading file version metadata. Failures are recorded as a user-facing
/// note on the entry rather than thrown, since a single bad entry must not stop the
/// rest of the list from loading.
/// </summary>
public static class StartupEntryFactory
{
    public static StartupEntry Create(
        StartupEntryKey key,
        string name,
        StartupEntrySource source,
        StartupEntryStatus status,
        string rawCommand)
    {
        string? resolvedPath = null;
        string? productName = null;
        string? applicationName = null;
        string? publisher = null;
        string? version = null;
        string? notes = null;

        try
        {
            var extracted = CommandLineParser.ExtractExecutablePath(rawCommand);
            if (extracted is not null)
            {
                resolvedPath = CommandLineParser.ExpandEnvironmentVariables(extracted);
                if (!File.Exists(resolvedPath))
                {
                    notes = "Resolved executable was not found on disk.";
                    resolvedPath = null;
                }
            }

            if (resolvedPath is not null)
            {
                var info = FileVersionInfo.GetVersionInfo(resolvedPath);
                productName = NullIfEmpty(info.ProductName);
                applicationName = NullIfEmpty(info.FileDescription);
                publisher = NullIfEmpty(info.CompanyName);
                version = NullIfEmpty(info.FileVersion);
            }
        }
        catch (Exception ex)
        {
            notes = $"Could not read file metadata: {ex.Message}";
        }

        return new StartupEntry
        {
            Key = key,
            Name = name,
            Source = source,
            Status = status,
            RawCommand = rawCommand,
            ResolvedExecutablePath = resolvedPath,
            ProductName = productName,
            ApplicationName = applicationName,
            Publisher = publisher,
            Version = version,
            Notes = notes
        };
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}