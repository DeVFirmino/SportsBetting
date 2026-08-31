using FluentAssertions;
using SportsBetting.Infrastructure.Security.Cryptography;
using DomainUser = SportsBetting.Domain.Entities.User;

namespace UseCase.Test.Security.Cryptography;

public class IdentityPasswordHasherTests
{
    [Fact]
    public void ShouldSucceedWhenTheStoredHashCameFromTheSameHasher()
    {
        // Arrange
        var hasher = new IdentityPasswordHasher();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        // Act
        var verified = hasher.Verify(user, stored, "password123");

        // Assert
        verified.Should().BeTrue();
    }

    [Fact]
    public void ShouldProduceDifferentHashesForTheSamePasswordWhenSaltIsRandom()
    {
        // Arrange
        var hasher = new IdentityPasswordHasher();
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var first = hasher.Hash(user, "password123");
        var second = hasher.Hash(user, "password123");

        // Assert
        first.Should().NotBe(second);
    }

    [Fact]
    public void ShouldFailWhenTheProvidedPasswordIsWrong()
    {
        // Arrange
        var hasher = new IdentityPasswordHasher();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        // Act
        var verified = hasher.Verify(user, stored, "wrong-password");

        // Assert
        verified.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("!!!!")]
    public void ShouldFailWithoutThrowingWhenTheStoredHashIsUnreadable(string stored)
    {
        // Arrange
        var hasher = new IdentityPasswordHasher();
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verified = hasher.Verify(user, stored, "password123");

        // Assert
        verified.Should().BeFalse();
    }
}
