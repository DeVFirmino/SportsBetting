namespace SportsBetting.Exceptions.ExceptionBase;

public sealed class InvalidLoginException : SportsBettingException
{
    public InvalidLoginException()
        : base(401, "Unauthorized", [ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID])
    {
    }
}
