using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.User.GetBalance;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.User.GetBalance;

public class GetBalanceUseCaseTests
{
    [Fact]
    public async Task Execute_WithWallet_ReturnsCurrentBalance()
    {
        // Arrange
        var useCase = CreateUseCase(new Domain.Entities.Wallet { UserId = 5, Balance = 345.67m });

        // Act
        var result = await useCase.Execute(CancellationToken.None);

        // Assert
        result.Balance.Should().Be(345.67m);
    }

    [Fact]
    public async Task Execute_WithoutWallet_ReturnsZeroBalance()
    {
        // Arrange
        var useCase = CreateUseCase(null);

        // Act
        var result = await useCase.Execute(CancellationToken.None);

        // Assert
        result.Balance.Should().Be(0m);
    }

    private static GetBalanceUseCase CreateUseCase(Domain.Entities.Wallet? wallet)
    {
        var user = new Domain.Entities.User { Id = 5 };
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var repository = new Mock<IWalletReadOnlyRepository>();
        repository.Setup(item => item.GetByUserIdAsync(
            user.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(wallet!);

        return new GetBalanceUseCase(repository.Object, loggedUser.Object);
    }
}
