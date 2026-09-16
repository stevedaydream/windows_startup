namespace WindowsStartupManager.Core;

/// <summary>
/// Thrown when disabling an entry fails to persist its recovery record and the
/// subsequent attempt to restore the live entry (so the user doesn't lose it) also
/// fails. Carries both underlying errors so the caller can surface the entry's
/// current, possibly inconsistent, state to the user.
/// </summary>
public sealed class DisableRollbackFailedException : Exception
{
    public DisableRollbackFailedException(Exception persistException, Exception rollbackException)
        : base(
            $"Could not save the disabled entry ({persistException.Message}), and restoring it also failed " +
            $"({rollbackException.Message}). The entry may be missing from its original location; check manually.",
            persistException)
    {
        PersistException = persistException;
        RollbackException = rollbackException;
    }

    public Exception PersistException { get; }

    public Exception RollbackException { get; }
}
