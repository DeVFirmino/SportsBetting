using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using SportsBetting.Application.UseCases.Bet.GetBetsById;
using SportsBetting.Application.UseCases.Bet.GetUserBets;
using SportsBetting.Application.UseCases.Bet.PlaceBet;
using SportsBetting.Communication.Requests;
using SportsBetting.Communication.Responses;

namespace SportsBetting.API.Controllers;

[ApiController]
[Route("Bet")]
[Authorize]
public sealed class BetController : ControllerBase
{
    [HttpPost("place-bet")]
    [ProducesResponseType(typeof(BetResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [EnableRateLimiting("betting")]
    public async Task<IActionResult> PlaceBet(
        [FromServices] IPlaceBetUseCase useCase,
        [FromBody] PlaceBetRequest request,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ClientRequestId))
            request.ClientRequestId = idempotencyKey;

        var result = await useCase.Execute(request, cancellationToken);
        
        return Created(string.Empty, result);
    }

    [HttpGet("get-bets")]
    [ProducesResponseType(typeof(PagedResponse<BetResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
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
    public async Task<IActionResult> GetBetById(
        [FromServices] IGetBetByIdUseCase useCase,
        [FromRoute] long id,
        CancellationToken cancellationToken)
    {
        var result = await useCase.Execute(id, cancellationToken);
        return Ok(result);
    }
}
