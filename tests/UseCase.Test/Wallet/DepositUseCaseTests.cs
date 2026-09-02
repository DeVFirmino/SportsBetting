using FluentAssertions;
using SportsBetting.Application.UseCases.Wallet.Deposit;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Entities;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.WalletRepository;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using UserEntity = SportsBetting.Domain.Entities.User;
using WalletEntity = SportsBetting.Domain.Entities.Wallet;

namespace UseCase.Test.Wallet;

public sealed class DepositUseCaseTests
{
    [Fact]
    public async Task ShouldAddAmountToWalletWhenDepositIsValid()
    {
        TestContext context = CreateContext(balance: 100m);

        await context.UseCase.Execute(new DepositRequest { Amount = 25m }, CancellationToken.None);

        context.Wallet!.Balance.Should().Be(125m);
    }

    [Fact]
    public async Task ShouldAddEveryDepositWhenSameAmountIsSubmittedTwice()
    {
        TestContext context = CreateContext(balance: 100m);
        DepositRequest request = new() { Amount = 25m };

        await context.UseCase.Execute(request, CancellationToken.None);
        await context.UseCase.Execute(request, CancellationToken.None);

        context.Wallet!.Balance.Should().Be(150m);
        context.UnitOfWork.CommitCount.Should().Be(2);
    }

    [Fact]
    public async Task ShouldReturnWalletNotFoundWhenWalletDoesNotExist()
    {
        TestContext context = CreateContext(walletExists: false);

        Func<Task> act = () => context.UseCase.Execute(
            new DepositRequest { Amount = 25m },
            CancellationToken.None);

        (await act.Should().ThrowAsync<ResourceNotFoundException>())
            .Which.Errors.Should().ContainSingle(ResourcesMessagesException.WALLET_NOT_FOUND);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ShouldReturnValidationErrorWhenAmountIsInvalid(decimal amount)
    {
        TestContext context = CreateContext();

        Func<Task> act = () => context.UseCase.Execute(
            new DepositRequest { Amount = amount },
            CancellationToken.None);

        await act.Should().ThrowAsync<ErrorOnValidationException>();
        context.UnitOfWork.CommitCount.Should().Be(0);
    }

    [Fact]
    public async Task ShouldCommitOnceWhenDepositIsValid()
    {
        TestContext context = CreateContext();

        await context.UseCase.Execute(new DepositRequest { Amount = 25m }, CancellationToken.None);

        context.UnitOfWork.CommitCount.Should().Be(1);
    }

    private static TestContext CreateContext(decimal balance = 100m, bool walletExists = true)
    {
        UserEntity user = new() { Id = 1, UserIdentifier = Guid.NewGuid() };
        WalletEntity? wallet = walletExists
            ? new WalletEntity { Id = 2, UserId = user.Id, Balance = balance }
            : null;
        UnitOfWorkStub unitOfWork = new();
        DepositUseCase useCase = new(
            new LoggedUserStub(user),
            new WalletRepositoryStub(wallet),
            unitOfWork);

        return new TestContext(useCase, wallet, unitOfWork);
    }

    private sealed record TestContext(
        DepositUseCase UseCase,
        WalletEntity? Wallet,
        UnitOfWorkStub UnitOfWork);

    private sealed class LoggedUserStub : ILoggedUser
    {
        private readonly UserEntity _user;

        public LoggedUserStub(UserEntity user)
        {
            _user = user;
        }

        public Task<UserEntity> GetUserAsync(CancellationToken cancellationToken)
        {
            return Task.FromResult(_user);
        }
    }

    private sealed class WalletRepositoryStub : IWalletUpdateOnlyRepository
    {
        private readonly WalletEntity? _wallet;

        public WalletRepositoryStub(WalletEntity? wallet)
        {
            _wallet = wallet;
        }

        public Task<WalletEntity?> GetByUserIdAsync(
            long userId,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(_wallet);
        }
    }

    private sealed class UnitOfWorkStub : IUnitOfWork
    {
        public int CommitCount { get; private set; }

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            CommitCount++;
            return Task.CompletedTask;
        }
    }
}
