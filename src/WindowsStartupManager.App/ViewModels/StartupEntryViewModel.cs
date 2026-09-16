using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.App.ViewModels;

public sealed class StartupEntryViewModel
{
    public StartupEntryViewModel(StartupEntry model)
    {
        Model = model;
    }

    public StartupEntry Model { get; }

    public string Name => Model.Name;
    public string SourceDisplayName => Model.SourceDisplayName;
    public string StatusDisplayName => Model.Status == StartupEntryStatus.Enabled ? "Enabled" : "Disabled";
    public string RawCommand => Model.RawCommand;
    public string ResolvedExecutablePath => Model.ResolvedExecutablePath ?? "(not found)";
    public string ProductName => Model.ProductName ?? string.Empty;
    public string ApplicationName => Model.ApplicationName ?? string.Empty;
    public string Publisher => Model.Publisher ?? string.Empty;
    public string Version => Model.Version ?? string.Empty;
    public string Notes => Model.Notes ?? string.Empty;
    public bool IsEnabled => Model.Status == StartupEntryStatus.Enabled;
    public bool RequiresElevation => Model.RequiresElevation;
}