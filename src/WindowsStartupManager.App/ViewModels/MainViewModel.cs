using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Data;
using WindowsStartupManager.Core;
using WindowsStartupManager.Core.Models;
using WindowsStartupManager.Core.Services;

namespace WindowsStartupManager.App.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly StartupManagerService _service;
    private bool _isBusy;
    private string? _statusMessage;
    private string? _errorMessage;
    private StartupEntryViewModel? _selectedEntry;
    private string _searchText = string.Empty;
    private int _statusFilterIndex;

    public MainViewModel(StartupManagerService service)
    {
        _service = service;
        Entries = new ObservableCollection<StartupEntryViewModel>();
        EntriesView = CollectionViewSource.GetDefaultView(Entries);
        EntriesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(StartupEntryViewModel.SourceDisplayName)));
        EntriesView.Filter = MatchesFilter;
        Entries.CollectionChanged += (_, _) => UpdateViewSummary();

        RefreshCommand = new AsyncRelayCommand(_ => RefreshAsync());
        AddEntryCommand = new AsyncRelayCommand(async parameter =>
        {
            if (parameter is AddEntryRequest request)
            {
                await AddEntryAsync(request).ConfigureAwait(true);
            }
        });
        ToggleEnabledCommand = new AsyncRelayCommand(_ => ToggleEnabledAsync(), _ => SelectedEntry is not null);
        RemoveCommand = new AsyncRelayCommand(_ => RemoveAsync(), _ => SelectedEntry is not null);
        OpenSourceLocationCommand = new RelayCommand(_ => OpenSourceLocation(), _ => SelectedEntry is not null);
        OpenResolvedLocationCommand = new RelayCommand(_ => OpenResolvedLocation(), _ => SelectedEntry is not null);
        RestartAsAdministratorCommand = new RelayCommand(_ => RestartAsAdministrator(), _ => !IsElevated);
    }

    public bool IsElevated { get; } = ElevationHelper.IsElevated();

    public ObservableCollection<StartupEntryViewModel> Entries { get; }
    public ICollectionView EntriesView { get; }

    public StartupEntryViewModel? SelectedEntry
    {
        get => _selectedEntry;
        set
        {
            if (SetProperty(ref _selectedEntry, value))
            {
                OnPropertyChanged(nameof(ToggleActionText));
                System.Windows.Input.CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public string ToggleActionText => SelectedEntry?.IsEnabled == true ? "Disable entry" : "Enable entry";
    public string SearchText
    {
        get => _searchText;
        set { if (SetProperty(ref _searchText, value)) ApplyFilter(); }
    }
    public int StatusFilterIndex
    {
        get => _statusFilterIndex;
        set { if (SetProperty(ref _statusFilterIndex, value)) ApplyFilter(); }
    }
    public bool IsViewEmpty => EntriesView.IsEmpty;
    public string EntrySummary => $"{EntriesView.Cast<object>().Count()} of {Entries.Count} entries";

    private bool MatchesFilter(object item)
    {
        if (item is not StartupEntryViewModel entry) return false;
        if (StatusFilterIndex == 1 && !entry.IsEnabled || StatusFilterIndex == 2 && entry.IsEnabled) return false;
        var query = SearchText.Trim();
        return query.Length == 0 || new[] { entry.Name, entry.Publisher, entry.RawCommand, entry.ApplicationName }
            .Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));
    }

    private void ApplyFilter()
    {
        EntriesView.Refresh();
        if (SelectedEntry is not null && !EntriesView.Contains(SelectedEntry)) SelectedEntry = null;
        UpdateViewSummary();
    }

    private void UpdateViewSummary()
    {
        OnPropertyChanged(nameof(EntrySummary));
        OnPropertyChanged(nameof(IsViewEmpty));
    }

    public bool IsBusy
    {
        get => _isBusy;
        private set => SetProperty(ref _isBusy, value);
    }

    public string? StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public AsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand AddEntryCommand { get; }
    public AsyncRelayCommand ToggleEnabledCommand { get; }
    public AsyncRelayCommand RemoveCommand { get; }
    public RelayCommand OpenSourceLocationCommand { get; }
    public RelayCommand OpenResolvedLocationCommand { get; }
    public RelayCommand RestartAsAdministratorCommand { get; }

    public async Task RefreshAsync()
    {
        var selectedKey = SelectedEntry?.Model.Key;
        await RunBusyAsync(async () =>
        {
            var entries = await _service.GetAllEntriesAsync().ConfigureAwait(true);
            Entries.Clear();
            foreach (var entry in entries)
            {
                Entries.Add(new StartupEntryViewModel(entry));
            }
            SelectedEntry = Entries.FirstOrDefault(entry => entry.Model.Key.Equals(selectedKey) && MatchesFilter(entry));
            UpdateViewSummary();
            StatusMessage = $"Loaded {Entries.Count} startup entries.";
        }, "Could not load startup entries.").ConfigureAwait(true);
    }

    private async Task AddEntryAsync(AddEntryRequest request)
    {
        await RunBusyAsync(async () =>
        {
            await _service.AddEntryAsync(request.Source, request.Name, request.Command).ConfigureAwait(true);
            StatusMessage = $"Added '{request.Name}'.";
            await RefreshAsync().ConfigureAwait(true);
        }, "Could not add the startup entry.").ConfigureAwait(true);
    }

    private async Task ToggleEnabledAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        var entry = SelectedEntry.Model;
        await RunBusyAsync(async () =>
        {
            if (entry.Status == StartupEntryStatus.Enabled)
            {
                await _service.DisableAsync(entry).ConfigureAwait(true);
                StatusMessage = $"Disabled '{entry.Name}'.";
            }
            else
            {
                await _service.EnableAsync(entry).ConfigureAwait(true);
                StatusMessage = $"Enabled '{entry.Name}'.";
            }
            await RefreshAsync().ConfigureAwait(true);
        }, "Could not change the entry's enabled state.").ConfigureAwait(true);
    }

    private async Task RemoveAsync()
    {
        if (SelectedEntry is null)
        {
            return;
        }

        var entry = SelectedEntry.Model;
        await RunBusyAsync(async () =>
        {
            await _service.RemoveEntryAsync(entry).ConfigureAwait(true);
            StatusMessage = $"Removed '{entry.Name}'.";
            await RefreshAsync().ConfigureAwait(true);
        }, "Could not remove the startup entry.").ConfigureAwait(true);
    }

    private void OpenSourceLocation()
    {
        if (SelectedEntry is null)
        {
            return;
        }
        TryRun(() => _service.OpenSourceLocation(SelectedEntry.Model), "Could not open the source location.");
    }

    private void OpenResolvedLocation()
    {
        if (SelectedEntry is null)
        {
            return;
        }
        TryRun(() => _service.OpenResolvedLocation(SelectedEntry.Model), "Could not open the resolved file location.");
    }

    private void RestartAsAdministrator()
    {
        try
        {
            ErrorMessage = null;
            Process.Start(new ProcessStartInfo(Environment.ProcessPath!) { UseShellExecute = true, Verb = "runas" });
            System.Windows.Application.Current.Shutdown();
        }
        catch (Win32Exception)
        {
            ErrorMessage = "Administrator restart was cancelled.";
        }
    }

    private void TryRun(Action action, string failureMessage)
    {
        try
        {
            ErrorMessage = null;
            action();
        }
        catch (ElevationRequiredException ex)
        {
            ErrorMessage = $"{ex.Message} Restart the app as administrator to perform this action.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{failureMessage} {ex.Message}";
        }
    }

    private async Task RunBusyAsync(Func<Task> action, string failureMessage)
    {
        IsBusy = true;
        ErrorMessage = null;
        try
        {
            await action().ConfigureAwait(true);
        }
        catch (ElevationRequiredException ex)
        {
            ErrorMessage = $"{ex.Message} Click 'Restart as Administrator' to perform this action.";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"{failureMessage} {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }
}
