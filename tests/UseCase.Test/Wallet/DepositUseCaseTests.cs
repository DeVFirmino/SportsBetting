using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.Wallet;

public class DepositUseCaseTests
{
    [Fact]
    public async Task Execute_WithoutExistingWallet_CreatesWalletWithDepositedBalance()
    {
        // Arrange
        var user = User();
        Domain.Entities.Wallet? createdWallet = null;
        var writeRepository = new Mock<IWalletWriteOnlyRepository>();
        writeRepository.Setup(repository => repository.Add(It.IsAny<Domain.Entities.Wallet>()))
            .Callback<Domain.Entities.Wallet>(wallet => createdWallet = wallet)
            .Returns(Task.CompletedTask);
        var useCase = CreateUseCase(user, null, writeRepository: writeRepository);

        // Act
        await useCase.Execute(new RequestDepositJson { Amount = 75m });

        // Assert
        createdWallet.Should().NotBeNull();
        createdWallet!.UserId.Should().Be(user.Id);
        createdWallet.Balance.Should().Be(75m);
    }

    [Fact]
    public async Task Execute_WithExistingWallet_AddsAmountToBalance()
    {
        // Arrange
        var wallet = new Domain.Entities.Wallet { Id = 8, UserId = 42, Balance = 100m };
        var useCase = CreateUseCase(User(), wallet);

        // Act
        await useCase.Execute(new RequestDepositJson { Amount = 25.50m });

        // Assert
        wallet.Balance.Should().Be(125.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Execute_WithInvalidAmount_ReturnsValidationError(decimal amount)
    {
        // Arrange
        var useCase = CreateUseCase(User(), null);

        // Act
        Func<Task> act = () => useCase.Execute(new RequestDepositJson { Amount = amount });

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().ContainSingle()
            .Which.Should().Be(ResourcesMessagesException.AMOUNT_INVALID);
    }

    private static DepositUseCase CreateUseCase(
        Domain.Entities.User user,
        Domain.Entities.Wallet? wallet,
        Mock<IWalletWriteOnlyRepository>? writeRepository = null)
    {
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.User()).ReturnsAsync(user);

        var readRepository = new Mock<IWalletReadOnlyRepository>();
        readRepository.Setup(repository => repository.GetByUserId(user.Id)).ReturnsAsync(wallet!);

        var updateRepository = new Mock<IWalletUpdateOnlyRepository>();
        if (wallet is not null)
        {
            updateRepository.Setup(repository => repository.GetById(wallet.Id)).ReturnsAsync(wallet);
        }

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.Commit()).Returns(Task.CompletedTask);

        return new DepositUseCase(
            (writeRepository ?? new Mock<IWalletWriteOnlyRepository>()).Object,
            loggedUser.Object,
            updateRepository.Object,
            readRepository.Object,
            unitOfWork.Object);
    }

    private static Domain.Entities.User User() => new() { Id = 42, Email = "user@example.com" };
}
