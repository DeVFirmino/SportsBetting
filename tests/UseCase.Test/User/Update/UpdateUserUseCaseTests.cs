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
    public async Task Execute_WithValidData_UpdatesNameAndEmail()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user);
        var request = new RequestUpdateUserJson { Name = "New name", Email = "new@example.com" };

        // Act
        await useCase.Execute(request);

        // Assert
        user.Name.Should().Be(request.Name);
        user.Email.Should().Be(request.Email);
    }

    [Fact]
    public async Task Execute_WithSameEmail_UpdatesProfile()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user, emailAlreadyExists: true);

        // Act
        await useCase.Execute(new RequestUpdateUserJson { Name = "New name", Email = user.Email });

        // Assert
        user.Name.Should().Be("New name");
        user.Email.Should().Be("current@example.com");
    }

    [Fact]
    public async Task Execute_WithEmailUsedByAnotherUser_ReturnsValidationError()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user, emailAlreadyExists: true);

        // Act
        Func<Task> act = () => useCase.Execute(new RequestUpdateUserJson
        {
            Name = "New name",
            Email = "registered@example.com"
        });

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().Contain(ResourcesMessagesException.EMAIL_ALREADY_REGISTERED);
        user.Email.Should().Be("current@example.com");
    }

    [Fact]
    public async Task Execute_WithEmptyFields_ReturnsBothValidationErrors()
    {
        // Arrange
        var user = User();
        var useCase = CreateUseCase(user);

        // Act
        Func<Task> act = () => useCase.Execute(new RequestUpdateUserJson());

        // Assert
        var exception = await act.Should().ThrowAsync<ErrorOnValidationException>();
        exception.Which.ErrorMessage.Should().BeEquivalentTo(
            ResourcesMessagesException.NAME_EMPTY,
            ResourcesMessagesException.EMAIL_EMPTY);
    }

    private static UpdateUserUseCase CreateUseCase(Domain.Entities.User user, bool emailAlreadyExists = false)
    {
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.User()).ReturnsAsync(user);

        var updateRepository = new Mock<IUserUpdateOnlyRepository>();
        updateRepository.Setup(repository => repository.GetById(user.Id)).ReturnsAsync(user);

        var readRepository = new Mock<IUserReadOnlyRepository>();
        readRepository.Setup(repository => repository.ExistActiveUserWithEmail(It.IsAny<string>()))
            .ReturnsAsync(emailAlreadyExists);

        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(work => work.Commit()).Returns(Task.CompletedTask);

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
