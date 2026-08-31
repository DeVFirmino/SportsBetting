namespace SportsBetting.Domain.Security.Cryptography;

public enum PasswordHashVerification
{
    Failed = 0,
    Success = 1,
    SuccessRehashNeeded = 2
}
