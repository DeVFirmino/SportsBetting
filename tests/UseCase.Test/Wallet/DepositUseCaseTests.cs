using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Repositories.WalletTransactionRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Repositories;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.Wallet;

public class DepositUseCaseTests
{
    [Fact]
    public async Task ShouldCreateWalletWithDepositedBalanceWhenWalletDoesNotExist()
    {
        // Arrange
        var user = User();
        Domain.Entities.Wallet? createdWallet = null;
        var writeRepository = new Mock<IWalletWriteOnlyRepository>();
        writeRepository.Setup(repository => repository.AddAsync(
                It.IsAny<Domain.Entities.Wallet>(),
                It.IsAny<CancellationToken>()))
            .Callback<Domain.Entities.Wallet, CancellationToken>((wallet, _) => createdWallet = wallet)
            .Returns(Task.CompletedTask);
        var useCase = CreateUseCase(user, null, writeRepository: writeRepository);

        // Act
        await useCase.Execute(new DepositRequest { Amount = 75m }, idempotencyKey: null, CancellationToken.None);

        // Assert
        createdWallet.Should().NotBeNull();
        createdWallet!.UserId.Should().Be(user.Id);
        createdWallet.Balance.Should().Be(75m);
    }

    [Fact]
    public async Task ShouldAddAmountToBalanceWhenWalletExists()
    {
        // Arrange
        var wallet = new Domain.Entities.Wallet { Id = 8, UserId = 42, Balance = 100m };
        var useCase = CreateUseCase(User(), wallet);

        // Act
        await useCase.Execute(new DepositRequest { Amount = 25.50m }, idempotencyKey: null, CancellationToken.None);

        // Assert
        wallet.Balance.Should().Be(125.50m);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task ShouldReturnValidationErrorWhenAmountIsInvalid(decimal amount)
    {
        // Arrange
        var useCase = CreateUseCase(User(), null);

        // Act
        Func<Task> act = () => useCase.Execute(new DepositRequest { Amount = amount }, idempotencyKey: null, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().ContainSingle()
            .Which.Should().Be(ResourcesMessagesException.AMOUNT_INVALID);
    }

    [Fact]
    public async Task ShouldRecordADepositLedgerEntryWhenTheWalletAlreadyExists()
    {
        // Arrange
        var wallet = new Domain.Entities.Wallet { Id = 8, UserId = 42, Balance = 100m };
        var ledger = new WalletTransactionWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(User(), wallet, ledger: ledger);

        // Act
        await useCase.Execute(new DepositRequest { Amount = 25m }, idempotencyKey: null, CancellationToken.None);

        // Assert
        var entry = ledger.Recorded.Should().ContainSingle().Subject;
        entry.Type.Should().Be(Domain.Enums.WalletTransactionType.Deposit);
        entry.Wallet.Id.Should().Be(8);
        entry.Amount.Should().Be(25m);
        entry.BalanceAfter.Should().Be(125m);
        entry.BetId.Should().BeNull();
    }

    [Fact]
    public async Task ShouldRecordADepositLedgerEntryWhenTheWalletIsCreated()
    {
        // Arrange
        var ledger = new WalletTransactionWriteOnlyRepositoryBuilder();
        var useCase = CreateUseCase(User(), null, ledger: ledger);

        // Act
        await useCase.Execute(new DepositRequest { Amount = 75m }, idempotencyKey: null, CancellationToken.None);

        // Assert
        var entry = ledger.Recorded.Should().ContainSingle().Subject;
        entry.Type.Should().Be(Domain.Enums.WalletTransactionType.Deposit);
        entry.Amount.Should().Be(75m);
        entry.BalanceAfter.Should().Be(75m);
    }

    private static DepositUseCase CreateUseCase(
        Domain.Entities.User user,
        Domain.Entities.Wallet? wallet,
        Mock<IWalletWriteOnlyRepository>? writeRepository = null,
        WalletTransactionWriteOnlyRepositoryBuilder? ledger = null)
    {
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var readRepository = new Mock<IWalletReadOnlyRepository>();
        readRepository.Setup(repository => repository.GetByUserIdAsync(
            user.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(wallet!);

        var updateRepository = new Mock<IWalletUpdateOnlyRepository>();
        if (wallet is not null)
        {
            updateRepository.Setup(repository => repository.GetByIdAsync(
                wallet.Id,
                It.IsAny<CancellationToken>())).ReturnsAsync(wallet);
        }

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        return new DepositUseCase(
            (writeRepository ?? new Mock<IWalletWriteOnlyRepository>()).Object,
            loggedUser.Object,
            updateRepository.Object,
            readRepository.Object,
            (ledger ?? new WalletTransactionWriteOnlyRepositoryBuilder()).Build(),
            new WalletTransactionReadOnlyRepositoryBuilder().Build(),
            unitOfWork.Object);
    }

    private static Domain.Entities.User User() => new() { Id = 42, Email = "user@example.com" };
}
