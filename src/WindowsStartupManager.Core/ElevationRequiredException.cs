using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.Core;

/// <summary>
/// Thrown when an operation targets a machine-wide startup source but the current
/// process is not running elevated.
/// </summary>
public sealed class ElevationRequiredException : Exception
{
    public ElevationRequiredException(StartupEntrySource startupSource)
        : base($"This action modifies a machine-wide startup entry ({startupSource}) and requires administrator privileges.")
    {
        StartupSource = startupSource;
    }

    public StartupEntrySource StartupSource { get; }
}