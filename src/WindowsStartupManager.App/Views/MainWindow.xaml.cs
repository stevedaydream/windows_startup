using System.Windows;
using WindowsStartupManager.App.ViewModels;

namespace WindowsStartupManager.App.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private async void RemoveEntryButton_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedEntry is not { } entry) return;
        if (MessageBox.Show(this, $"Remove '{entry.Name}' from startup?\n\nThe application itself will not be uninstalled. You can disable the entry instead to keep it available for later.",
            "Remove startup entry", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
        {
            await ViewModel.RemoveCommand.ExecuteAsync(null).ConfigureAwait(true);
        }
    }

    private async void AddEntryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddEntryWindow { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is AddEntryRequest request)
        {
            await ViewModel.AddEntryCommand.ExecuteAsync(request).ConfigureAwait(true);
        }
    }
}
