namespace SportsBetting.Exceptions.ExceptionBase;

public sealed class IdempotencyConflictException : SportsBettingException
{
    public IdempotencyConflictException()
        : base(409, "Conflict", [ResourcesMessagesException.IDEMPOTENCY_KEY_REUSED])
    {
    }
}
