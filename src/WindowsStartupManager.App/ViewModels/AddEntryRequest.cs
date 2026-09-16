using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.App.ViewModels;

public sealed record AddEntryRequest(StartupEntrySource Source, string Name, string Command);