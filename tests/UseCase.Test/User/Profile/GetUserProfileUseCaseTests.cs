using FluentAssertions;
using Moq;
using SportsBetting.Application.UseCases.User.Profile;
using SportsBetting.Domain.Services.LoggedUser;
using SportsBetting.Tests.Common.Mapper;
using Domain = SportsBetting.Domain;

namespace UseCase.Test.User.Profile;

public class GetUserProfileUseCaseTests
{
    [Fact]
    public async Task ShouldReturnMappedProfileWhenUserIsLoggedIn()
    {
        // Arrange
        var user = new Domain.Entities.User { Name = "Grace", Email = "grace@example.com" };
        var loggedUser = new Mock<ILoggedUser>();
        loggedUser.Setup(service => service.GetUserAsync(It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var useCase = new GetUserProfileUseCase(loggedUser.Object, MapperBuilder.Build());

        // Act
        var result = await useCase.Execute(CancellationToken.None);

        // Assert
        result.Name.Should().Be(user.Name);
        result.Email.Should().Be(user.Email);
    }
}
