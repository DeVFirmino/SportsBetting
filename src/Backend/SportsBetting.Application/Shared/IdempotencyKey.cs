using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;

namespace SportsBetting.Application.Shared;

/// <summary>
/// The client-supplied key that makes a write replayable. Both the betting and the deposit use
/// case read it from the same header, so the length rule that the column enforces lives in one
/// place — without it an over-long key reaches SQL Server and a client mistake becomes a 500.
/// </summary>
public static class IdempotencyKey
{
    public const int MaxLength = 128;

    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        string normalized = value.Trim();

        if (normalized.Length > MaxLength)
            throw new ErrorOnValidationException([ResourcesMessagesException.IDEMPOTENCY_KEY_TOO_LONG]);

        return normalized;
    }
}
