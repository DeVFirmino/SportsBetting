namespace SportsBetting.Exceptions.ExceptionBase;

public sealed class ConcurrencyException : SportsBettingException
{
    public ConcurrencyException()
        : this(ResourcesMessagesException.CONCURRENT_BET_DETECTED)
    {
    }

    public ConcurrencyException(string message)
        : base(409, "Conflict", [message])
    {
    }
}
