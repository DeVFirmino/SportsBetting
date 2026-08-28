using CommonTestsUtilities.Entities;
using CommonTestsUtilities.LoggedUser;
using CommonTestsUtilities.Repositories;
using FluentAssertions;
using SportsBetting.Application.UseCases.Bet.GetUserBets;
using SportsBetting.Communication.Requests;
using SportsBetting.Exceptions.ExceptionBase;
using SportsBetting.Tests.Common.Entities;
using SportsBetting.Tests.Common.Mapper;

namespace UseCase.Test.Bet;

public class GetUserBetsUseCaseTest
{
    [Fact]
    public async Task Success()
    {
        (var user, _) = UserBuilder.Build();
        var bets = BetBuilder.Collection(5, user.Id);
        var request = new GetUserBetsRequest
        {
            PageNumber = 1,
            PageSize = 10,
            Status = "Pending"
        };

        var useCase = CreateUseCase(user, bets, totalCount: 15);

        var result = await useCase.Execute(request, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(5);
        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
        result.TotalCount.Should().Be(15);
        result.TotalPages.Should().Be(2);
        result.HasPreviousPage.Should().BeFalse();
        result.HasNextPage.Should().BeTrue();
    }

    [Fact]
    public async Task Error_Invalid_User()
    {
        var request = new GetUserBetsRequest();
        var useCase = CreateUseCase(user: null);

        Func<Task> action = async () => await useCase.Execute(request, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidLoginException>();
    }

    private static GetUserBetsUseCase CreateUseCase(
        SportsBetting.Domain.Entities.User? user = null,
        List<SportsBetting.Domain.Entities.Bet>? bets = null,
        int totalCount = 0)
    {
        var mapper = MapperBuilder.Build();
        var loggedUserBuilder = new LoggedUserBuilder();
        var repositoryBuilder = new BetReadOnlyRepositoryBuilder();

        if (user is not null)
        {
            loggedUserBuilder.User(user);
        }

        if (bets is not null)
        {
            repositoryBuilder.GetPagedByUserIdAsync(bets, totalCount);
        }

        return new GetUserBetsUseCase(loggedUserBuilder.Build(), repositoryBuilder.Build(), mapper);
    }
}
