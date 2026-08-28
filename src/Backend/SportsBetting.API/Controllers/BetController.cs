using Microsoft.AspNetCore.Mvc;
using SportsBetting.API.Attributes;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Application.UseCases.Bet.GetUserBets;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("Bet")]
public sealed class BetController : ControllerBase
{
    [HttpPost("place-bet")]
    [ProducesResponseType(typeof(BetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<IActionResult> PlaceBet(
        [FromServices] IPlaceBetUseCase useCase,
        [FromBody] PlaceBetRequest request,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request, cancellationToken);
        
        return Created(string.Empty, result);
    }

    [HttpGet("get-bets")]
    [ProducesResponseType(typeof(PagedResponse<BetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [AuthenticatedUser]
    public async Task<IActionResult> GetUserBets(
        [FromServices] IGetUserBetsUseCase useCase,
        [FromQuery] GetUserBetsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(request, cancellationToken);
        
        if (result.TotalCount == 0)
            return NoContent();
        
        return Ok(result);
    }
    
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(BetResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [AuthenticatedUser]
    public async Task<IActionResult> GetBetById(
        [FromServices] IGetBetByIdUseCase useCase,
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(id, cancellationToken);
        return Ok(result);
    }
}
