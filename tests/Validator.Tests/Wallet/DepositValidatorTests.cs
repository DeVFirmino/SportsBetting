using FluentAssertions;
using FluentValidation.Results;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.Wallet;

public class DepositValidatorTests
{
    [Fact]
    public void ShouldBeValidWhenAmountIsPositive()
    {
        var validator = new DepositValidator();
        var request = new DepositRequest { Amount = 25.50m };

        ValidationResult result = validator.Validate(request);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-0.01)]
    [InlineData(-100)]
    public void ShouldReturnAmountInvalidWhenAmountIsNotPositive(decimal amount)
    {
        var validator = new DepositValidator();
        var request = new DepositRequest { Amount = amount };

        ValidationResult result = validator.Validate(request);

        result.Errors.Should().ContainSingle()
            .Which.ErrorMessage.Should().Be(ResourcesMessagesException.AMOUNT_INVALID);
    }
}
