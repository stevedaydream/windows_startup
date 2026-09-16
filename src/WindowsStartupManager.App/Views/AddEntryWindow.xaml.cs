using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using WindowsStartupManager.App.ViewModels;
using WindowsStartupManager.Core.Models;

namespace WindowsStartupManager.App.Views;

public partial class AddEntryWindow : Window
{
    public AddEntryWindow()
    {
        InitializeComponent();
    }

    public AddEntryRequest? Result { get; private set; }

    private void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
        };

        if (dialog.ShowDialog(this) == true)
        {
            CommandTextBox.Text = $"\"{dialog.FileName}\"";
        }
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        var name = NameTextBox.Text.Trim();
        var command = CommandTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(command))
        {
            MessageBox.Show(this, "Both a name and a command are required.", "Add Startup Entry", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var tag = (SourceComboBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() ?? nameof(StartupEntrySource.CurrentUserRegistryRun);
        var source = Enum.Parse<StartupEntrySource>(tag);

        Result = new AddEntryRequest(source, name, command);
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}