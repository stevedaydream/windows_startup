namespace WindowsStartupManager.Core.Models;

public enum StartupEntrySource
{
    CurrentUserRegistryRun,
    LocalMachineRegistryRun,
    CurrentUserStartupFolder,
    CommonStartupFolder
}