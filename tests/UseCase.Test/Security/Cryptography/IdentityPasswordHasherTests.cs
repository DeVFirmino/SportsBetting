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
        IPasswordHasher hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        var verified = hasher.Verify(user, stored, "password123");

        verified.Should().BeTrue();
    }

    [Fact]
    public void ShouldProduceDifferentHashesForTheSamePasswordWhenSaltIsRandom()
    {
        IPasswordHasher hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };

        var first = hasher.Hash(user, "password123");
        var second = hasher.Hash(user, "password123");

        first.Should().NotBe(second);
    }

    [Fact]
    public void ShouldFailWhenTheProvidedPasswordIsWrong()
    {
        IPasswordHasher hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };
        var stored = hasher.Hash(user, "password123");

        var verified = hasher.Verify(user, stored, "wrong-password");

        verified.Should().BeFalse();
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("!!!!")]
    public void ShouldFailWithoutThrowingWhenTheStoredHashIsUnreadable(string stored)
    {
        IPasswordHasher hasher = PasswordHasherBuilder.Build();
        var user = new DomainUser { Email = "user@example.com" };

        var verified = hasher.Verify(user, stored, "password123");

        verified.Should().BeFalse();
    }
}
