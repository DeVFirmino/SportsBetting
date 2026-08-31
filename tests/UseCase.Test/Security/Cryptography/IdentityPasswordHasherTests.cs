using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Tests.Common.Cryptography;
using DomainUser = SportsBetting.Domain.Entities.User;

namespace UseCase.Test.Security.Cryptography;

public class IdentityPasswordHasherTests
{
    [Fact]
    public void ShouldSucceedWhenTheStoredHashCameFromTheSameHasher()
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        // Act
        var verified = hasher.Verify(user, stored, "password123");

        // Assert
        verified.Should().Be(PasswordVerificationOutcome.Success);
    }

    [Fact]
    public void ShouldProduceDifferentHashesForTheSamePasswordWhenSaltIsRandom()
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build();
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
        var hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        // Act
        var verified = hasher.Verify(user, stored, "wrong-password");

        // Assert
        verified.Should().Be(PasswordVerificationOutcome.Failed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("!!!!")]
    public void ShouldFailWithoutThrowingWhenTheStoredHashIsUnreadable(string stored)
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verified = hasher.Verify(user, stored, "password123");

        // Assert
        verified.Should().Be(PasswordVerificationOutcome.Failed);
    }

    [Fact]
    public void ShouldRequireARehashWhenTheStoredHashUsesTheRetiredSha512Scheme()
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build(legacyAdditionalKey: "abc1234");
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verified = hasher.Verify(user, LegacySha512("password123", "abc1234"), "password123");

        // Assert
        // The password matched, but only the login flow holds the plain text needed to store a
        // modern hash in its place — hence the distinct outcome.
        verified.Should().Be(PasswordVerificationOutcome.SuccessRehashRequired);
    }

    [Fact]
    public void ShouldFailWhenTheLegacyPasswordIsWrong()
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build(legacyAdditionalKey: "abc1234");
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verified = hasher.Verify(user, LegacySha512("password123", "abc1234"), "wrong-password");

        // Assert
        verified.Should().Be(PasswordVerificationOutcome.Failed);
    }

    [Fact]
    public void ShouldFailWhenTheLegacyPepperDiffersFromTheOneTheHashWasMadeWith()
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build(legacyAdditionalKey: "another-key");
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verified = hasher.Verify(user, LegacySha512("password123", "abc1234"), "password123");

        // Assert
        verified.Should().Be(PasswordVerificationOutcome.Failed);
    }

    [Fact]
    public void ShouldFailWhenTheHashIsLegacyShapedButNoPepperIsConfigured()
    {
        // Arrange
        var hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };

        // Act
        var verified = hasher.Verify(user, LegacySha512("password123", "abc1234"), "password123");

        // Assert
        verified.Should().Be(PasswordVerificationOutcome.Failed);
    }

    private static string LegacySha512(string password, string additionalKey) =>
        Convert.ToHexString(SHA512.HashData(Encoding.UTF8.GetBytes($"{password} {additionalKey}")));
}
