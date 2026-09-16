using System.Windows;
using WindowsStartupManager.App.ViewModels;
using WindowsStartupManager.App.Views;
using WindowsStartupManager.Core.Adapters;
using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Services;

namespace WindowsStartupManager.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var disabledStore = new DisabledEntryStore(DisabledEntryStore.GetDefaultFilePath());
        var adapters = new IStartupEntrySourceAdapter[]
        {
            new RegistryRunAdapter(StartupEntrySource.CurrentUserRegistryRun),
            new RegistryRunAdapter(StartupEntrySource.LocalMachineRegistryRun),
            new StartupFolderAdapter(StartupEntrySource.CurrentUserStartupFolder),
            new StartupFolderAdapter(StartupEntrySource.CommonStartupFolder)
        };
        var service = new StartupManagerService(adapters, disabledStore);
        var viewModel = new MainViewModel(service);

        var window = new MainWindow(viewModel);
        MainWindow = window;
        window.Show();

        _ = viewModel.RefreshAsync();
    }
}