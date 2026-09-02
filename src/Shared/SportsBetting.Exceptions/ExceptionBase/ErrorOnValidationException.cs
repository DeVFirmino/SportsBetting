

namespace SportsBetting.Exceptions.ExceptionBase;

public sealed class ErrorOnValidationException : SportsBettingException
{
    public ErrorOnValidationException(IReadOnlyList<string> errors)
        : base(400, "Validation failed", errors)
    {
    }
}
