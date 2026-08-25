namespace SportsBetting.Exceptions.ExceptionBase;

/// <summary>
/// Raised when a write loses an optimistic concurrency check — the row moved between the read
/// and the save. The request was valid, so the caller is expected to retry rather than correct it.
/// </summary>
public class ConcurrencyException : SportsBettingException
{
    public ConcurrencyException() : base(ResourcesMessagesException.CONCURRENT_BET_DETECTED)
    {
    }

    public ConcurrencyException(string message) : base(message)
    {
    }
}
