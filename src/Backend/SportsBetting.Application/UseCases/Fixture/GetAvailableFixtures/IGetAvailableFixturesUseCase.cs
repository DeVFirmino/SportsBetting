using SportsBetting.Communication.Responses;

namespace SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;

public interface IGetAvailableFixturesUseCase
{
    Task<List<FixtureResponse>> Execute(CancellationToken cancellationToken);
}
