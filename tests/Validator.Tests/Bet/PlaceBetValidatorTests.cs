using FluentAssertions;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions;

namespace SportsBetting.Tests.Bet;

public class PlaceBetValidatorTests
{
    [Theory]
    [InlineData("HomeWin")]
    [InlineData("Draw")]
    [InlineData("AwayWin")]
    public void Validate_WithSupportedBetType_IsValid(string betType)
    {
        // Arrange
        var validator = new PlaceBetValidator();
        var request = ValidRequest();
        request.BetType = betType;

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DoubleChance")]
    [InlineData("homewin")]
    public void Validate_WithUnsupportedBetType_ReturnsBetTypeRequired(string? betType)
    {
        // Arrange
        var validator = new PlaceBetValidator();
        var request = ValidRequest();
        request.BetType = betType;

        // Act
        var result = validator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error =>
            error.ErrorMessage == ResourcesMessagesException.BET_TYPE_REQUIRED);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WithNonPositiveAmount_ReturnsAmountError(decimal amount)
    {
        // Arrange
        var validator = new PlaceBetValidator();
        var request = ValidRequest();
        request.Amount = amount;

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == ResourcesMessagesException.BET_AMOUNT_GREATER_THAN_ZERO);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Validate_WithInvalidFixtureId_ReturnsFixtureNotFound(int fixtureId)
    {
        // Arrange
        var validator = new PlaceBetValidator();
        var request = ValidRequest();
        request.FixtureId = fixtureId;

        // Act
        var result = validator.Validate(request);

        // Assert
        result.Errors.Should().ContainSingle(error =>
            error.ErrorMessage == ResourcesMessagesException.FIXTURE_NOT_FOUND);
    }

    private static RequestPlaceBetJson ValidRequest() => new()
    {
        FixtureId = 123,
        Amount = 20m,
        BetType = "HomeWin"
    };
}
