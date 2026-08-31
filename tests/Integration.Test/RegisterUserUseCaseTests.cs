using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;
using SportsBetting.Domain.Entities;
using SportsBetting.Infrastructure.DataAccess;

namespace Integration.Test;

/// <summary>
/// Registration writes a user and its wallet in one commit. The wallet's foreign key only exists
/// once the user row is written, so the ordering is a SQL Server constraint that an in-memory
/// provider cannot show.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class RegisterUserUseCaseTests
{
    private readonly SqlServerFixture _fixture;

    public RegisterUserUseCaseTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ShouldPersistTheUserAndItsWalletWhenRegistrationSucceeds()
    {
        string email = $"{Guid.NewGuid():N}@example.com";

        await using ServiceProvider provider = _fixture.BuildProvider(new User(), new FootballApiStub());
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        IRegisterUserUseCase useCase = scope.ServiceProvider.GetRequiredService<IRegisterUserUseCase>();

        AuthenticatedUserResponse response = await useCase.Execute(
            new RegisterUserRequest { Name = "Integration Tester", Email = email, Password = "Password123!" },
            CancellationToken.None);

        response.Tokens.AccessToken.Should().NotBeNullOrEmpty();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        User persisted = await verification.Users.AsNoTracking().FirstAsync(user => user.Email == email);
        persisted.Password.Should().NotBe("Password123!");

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.UserId == persisted.Id);
        wallet.Balance.Should().Be(0m);
    }
}
