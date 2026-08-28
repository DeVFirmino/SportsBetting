using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;

public interface IGetAvailableFixtureUseCase
{
    Task<List<FixtureResponse>> Execute(CancellationToken cancellationToken);
}
