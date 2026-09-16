# Project bugfix notes

Record platform-specific or repeatedly encountered issues here after they are resolved.

## `MainViewModel` compile error: `ICollectionView` not found

`ICollectionView` and `CollectionViewSource` live in `System.Windows.Data`, which isn't
covered by WPF's default global usings. Fixed by adding `using System.Windows.Data;` to
`src/WindowsStartupManager.App/ViewModels/MainViewModel.cs`.

## `StartupManagerService.DisableAsync` could lose an entry on persistence failure

`DisableAsync` used to call `adapter.CaptureAndRemoveAsync` (which removes the live
Run-key value or moves the `.lnk` file) and then `DisabledEntryStore.AddAsync` to persist
the recovery record. If the store write failed (full disk, permissions, serialization),
the live entry was already gone and nothing restored it, silently breaking the advertised
reversible disable.

Fixed by wrapping the store write in a try/catch: on failure, `DisableAsync` calls
`adapter.RestoreAsync` with the just-captured data before rethrowing the original error,
so the entry re-appears where it started. If the rollback restore also fails, both
errors are reported together via `DisableRollbackFailedException` instead of silently
swallowing either one. Introduced `Services.IDisabledEntryStore` as a persistence seam so
`StartupManagerServiceTests` can exercise the success and rollback paths with a fake
store, without touching the JSON file on disk.
