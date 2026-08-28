using FluentAssertions;
using SportsBetting.Infrastructure.Security.Cryptography;

namespace UseCase.Test.Security.Cryptography;

public class Sha512EncrypterTests
{
    [Fact]
    public void ShouldReturnStableHashWhenPasswordAndKeyAreSame()
    {
        // Arrange
        var encrypter = new Sha512Encrypter("additional-key");

        // Act
        var first = encrypter.Encrypt("password");
        var second = encrypter.Encrypt("password");

        // Assert
        first.Should().Be(second);
        first.Should().HaveLength(128);
    }

    [Fact]
    public void ShouldReturnDifferentHashWhenAdditionalKeyDiffers()
    {
        // Arrange
        var firstEncrypter = new Sha512Encrypter("first-key");
        var secondEncrypter = new Sha512Encrypter("second-key");

        // Act
        var first = firstEncrypter.Encrypt("password");
        var second = secondEncrypter.Encrypt("password");

        // Assert
        first.Should().NotBe(second);
    }
}
