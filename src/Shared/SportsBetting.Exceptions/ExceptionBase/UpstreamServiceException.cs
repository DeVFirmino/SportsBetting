namespace SportsBetting.Exceptions.ExceptionBase;

public sealed class UpstreamServiceException : SportsBettingException
{
    public UpstreamServiceException(int statusCode, string message)
        : base(statusCode, "Upstream service unavailable", [message])
    {
        if (statusCode is not 502 and not 503)
            throw new ArgumentOutOfRangeException(nameof(statusCode));
    }
}
