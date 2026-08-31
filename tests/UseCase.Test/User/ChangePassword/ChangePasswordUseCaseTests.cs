using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.User.ChangePassword;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Security.Cryptography;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Cryptography;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.User.ChangePassword;

public class ChangePasswordUseCaseTests
{
    [Fact]
    public async Task ShouldChangePasswordWhenCurrentPasswordIsCorrect()
    {
        // Arrange
        var passwordHasher = PasswordHasherBuilder.Build();
        var user = new Domain.Entities.User
        {
            Id = 7,
        };
        user.Password = passwordHasher.Hash(user, "current-password");
        var useCase = CreateUseCase(user);

        // Act
        await useCase.Execute(new ChangePasswordRequest
        {
            Password = "current-password",
            NewPassword = "new-password"
        }, CancellationToken.None);

        // Assert
        PasswordHasherBuilder.Build().Verify(user, user.Password, "new-password")
            .Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnInvalidCredentialsWhenCurrentPasswordIsIncorrect()
    {
        // Arrange
        var passwordHasher = PasswordHasherBuilder.Build();
        var user = new Domain.Entities.User { Id = 7 };
        user.Password = passwordHasher.Hash(user, "correct-password");
        var useCase = CreateUseCase(user);

        // Act
        Func<Task> act = () => useCase.Execute(new ChangePasswordRequest
        {
            Password = "wrong-password",
            NewPassword = "new-password"
        }, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().Contain(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID);
        PasswordHasherBuilder.Build().Verify(user, user.Password, "correct-password")
            .Should().BeTrue();
    }

    [Fact]
    public async Task ShouldReturnValidationErrorWhenNewPasswordIsTooShort()
    {
        // Arrange
        var passwordHasher = PasswordHasherBuilder.Build();
        var user = new Domain.Entities.User { Id = 7 };
        user.Password = passwordHasher.Hash(user, "current-password");
        var useCase = CreateUseCase(user);

        // Act
        Func<Task> act = () => useCase.Execute(new ChangePasswordRequest
        {
            Password = "current-password",
            NewPassword = "12345"
        }, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().Contain(ResourcesMessagesException.EMAIL_OR_PASSWORD_INVALID);
    }

    private static ChangePasswordUseCase CreateUseCase(Domain.Entities.User user)
    {
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var repository = new Mock<IUserUpdateOnlyRepository>();
        repository.Setup(item => item.GetByIdAsync(
            user.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new ChangePasswordUseCase(
            loggedUser.Object,
            repository.Object,
            unitOfWork.Object,
            PasswordHasherBuilder.Build());
    }
}
