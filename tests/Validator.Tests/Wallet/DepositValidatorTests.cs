using FluentAssertions;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.Wallet;

public class DepositValidatorTests
{
    [Fact]
    public void Validate_WithPositiveAmount_IsValid()
    {
        // Arrange
        var validator = new DepositValidator();
        var request = new DepositRequest { Amount = 25.50m };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void Validate_WithNonPositiveAmount_ReturnsAmountInvalid(decimal amount)
    {
        // Arrange
        var validator = new DepositValidator();
        var request = new DepositRequest { Amount = amount };

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.AMOUNT_INVALID);
    }
}
