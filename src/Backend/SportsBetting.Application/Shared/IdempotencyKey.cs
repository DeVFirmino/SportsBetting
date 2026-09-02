using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.Shared;

public static class IdempotencyKey
{
    public const int MaxLength = 128;

    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ErrorOnValidationException([ResourcesMessagesException.IDEMPOTENCY_KEY_REQUIRED]);

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new ErrorOnValidationException([ResourcesMessagesException.IDEMPOTENCY_KEY_TOO_LONG]);

        return normalized;
    }
}
