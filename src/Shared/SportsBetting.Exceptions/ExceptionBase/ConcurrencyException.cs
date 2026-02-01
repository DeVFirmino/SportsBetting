namespace SportsBetting.Exceptions.ExceptionBase;

public class ConcurrencyException : SportsBettingException
{
    public ConcurrencyException(string message) : base(ResourcesMessagesException.WALLET_NOT_FOUND)
    {
    }
}