# Project API

No external API in the first release.

Internal surface, in `WindowsStartupManager.Core`:

- `Adapters.IStartupEntrySourceAdapter` — implemented by `RegistryRunAdapter` (HKCU/HKLM Run keys) and `StartupFolderAdapter` (current-user/common Startup folders). Exposes `GetEntriesAsync`, `AddAsync(name, command)`, `RemoveAsync(key)`, `CaptureAndRemoveAsync(key)` / `RestoreAsync(data)` (the disable/enable primitive pair), and `OpenSourceLocation(key)`. Mutating members on machine-wide adapters throw `ElevationRequiredException` when the process is not elevated.
- `Services.StartupManagerService` — the facade the UI talks to: `GetAllEntriesAsync` (merges live adapter entries with disabled records), `AddEntryAsync`, `RemoveEntryAsync`, `DisableAsync`, `EnableAsync`, `OpenSourceLocation`, `OpenResolvedLocation`. `DisableAsync` is failure-safe: it captures-and-removes the live entry via the adapter, then persists the recovery record through `IDisabledEntryStore`; if persistence throws, it restores the entry through the same adapter before rethrowing the original exception, and throws `DisableRollbackFailedException` (carrying both `PersistException` and `RollbackException`) if the rollback itself also fails.
- `Services.IDisabledEntryStore` — persistence seam for disabled-entry records (`GetAllAsync`, `TryGetAsync`, `AddAsync`, `RemoveAsync`), keyed by `(StartupEntrySource, Identifier)`, implemented by `DisabledEntryStore` (JSON-backed) and fakeable in tests.
- `Services.DisabledEntryStore` — JSON-backed `IDisabledEntryStore` implementation for disabled entries.
- `DisableRollbackFailedException` — thrown by `StartupManagerService.DisableAsync` when both persisting the disabled record and rolling back the live entry fail; exposes `PersistException` and `RollbackException`.
- `Services.ElevationHelper` — `IsElevated()` / `RelaunchElevated()`.
- `Parsing.CommandLineParser` — `SplitArguments` (CommandLineToArgvW-compatible quoting/escaping), `ExtractExecutablePath` (handles quoted paths and unquoted paths containing spaces), `ExpandEnvironmentVariables`.
- `Metadata.StartupEntryFactory` — builds a `Models.StartupEntry` from a raw command, resolving the executable and reading `FileVersionInfo` (product name, application/file description, publisher, version); failures become a `Notes` string rather than an exception.
- `Interop.ShellLinkService` — reads/writes `.lnk` shortcuts via late-bound `WScript.Shell` COM automation.
