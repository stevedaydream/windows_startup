# Windows Start Up Manager

## Purpose

Native Windows desktop app for viewing and managing startup entries.

## First-release scope

- Read and group startup entries from `HKCU` / `HKLM` `Run` registry keys and the current-user / common Startup folders.
- Show entry name, source, status, command, resolved executable, application metadata, and publisher where available.
- Add an entry, disable and re-enable it reversibly, remove it, and open its file location.
- Request elevation only for operations that modify machine-wide sources.

## Architecture

- .NET 8 WPF (`net8.0-windows`), nullable reference types enabled, MVVM.
- Solution `WindowsStartupManager.sln` with three projects:
  - `src/WindowsStartupManager.Core` — class library with domain models (`StartupEntry`, `StartupEntryKey`, `StartupEntryRawData`), command-line parsing (`CommandLineParser`), the `IStartupEntrySourceAdapter` abstraction and its `RegistryRunAdapter` / `StartupFolderAdapter` implementations, the `DisabledEntryStore`, and the `StartupManagerService` that orchestrates adapters + the disabled store.
  - `src/WindowsStartupManager.App` — WPF UI (views, view models, `RelayCommand`/`AsyncRelayCommand`), the composition root in `App.xaml.cs`.
  - `tests/WindowsStartupManager.Core.Tests` — xUnit tests for `CommandLineParser` and `DisabledEntryStore`.
- `RegistryRunAdapter` covers HKCU/HKLM `...\CurrentVersion\Run`; `StartupFolderAdapter` covers the current-user/common Startup folders, reading and writing `.lnk` shortcuts via late-bound `WScript.Shell` COM automation (no extra NuGet dependency).
- Disabling an entry captures its raw command (and, for shortcuts, moves the `.lnk` file into an app-managed backup directory) into `DisabledEntryStore` (JSON under `%LocalAppData%\WindowsStartupManager`); enabling restores it and removes the record. Disabled entries are grouped and shown with the enabled ones by `StartupManagerService.GetAllEntriesAsync`.
- Machine-wide mutations (`LocalMachineRegistryRun`, `CommonStartupFolder`) check `ElevationHelper.IsElevated()` before mutating and throw `ElevationRequiredException` when not elevated; the view model surfaces this as an actionable error message instead of failing silently.
- The UI stays responsive via `AsyncRelayCommand` (async command execution with re-entrancy guarded by an `IsExecuting` flag) and a busy/status/error message surface on `MainViewModel`.

## Out of scope for first release

- Startup services and logon-triggered Scheduled Tasks.
- Automatic cloud backup or synchronization.
