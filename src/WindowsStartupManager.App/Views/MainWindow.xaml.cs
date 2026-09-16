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

    private async void AddEntryButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new AddEntryWindow { Owner = this };
        if (dialog.ShowDialog() == true && dialog.Result is AddEntryRequest request)
        {
            await ViewModel.AddEntryCommand.ExecuteAsync(request).ConfigureAwait(true);
        }
    }
}