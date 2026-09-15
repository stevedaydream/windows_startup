# Windows Start Up Manager

## Purpose

Native Windows desktop app for viewing and managing startup entries.

## First-release scope

- Read and group startup entries from `HKCU` / `HKLM` `Run` registry keys and the current-user / common Startup folders.
- Show entry name, source, status, command, resolved executable, application metadata, and publisher where available.
- Add an entry, disable and re-enable it reversibly, remove it, and open its file location.
- Request elevation only for operations that modify machine-wide sources.

## Architecture

- .NET 8 WPF, using MVVM.
- A source adapter owns enumeration and mutations for each Windows startup source.
- A shared domain model feeds the UI; command handling stays in application services.
- Disabled entries are kept in an app-managed reversible store rather than deleted.

## Out of scope for first release

- Startup services and logon-triggered Scheduled Tasks.
- Automatic cloud backup or synchronization.
