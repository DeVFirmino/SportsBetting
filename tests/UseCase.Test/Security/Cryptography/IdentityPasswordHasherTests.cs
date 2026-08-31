using FluentAssertions;
using Microsoft.Extensions.Options;
using SportsBetting.Domain.Security.Cryptography;
using DomainUser = SportsBetting.Domain.Entities.User;
using SportsBetting.Infrastructure.Security.Cryptography;
using PasswordSettings = SportsBetting.Infrastructure.Options.PasswordOptions;

namespace UseCase.Test.Security.Cryptography;

public class IdentityPasswordHasherTests
{
    private const string AdditionalKey = "additional-key";

    [Fact]
    public void ShouldSucceedWhenTheStoredHashCameFromTheIdentityHasher()
    {
        // Arrange
        var hasher = CreateHasher();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        // Act
        var verification = hasher.Verify(user, stored, "password123");

        // Assert
        verification.Should().Be(PasswordHashVerification.Success);
    }

    [Fact]
    public void ShouldProduceDifferentHashesForTheSamePasswordWhenSaltIsRandom()
    {
        // Arrange
        var hasher = CreateHasher();
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var first = hasher.Hash(user, "password123");
        var second = hasher.Hash(user, "password123");

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void ShouldRequestRehashWhenTheStoredHashIsALegacySha512Hash()
    {
        // Arrange
        var hasher = CreateHasher();
        var user = new DomainUser { Email = "user@example.com" };
        var legacy = new Sha512Encrypter(AdditionalKey).Encrypt("password123");

        // Act
        var verification = hasher.Verify(user, legacy, "password123");

        // Assert
        verification.Should().Be(PasswordHashVerification.SuccessRehashNeeded);
    }

    [Fact]
    public void ShouldFailWhenTheLegacyHashDoesNotMatchTheProvidedPassword()
    {
        // Arrange
        var hasher = CreateHasher();
        var user = new DomainUser { Email = "user@example.com" };
        var legacy = new Sha512Encrypter(AdditionalKey).Encrypt("password123");

        // Act
        var verification = hasher.Verify(user, legacy, "wrong-password");

        // Assert
        verification.Should().Be(PasswordHashVerification.Failed);
    }

    [Fact]
    public void ShouldFailWhenTheProvidedPasswordIsWrong()
    {
        // Arrange
        var hasher = CreateHasher();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        // Act
        var verification = hasher.Verify(user, stored, "wrong-password");

        // Assert
        verification.Should().Be(PasswordHashVerification.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("!!!!")]
    public void ShouldFailWithoutThrowingWhenTheStoredHashIsUnreadable(string stored)
    {
        // Arrange
        var hasher = CreateHasher();
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verification = hasher.Verify(user, stored, "password123");

        // Assert
        verification.Should().Be(PasswordHashVerification.Failed);
    }

    private static IdentityPasswordHasher CreateHasher()
    {
        return new IdentityPasswordHasher(
            Options.Create(new PasswordSettings { AdditionalKey = AdditionalKey }));
    }
}
