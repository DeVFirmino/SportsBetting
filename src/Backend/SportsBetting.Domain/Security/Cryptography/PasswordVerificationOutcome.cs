namespace SportsBetting.Domain.Security.Cryptography;

public enum PasswordVerificationOutcome
{
    Failed = 0,
    Success = 1,

    /// <summary>
    /// The password matched, but the stored hash uses an outdated scheme and should be replaced
    /// with a current one while the plain-text password is available — which is only during a
    /// successful verification.
    /// </summary>
    SuccessRehashRequired = 2
}
