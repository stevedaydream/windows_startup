# Project bugfix notes

Record platform-specific or repeatedly encountered issues here after they are resolved.

## `MainViewModel` compile error: `ICollectionView` not found

`ICollectionView` lives in `System.ComponentModel`, while `CollectionViewSource` and
`PropertyGroupDescription` live in `System.Windows.Data`; neither is covered by WPF's
default global usings. Fixed by adding `using System.ComponentModel;` and
`using System.Windows.Data;` to
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

The rollback restore is called with `CancellationToken.None`, not the caller's token.
The original bug: if the store write failed because the caller's token had just been
cancelled (`OperationCanceledException`), passing that same cancelled token to
`adapter.RestoreAsync` could make the compensating restore abort immediately too,
leaving the live entry removed with no disabled-record fallback — exactly the data loss
the rollback exists to prevent. `DisableAsync` still rethrows the original
`OperationCanceledException` once the restore (run with a live token) succeeds.

## DataGrid columns start collapsed to a few pixels

`MainWindow.xaml` gave fixed widths to every column except `Notes` (`Width="*"`). When the
window's initial width was smaller than the fixed columns' total (e.g. the window exceeded
the screen), WPF's DataGrid shrank the non-star columns down to their minimum width to make
room for the star column, and never grew them back. Fixed by giving `Notes` a fixed width
too, so the grid scrolls horizontally instead of redistributing.
