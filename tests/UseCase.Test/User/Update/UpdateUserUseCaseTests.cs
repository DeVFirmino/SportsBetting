using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.User.Update;
using SportsBetting.Communication.Requests;
using SportsBetting.Domain.Repositories;
using SportsBetting.Domain.Repositories.User;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Exceptions;
using SportsBetting.Exceptions.ExceptionBase;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.User.Update;

public class UpdateUserUseCaseTests
{
    [Fact]
    public async Task ShouldUpdateNameAndEmailWhenDataIsValid()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user);
        var request = new UpdateUserRequest { Name = "New name", Email = "new@example.com" };

        // Act
        await useCase.Execute(request, CancellationToken.None);

        // Assert
        user.Name.Should().Be(request.Name);
        user.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task ShouldUpdateProfileWhenEmailIsUnchanged()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user, emailAlreadyExists: true);

        // Act
        await useCase.Execute(
            new UpdateUserRequest { Name = "New name", Email = user.Email },
            CancellationToken.None);

        // Assert
        user.Name.Should().Be("New name");
        user.Email.Should().Be("current@example.com");
    }

    [Fact]
    public async Task ShouldReturnValidationErrorWhenEmailBelongsToAnotherUser()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user, emailAlreadyExists: true);

        // Act
        Func<Task> act = () => useCase.Execute(new UpdateUserRequest
        {
            Name = "New name",
            Email = "registered@example.com"
        }, CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().Contain(ResourcesMessagesException.EMAIL_ALREADY_REGISTERED);
        user.Email.Should().Be("current@example.com");
    }

    [Fact]
    public async Task ShouldReturnBothValidationErrorsWhenFieldsAreEmpty()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user);

        // Act
        Func<Task> act = () => useCase.Execute(new UpdateUserRequest(), CancellationToken.None);

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().BeEquivalentTo(
            ResourcesMessagesException.NAME_EMPTY,
            ResourcesMessagesException.EMAIL_EMPTY);
    }

    private static UpdateUserUseCase CreateUseCase(Domain.Entities.User user, bool emailAlreadyExists = false)
    {
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var updateRepository = new Mock<IUserUpdateOnlyRepository>();
        updateRepository.Setup(repository => repository.GetByIdAsync(
            user.Id,
            It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var readRepository = new Mock<IUserReadOnlyRepository>();
        readRepository.Setup(repository => repository.ExistsActiveUserWithEmailAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emailAlreadyExists);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        return new UpdateUserUseCase(
            loggedUser.Object,
            updateRepository.Object,
            readRepository.Object,
            unitOfWork.Object);
    }

    private static Domain.Entities.User User() => new()
    {
        Id = 9,
        Name = "Current name",
        Email = "current@example.com"
    };
}
