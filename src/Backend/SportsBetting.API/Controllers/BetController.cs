using Microsoft.AspNetCore.Mvc;
using SportsBetting.API.Attributes;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Application.UseCases.Bet.GetUserBets;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

public class BetController : SportsBettingBaseController
{
    [HttpPost("place-bet")]
    [ProducesResponseType(typeof(ResponseBetsJson), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<IActionResult> PlaceBet(
        [FromServices] IPlaceBetUseCase useCase,
        [FromBody] RequestPlaceBetJson request)
    {
        var result = await useCase.Execute(request);
        
        return Created(string.Empty, result);
    }

    [HttpGet("get-bets")]
    [ProducesResponseType(typeof(ResponsePagedListJson<ResponseBetsJson>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [AuthenticatedUser]
    public async Task<IActionResult> GetUserBets(
        [FromServices] IGetUserBetsUseCase useCase,
        [FromQuery] RequestFilterBetsJson request)
    {
        var result = await useCase.Execute(request);
        
        if (result.TotalCount == 0)
            return NoContent();
        
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ResponseBetsJson), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ResponseErrorJson), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<IActionResult> GetBetById(
        [FromServices] IGetBetByIdUseCase useCase,
        [FromRoute] long id)
    {
        var result = await useCase.Execute(id);
        return Ok(result);
    }
}