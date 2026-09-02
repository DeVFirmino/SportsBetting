namespace SportsBetting.Exceptions.ExceptionBase;

public sealed class ResourceNotFoundException : SportsBettingException
{
    public ResourceNotFoundException(string message)
        : base(404, "Not found", [message])
    {
    }
}
