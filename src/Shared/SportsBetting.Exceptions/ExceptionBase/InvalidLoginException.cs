namespace SportsBetting.Exceptions.ExceptionBase;

public class InvalidLoginException : SportsBettingException
{
    public InvalidLoginException() : base(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID)
    {
    }
}