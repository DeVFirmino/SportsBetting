using Microsoft.AspNetCore.Mvc;
using SportsBetting.API.Attributes;
using SportsBetting.Application.UseCases.Fixture.GetAvailableFixtures;
using SportsBetting.Communication.Responses;


namespace SportsBetting.API.Controllers;

public class FixturesController : SportsBettingBaseController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<ResponseFixtureJson>), StatusCodes.Status200OK)]
    [AuthenticatedUser]
    public async Task<IActionResult> GetAvailableFixtures(
        [FromServices] IGetAvailableFixtureUseCase useCase)
    {
        var result = await useCase.Execute();
        return Ok(result);
    }
    
    
}