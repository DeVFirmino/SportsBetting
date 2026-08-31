using Moq;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Security.Cryptography;

namespace SportsBetting.Tests.Common.Cryptography;

/// <summary>
/// A hasher double for tests that assert *whether* hashing work happened — such as the login
/// timing equaliser — rather than its outcome. <see cref="PasswordHasherBuilder"/> remains the
/// choice whenever real hashing behavior is under test.
/// </summary>
public sealed class PasswordHasherMockBuilder
{
    private readonly Mock<IPasswordHasher> _hasher = new();

    public PasswordHasherMockBuilder()
    {
        _hasher
            .Setup(hasher => hasher.Hash(It.IsAny<User>(), It.IsAny<string>()))
            .Returns("hashed");

        _hasher
            .Setup(hasher => hasher.Verify(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(PasswordVerificationOutcome.Failed);
    }

    public PasswordHasherMockBuilder VerifyWasInvokedOnce()
    {
        _hasher.Verify(
            hasher => hasher.Verify(It.IsAny<User>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);

        return this;
    }

    public IPasswordHasher Build() => _hasher.Object;
}
