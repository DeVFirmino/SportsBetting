using FluentAssertions;
using Microsoft.IdentityModel.Tokens;
using SportsBetting.Infrastructure.Security.Tokens.Access.Generator;
using SportsBetting.Infrastructure.Security.Tokens.Access.Validator;

namespace UseCase.Test.Security.Tokens.Access;

public class JwtTokenValidatorTest
{
    private const string SigningKey = "a-signing-key-that-is-at-least-32-bytes-long";

    [Fact]
    public void ValidateAndGetUserIdentifier_WithValidToken_ReturnsIdentifier()
    {
        // Arrange
        var identifier = Guid.NewGuid();
        var token = new JwtTokenGenerator(5, SigningKey).Generate(identifier);
        var validator = new JwtTokenValidator(SigningKey);

        // Act
        var result = validator.ValidateAndGetUserIdentifier(token);

        // Assert
        result.Should().Be(identifier);
    }

    [Fact]
    public void ValidateAndGetUserIdentifier_WithDifferentSigningKey_RejectsToken()
    {
        // Arrange
        var token = new JwtTokenGenerator(5, SigningKey).Generate(Guid.NewGuid());
        var validator = new JwtTokenValidator("a-different-signing-key-of-32-bytes-minimum");

        // Act
        Action act = () => validator.ValidateAndGetUserIdentifier(token);

        // Assert
        act.Should().Throw<SecurityTokenInvalidSignatureException>();
    }

    [Fact]
    public void ValidateAndGetUserIdentifier_WithMalformedToken_RejectsToken()
    {
        // Arrange
        var validator = new JwtTokenValidator(SigningKey);

        // Act
        Action act = () => validator.ValidateAndGetUserIdentifier("not-a-jwt");

        // Assert
        act.Should().Throw<SecurityTokenMalformedException>();
    }
}
