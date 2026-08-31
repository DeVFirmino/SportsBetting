using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SportsBetting.Application.UseCases.User.Register;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Entities;
using SportsBetting.Infrastructure.DataAccess;
using SportsBetting.Infrastructure.DataAccess.Repositories;
using SportsBetting.Tests.Common.Cryptography;
using SportsBetting.Tests.Common.Mapper;
using SportsBetting.Tests.Common.Tokens;

namespace Integration.Test;

/// <summary>
/// Registration inserts a user and its wallet in one commit. The wallet's foreign key only exists
/// after the user row is written, so the ordering is a real SQL Server constraint and an in-memory
/// provider cannot show whether it holds.
/// </summary>
[Collection(SqlServerCollection.Name)]
public sealed class RegisterUserTests
{
    private readonly SqlServerFixture _fixture;

    public RegisterUserTests(SqlServerFixture fixture)
    {
        _fixture = fixture;
    }

    [SqlServerFact]
    public async Task ShouldPersistTheUserAndItsWalletWhenRegistrationSucceeds()
    {
        // Arrange
        string email = $"{Guid.NewGuid():N}@example.com";

        await using SportsBettingDbContext context = _fixture.NewContext();
        UserRepository users = new(context);

        RegisterUserUseCase useCase = new(
            users,
            users,
            MapperBuilder.Build(),
            PasswordHasherBuilder.Build(),
            new UnitOfWork(context),
            JwtTokenGeneratorBuilder.Build(),
            new WalletRepository(context));

        var request = new RegisterUserRequest
        {
            Name = "Integration Tester",
            Email = email,
            Password = "Password123!",
        };

        // Act
        var response = await useCase.Execute(request, CancellationToken.None);

        // Assert
        response.Tokens.AccessToken.Should().NotBeNullOrEmpty();

        await using SportsBettingDbContext verification = _fixture.NewContext();

        User persisted = await verification.Users.AsNoTracking().FirstAsync(user => user.Email == email);
        persisted.Password.Should().NotBe("Password123!");

        Wallet wallet = await verification.Wallets.AsNoTracking().FirstAsync(entity => entity.UserId == persisted.Id);
        wallet.Balance.Should().Be(0m);
    }
}
