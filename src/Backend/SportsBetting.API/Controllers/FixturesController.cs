using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("fixtures")]
[Authorize]
public sealed class FixturesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(List<FixtureResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailableFixtures(
        [FromServices] IGetAvailableFixtureUseCase useCase,
        CancellationToken cancellationToken)
    {
        List<FixtureResponse> result = await useCase.Execute(cancellationToken);

        return Ok(result);
    }
}
